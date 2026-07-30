using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.Utils.Enums;

namespace dataproduct.api.Services
{
    public class NhomPhanBoService
    {
        private readonly INhomPhanBoRepository _repo;
        private readonly ITyLePhanBoRepository _tyLeRepo;
        private readonly IKetQuaPhanBoRepository _ketQuaRepo;

        private static readonly byte[] TatCaLoaiPhanBo = { 1, 2, 3 };

        public NhomPhanBoService(INhomPhanBoRepository repo, ITyLePhanBoRepository tyLeRepo, IKetQuaPhanBoRepository ketQuaRepo)
        {
            _repo = repo;
            _tyLeRepo = tyLeRepo;
            _ketQuaRepo = ketQuaRepo;
        }

        public async Task<List<NhomPhanBoDto>> GetListAsync(byte? loaiPhanBo)
        {
            var list = await _repo.GetListAsync(loaiPhanBo);
            return list.Select(MapNhom).ToList();
        }

        public async Task<NhomPhanBoDto> CreateAsync(CreateNhomPhanBoDto dto)
        {
            ValidateLoaiPhanBo(dto.LoaiPhanBo);

            var entity = new LG_PB_NhomPhanBo
            {
                TenNhom = dto.TenNhom,
                LoaiPhanBo = dto.LoaiPhanBo,
                PhuongThucPhanBo = dto.PhuongThucPhanBo,
                MaVatTu = dto.MaVatTu,
                ThuTu = dto.ThuTu
            };
            var created = await _repo.AddAsync(entity);
            return MapNhom(created);
        }

        // Than cốc <10mm dùng chung cấu hình nhóm/NVL với CVH (LoaiPhanBo=2) — không tạo nhóm riêng cho 3
        private static void ValidateLoaiPhanBo(byte loaiPhanBo)
        {
            if (loaiPhanBo == (byte)LoaiPhanBoEnum.ThanCoc10)
                throw new InvalidOperationException(
                    "Không tạo nhóm riêng cho Than cốc <10mm — nhóm này dùng chung cấu hình với CVH (chọn Loại phân bổ = CVH).");
        }

        public async Task<NhomPhanBoDto> UpdateAsync(int id, UpdateNhomPhanBoDto dto)
        {
            ValidateLoaiPhanBo(dto.LoaiPhanBo);

            var entity = new LG_PB_NhomPhanBo
            {
                TenNhom = dto.TenNhom,
                LoaiPhanBo = dto.LoaiPhanBo,
                PhuongThucPhanBo = dto.PhuongThucPhanBo,
                MaVatTu = dto.MaVatTu,
                ThuTu = dto.ThuTu
            };
            var updated = await _repo.UpdateAsync(id, entity);
            if (updated == null) throw new InvalidOperationException("Không tìm thấy nhóm phân bổ.");
            return MapNhom(updated);
        }

        public async Task<bool> DeleteAsync(int id) => await _repo.DeleteAsync(id);

        // NVL thành viên của nhóm — cấu hình RIÊNG cho từng (Ngày, Ca), không kế thừa từ ngày/ca trước
        public async Task<List<NvlNhomPhanBoDto>> GetNvlByNhomAsync(int idNhomPhanBo, DateTime ngay, byte ca)
            => await _repo.GetNvlByNhomAsync(idNhomPhanBo, ngay, ca);

        // Với nhóm PP1 (tỷ trọng + dòng dư), "dòng dư" (NVL nhận phần bù trừ do làm tròn) được
        // PhanBoService TỰ ĐỘNG chọn theo khối lượng nạp liệu (E) lớn nhất tại thời điểm tính —
        // không cần cấu hình tay ở đây, kể cả khi nhóm chỉ có 1 NVL.
        public async Task<NvlNhomPhanBoDto> AddNvlAsync(int idNhomPhanBo, AddNvlNhomPhanBoDto dto)
        {
            var nhom = await _repo.GetByIdAsync(idNhomPhanBo)
                ?? throw new InvalidOperationException("Không tìm thấy nhóm phân bổ.");

            var entity = new LG_PB_NVL_NhomPhanBo
            {
                IDNVL = dto.IdNvl,
                IDNhomPhanBo = idNhomPhanBo,
                Ngay = dto.Ngay.Date,
                Ca = dto.Ca,
                IDLoCao = dto.IdLoCao,
                ThuTuUuTien = 0
            };
            await _repo.AddNvlAsync(entity);

            // Nhóm PP2 (tỷ lệ nhập tay) đã có % nhóm cấu hình sẵn cho đúng (Ngày, Ca, Lò cao) này
            // → NVL mới thêm vào tự động lấy % đó, không cần nhập tay lại.
            if (nhom.PhuongThucPhanBo == (byte)PhuongThucPhanBoEnum.TyLeNhapTay)
            {
                var tyLeNhom = await _tyLeRepo.GetTyLeNhomAsync(idNhomPhanBo, dto.Ngay.Date, dto.Ca, dto.IdLoCao);
                if (tyLeNhom != null)
                {
                    await _tyLeRepo.UpsertAsync(new LG_PB_TyLePhanBo
                    {
                        IDNVL = dto.IdNvl,
                        Ngay = dto.Ngay.Date,
                        Ca = dto.Ca,
                        TyLe = tyLeNhom.TyLe,
                        IDNguoiNhap = tyLeNhom.IDNguoiNhap
                    });
                }
            }

            var list = await _repo.GetNvlByNhomAsync(idNhomPhanBo, dto.Ngay, dto.Ca);
            return list.First(x => x.IdNvl == dto.IdNvl);
        }

