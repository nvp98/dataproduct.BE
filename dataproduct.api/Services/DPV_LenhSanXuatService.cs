using System.Text.RegularExpressions;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class DPV_LenhSanXuatDto
    {
        public int Id { get; set; }
        public int? IdPB { get; set; }
        public int? IdXuong { get; set; }
        public string Mac { get; set; } = null!;
        public string KichThuoc { get; set; } = null!;
        public int MayDuc { get; set; }
        public string? MaLenh { get; set; }
        public string? LenhSanXuat { get; set; }
        public string? GhiChu { get; set; }
        public string? NguoiTao { get; set; }
        public DateTime NgayTao { get; set; }
        public string? NguoiCapNhat { get; set; }
        public DateTime? NgayCapNhat { get; set; }
        /// <summary>Cùng LSX với tổ hợp (Mác, KT, Máy đúc) khác → cảnh báo.</summary>
        public bool TrungLSX { get; set; }
        public bool ChuaCoMaLenh => string.IsNullOrEmpty(MaLenh);
    }

    public class DPV_LenhSanXuatPayload
    {
        public int? IdPB { get; set; }
        public int? IdXuong { get; set; }
        public string Mac { get; set; } = null!;
        public string KichThuoc { get; set; } = null!;
        public int MayDuc { get; set; }
        public string? MaLenh { get; set; }
        public string? GhiChu { get; set; }
    }

    public class SinhLenhSanXuatResult
    {
        public int TongToHop { get; set; }
        public int DaCo { get; set; }
        public int ThemMoi { get; set; }
    }

    public class DPV_LenhSanXuatService
    {
        /// <summary>LenhSanXuat = TienTo + MaLenh (vd 3861 → 1010003861).</summary>
        public const string TienToLSX = "101000";

        private readonly IDPV_LenhSanXuatRepository _repo;

        public DPV_LenhSanXuatService(IDPV_LenhSanXuatRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<DPV_LenhSanXuatDto>> SearchAsync(string? mac, string? kichThuoc, int? mayDuc, int? idXuong, string? searchKey)
        {
            var rows = await _repo.SearchAsync(mac, kichThuoc, mayDuc, idXuong, searchKey);

            // Kiểm tra trùng LSX trên toàn bộ danh mục, không chỉ trên các dòng đang lọc
            var trung = (await _repo.GetAllActiveAsync())
                .Where(x => !string.IsNullOrEmpty(x.LenhSanXuat))
                .GroupBy(x => x.LenhSanXuat!)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            return rows.Select(x => ToDto(x, x.LenhSanXuat != null && trung.Contains(x.LenhSanXuat))).ToList();
        }

        public async Task<DPV_LenhSanXuat> CreateAsync(DPV_LenhSanXuatPayload payload, string? nguoi)
        {
            var entity = new DPV_LenhSanXuat { NguoiTao = nguoi, NgayTao = DateTime.Now };
            await ApplyAsync(entity, payload, excludeId: null);
            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, DPV_LenhSanXuatPayload payload, string? nguoi)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            await ApplyAsync(entity, payload, excludeId: id);
            entity.NguoiCapNhat = nguoi;
            entity.NgayCapNhat = DateTime.Now;
            await _repo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, string? nguoi)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            entity.IsDelete = true;
            entity.NguoiCapNhat = nguoi;
            entity.NgayCapNhat = DateTime.Now;
            await _repo.UpdateAsync(entity);
            return true;
        }

        /// <summary>
        /// Quét dữ liệu mẻ trong khoảng ngày, thêm các tổ hợp (Mác, KT, Máy đúc) chưa có trong danh mục
        /// (chưa có mã lệnh — người dùng nhập bổ sung).
        /// </summary>
        public async Task<SinhLenhSanXuatResult> SinhTuDuLieuMeAsync(DateOnly tuNgay, DateOnly denNgay, int? idPB, int? idXuong, string? nguoi)
        {
            if (denNgay < tuNgay) throw new InvalidOperationException("Đến ngày phải sau Từ ngày.");

            var toHops = await _repo.GetToHopTuDuLieuMeAsync(tuNgay, denNgay);
            var daCo = (await _repo.GetAllActiveAsync())
                .Select(x => Key(x.Mac, x.KichThuoc, x.MayDuc))
                .ToHashSet();

            var result = new SinhLenhSanXuatResult { TongToHop = toHops.Count };
            var moi = new List<DPV_LenhSanXuat>();

            foreach (var (mac, kichThuoc, mayDuc) in toHops)
            {
                if (!daCo.Add(Key(mac, kichThuoc, mayDuc))) { result.DaCo++; continue; }

                moi.Add(new DPV_LenhSanXuat
                {
                    IdPB = idPB,
                    IdXuong = idXuong,
                    Mac = mac,
                    KichThuoc = kichThuoc,
                    MayDuc = mayDuc,
                    NguoiTao = nguoi,
                    NgayTao = DateTime.Now,
                });
            }

            if (moi.Count > 0)
                await _repo.AddRangeAsync(moi);
            result.ThemMoi = moi.Count;
            return result;
        }

        public static string? TinhLenhSanXuat(string? maLenh)
            => string.IsNullOrEmpty(maLenh) ? null : TienToLSX + maLenh;

        private async Task ApplyAsync(DPV_LenhSanXuat entity, DPV_LenhSanXuatPayload payload, int? excludeId)
        {
            var mac = payload.Mac?.Trim();
            var kichThuoc = payload.KichThuoc?.Trim();
            var maLenh = string.IsNullOrWhiteSpace(payload.MaLenh) ? null : payload.MaLenh.Trim();

            if (string.IsNullOrEmpty(mac)) throw new InvalidOperationException("Mác không được để trống.");
            if (string.IsNullOrEmpty(kichThuoc)) throw new InvalidOperationException("Kích thước không được để trống.");
            if (payload.MayDuc <= 0) throw new InvalidOperationException("Máy đúc không hợp lệ.");
            if (maLenh != null && !Regex.IsMatch(maLenh, @"^\d{1,14}$"))
                throw new InvalidOperationException("Mã lệnh chỉ gồm chữ số.");

            if (await _repo.ExistsToHopAsync(mac, kichThuoc, payload.MayDuc, excludeId))
                throw new InvalidOperationException($"Đã có LSX cho mác {mac}, KT {kichThuoc}, máy đúc {payload.MayDuc}.");

            entity.IdPB = payload.IdPB;
            entity.IdXuong = payload.IdXuong;
            entity.Mac = mac;
            entity.KichThuoc = kichThuoc;
            entity.MayDuc = payload.MayDuc;
            entity.MaLenh = maLenh;
            entity.LenhSanXuat = TinhLenhSanXuat(maLenh);
            entity.GhiChu = string.IsNullOrWhiteSpace(payload.GhiChu) ? null : payload.GhiChu.Trim();
        }

        private static string Key(string mac, string kichThuoc, int mayDuc)
            => $"{mac.Trim().ToUpperInvariant()}|{kichThuoc.Trim().ToUpperInvariant()}|{mayDuc}";

        private static DPV_LenhSanXuatDto ToDto(DPV_LenhSanXuat x, bool trungLSX) => new()
        {
            Id = x.Id,
            IdPB = x.IdPB,
            IdXuong = x.IdXuong,
            Mac = x.Mac,
            KichThuoc = x.KichThuoc,
            MayDuc = x.MayDuc,
            MaLenh = x.MaLenh,
            LenhSanXuat = x.LenhSanXuat,
            GhiChu = x.GhiChu,
            NguoiTao = x.NguoiTao,
            NgayTao = x.NgayTao,
            NguoiCapNhat = x.NguoiCapNhat,
            NgayCapNhat = x.NgayCapNhat,
            TrungLSX = trungLSX,
        };
    }
}