        public async Task<SaoChepNhomPhanBoResultDto> SaoChepAsync(SaoChepNhomPhanBoRequestDto dto)
        {
            // Kiểm tra ngày đích đã chốt
            foreach (var loai in TatCaLoaiPhanBo)
            {
                if (await _ketQuaRepo.IsNgayDaChotAsync(dto.NgayDich.Date, loai))
                    throw new InvalidOperationException(
                        $"Ngày đích {dto.NgayDich:dd/MM/yyyy} đã chốt, không thể sao chép vào.");
            }

            // Tìm ca có cấu hình gần nhất
            var caLienKe = await _repo.GetCaLienKe(
                dto.LoaiPhanBo,
                dto.IdLoCaoDich,
                dto.NgayDich.Date,
                dto.CaDich);

            if (caLienKe == null)
                throw new InvalidOperationException("Không tìm thấy cấu hình ca trước để sao chép.");

            var (ngayNguon, caNguon) = caLienKe.Value;

            // Lấy dữ liệu nguồn
            var nguon = await _repo.GetNhomVaThanhVienAsync(
                dto.LoaiPhanBo,
                ngayNguon,
                caNguon,
                dto.IdLoCaoDich);

            if (!nguon.Any())
                throw new InvalidOperationException("Ca nguồn không có cấu hình để sao chép.");

            // Lấy dữ liệu đích
            var dich = await _repo.GetNhomVaThanhVienAsync(
                dto.LoaiPhanBo,
                dto.NgayDich.Date,
                dto.CaDich,
                dto.IdLoCaoDich);

            var dichMemberMap = dich.ToDictionary(
                x => x.Nhom.ID,
                x => x.ThanhVien.Select(t => t.IDNVL).ToHashSet());

            var soNvlDaCopy = 0;
            var soTyLeDaCopy = 0;

            foreach (var (nhom, thanhVienNguon) in nguon)
            {
                if (thanhVienNguon.Count == 0)
                    continue;

                var laPp2 = nhom.PhuongThucPhanBo == (byte)PhuongThucPhanBoEnum.TyLeNhapTay;

                var daCoODich = dichMemberMap.GetValueOrDefault(nhom.ID)
                                ?? new HashSet<int>();

                var tyLeNguonMap = laPp2
                    ? await _tyLeRepo.GetHieuLucMapAsync(
                        thanhVienNguon.Select(x => x.IDNVL),
                        ngayNguon,
                        caNguon)
                    : new Dictionary<int, decimal>();

                foreach (var tv in thanhVienNguon)
                {
                    if (!daCoODich.Contains(tv.IDNVL))
                    {
                        await _repo.AddNvlAsync(new LG_PB_NVL_NhomPhanBo
                        {
                            IDNVL = tv.IDNVL,
                            IDNhomPhanBo = nhom.ID,
                            Ngay = dto.NgayDich.Date,
                            Ca = dto.CaDich,
                            IDLoCao = dto.IdLoCaoDich,
                            ThuTuUuTien = tv.ThuTuUuTien
                        });

                        soNvlDaCopy++;
                    }

                    if (laPp2 &&
                        tyLeNguonMap.TryGetValue(tv.IDNVL, out var tyLe))
                    {
                        await _tyLeRepo.UpsertAsync(new LG_PB_TyLePhanBo
                        {
                            IDNVL = tv.IDNVL,
                            Ngay = dto.NgayDich.Date,
                            Ca = dto.CaDich,
                            TyLe = tyLe,
                            IDNguoiNhap = dto.IdNguoiThucHien
                        });

                        soTyLeDaCopy++;
                    }
                }

                if (laPp2)
                {
                    var tyLeNhomNguon = await _tyLeRepo.GetTyLeNhomAsync(
                        nhom.ID,
                        ngayNguon,
                        caNguon,
                        dto.IdLoCaoDich);

                    if (tyLeNhomNguon != null)
                    {
                        await _tyLeRepo.UpsertTyLeNhomAsync(new LG_PB_TyLeNhom
                        {
                            IDNhomPhanBo = nhom.ID,
                            Ngay = dto.NgayDich.Date,
                            Ca = dto.CaDich,
                            IDLoCao = dto.IdLoCaoDich,
                            TyLe = tyLeNhomNguon.TyLe,
                            GhiChu = tyLeNhomNguon.GhiChu,
                            IDNguoiNhap = dto.IdNguoiThucHien
                        });
                    }
                }
            }

            return new SaoChepNhomPhanBoResultDto
            {
                SoNvlDaCopy = soNvlDaCopy,
                SoTyLeDaCopy = soTyLeDaCopy,
                NgayNguon = ngayNguon,
                CaNguon = caNguon
            };
        }

        public async Task<bool> RemoveNvlAsync(int idNhomPhanBo, int idNvl, DateTime ngay, byte ca)
            => await _repo.RemoveNvlAsync(idNhomPhanBo, idNvl, ngay, ca);

        private static NhomPhanBoDto MapNhom(LG_PB_NhomPhanBo x) => new()
        {
            Id = x.ID,
            TenNhom = x.TenNhom,
            LoaiPhanBo = x.LoaiPhanBo,
            PhuongThucPhanBo = x.PhuongThucPhanBo,
            MaVatTu = x.MaVatTu,
            ThuTu = x.ThuTu
        };
    }
}
