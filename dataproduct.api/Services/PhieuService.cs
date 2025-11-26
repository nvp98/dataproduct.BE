using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace dataproduct.api.Business
{
    public class PhieuService
    {
        private readonly IPhieuRepository _repo;
        private readonly IBMPheDuyetRepository _pheDuyetRepo;
        private readonly BmPheDuyetService _pheDuyetService;

        public PhieuService(IPhieuRepository repo, IBMPheDuyetRepository pheDuyetRepo, BmPheDuyetService pheDuyetService)
        {
            _repo = repo;
            _pheDuyetRepo = pheDuyetRepo;
            _pheDuyetService = pheDuyetService;
        }

        public async Task<IEnumerable<PhieuDto>> GetAllAsync(string? MaBM,int? NguoiTaoID,int? NguoiDuyetID,int? isCheckDuyet)
        {
            var data = (await _repo.GetAllAsync(MaBM, NguoiTaoID)).ToList();
            var listduyet = (await _pheDuyetService.GetAllAsync(NguoiDuyetID, isCheckDuyet)).ToList();

            if(NguoiDuyetID != null) // lọc theo user được duyệt
            {
                // Join 2 danh sách theo Id phiếu
                var result = (from p in data
                              join d in listduyet on p.Idphieu equals d.PhieuId
                              //into joined
                              //from d in joined.DefaultIfEmpty()  // left join
                              select new PhieuDto
                              {
                                  Idphieu = p.Idphieu,
                                  MaBm = p.MaBm,
                                  SoPhieu = p.SoPhieu,
                                  XuongId = p.XuongId,
                                  IdphongBan = p.IdphongBan,
                                  Idkip = p.Idkip,
                                  Ca = p.Ca,
                                  Kip = p.Kip,
                                  NgayTao = p.NgayTao,
                                  NgaySX = p.NgaySX,
                                  MayDuc = p.MayDuc,
                                  NguoiTaoId = p.NguoiTaoId,
                                  TinhTrang = p.TinhTrang,
                                //   DataJson = p.DataJson,
                                  IsDelete = p.IsDelete,
                                  IsLock = p.IsLock,
                                  LoaiPhieu = p.LoaiPhieu,
                                  IsClone = p.IsClone,
                                  VersionClone = p.VersionClone,
                                  ID_PhieuGoc = p.ID_PhieuGoc,
                                  PheDuyet = new List<BM_PheDuyetDto> { d },
                              }).ToList();

                return result;
            }
            else 
            {
                return data.Select(p => new PhieuDto
                {
                    Idphieu = p.Idphieu,
                    MaBm = p.MaBm,
                    SoPhieu = p.SoPhieu,
                    XuongId = p.XuongId,
                    IdphongBan = p.IdphongBan,
                    Idkip = p.Idkip,
                    Ca = p.Ca,
                    Kip = p.Kip,
                    NgayTao = p.NgayTao,
                    NgaySX = p.NgaySX,
                    MayDuc = p.MayDuc,
                    NguoiTaoId = p.NguoiTaoId,
                    TinhTrang = p.TinhTrang,
                    // DataJson = p.DataJson,
                    IsDelete = p.IsDelete,
                    IsLock = p.IsLock,
                    LoaiPhieu = p.LoaiPhieu,
                    IsClone = p.IsClone,
                    VersionClone = p.VersionClone,
                    ID_PhieuGoc = p.ID_PhieuGoc,
                    // PheDuyet = new List<BM_PheDuyetDto>(),
                });
            }
            

        }

        public async Task<PhieuDto?> GetByIdAsync(Guid id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return null;

            // Parse JSON trong DataJson thành object
            var jsonObject = !string.IsNullOrEmpty(item.DataJson)
                ? JsonSerializer.Deserialize<JsonElement>(item.DataJson)
                : new JsonElement();
            // Thông tin phê duyệt

            var duyet = await _pheDuyetService.GetByIdPhieuAsync(id);

            return new PhieuDto
            {
                Idphieu = item.Idphieu,
                MaBm = item.MaBm,
                SoPhieu = item.SoPhieu,
                XuongId = item.XuongId,
                IdphongBan = item.IdphongBan,
                Idkip = item.Idkip,
                Ca = item.Ca,
                Kip = item.Kip,
                NgayTao = item.NgayTao,
                MayDuc = item.MayDuc,
                NguoiTaoId = item.NguoiTaoId,
                TinhTrang = item.TinhTrang,
                //DataJson = item.DataJson,
                JsonData = jsonObject,
                IsDelete = item.IsDelete,
                IsLock = item.IsLock,
                LoaiPhieu = item.LoaiPhieu,
                IsClone = item.IsClone,
                VersionClone = item.VersionClone,
                ID_PhieuGoc = item.ID_PhieuGoc,
                PheDuyet = duyet?.ToList() ?? new List<BM_PheDuyetDto>(),
            };
        }

        public async Task<BmPhieu> CreateAsync(JsonElement formData)
        {
            try
            {
                var phieu = await _repo.AddAsync(formData);
                return phieu;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<BmPhieu?> UpdateAsync(Guid id, JsonElement formData)
        {
            // 1. Lấy phiếu hiện tại
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            // 2. Cập nhật các field chính (nếu có trong JSON)
            if (formData.TryGetProperty("NgaySX", out var ngaySXProp) && ngaySXProp.ValueKind != JsonValueKind.Null)
                existing.NgaySX = DateOnly.FromDateTime(ngaySXProp.GetDateTime());

            if (formData.TryGetProperty("ca", out var caProp) && caProp.ValueKind != JsonValueKind.Null)
                existing.Ca = caProp.GetInt32();

            if (formData.TryGetProperty("mayduc", out var mayDucProp) && mayDucProp.ValueKind != JsonValueKind.Null)
                existing.MayDuc = mayDucProp.GetInt32();

            if (formData.TryGetProperty("nguoiTaoId", out var nguoiTaoProp) && nguoiTaoProp.ValueKind != JsonValueKind.Null)
                existing.NguoiTaoId = nguoiTaoProp.GetInt32();

            if (formData.TryGetProperty("xuongId", out var xuongIdProp) && xuongIdProp.ValueKind != JsonValueKind.Null)
                existing.XuongId = xuongIdProp.GetInt32();

            if (formData.TryGetProperty("idphongBan", out var idphongBan) && idphongBan.ValueKind != JsonValueKind.Null)
                existing.IdphongBan = idphongBan.GetInt32();

            // 3. Lưu lại JSON gốc (form động)
            existing.DataJson = formData.GetRawText();
            existing.NgayTao = existing.NgayTao; // giữ nguyên ngày tạo
            existing.IsLock = 0; // nếu muốn mở khóa khi sửa
            // Giữ nguyên TinhTrang hiện tại, không reset về 0
            // existing.TinhTrang giữ nguyên giá trị hiện tại

            // 4. Gọi repository để lưu
            await _repo.UpdateAsync(existing);
            // Cập nhật thông tin phê duyệt
            // Lưu thông tin phê duyệt

            List<BmPheDuyet> pheDuyetList = new();

            if (formData.TryGetProperty("pheDuyet", out var pheDuyetProp) && pheDuyetProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in pheDuyetProp.EnumerateArray())
                {
                    var phe = new BmPheDuyet
                    {
                        PhieuId = existing.Idphieu,
                        CapDuyet = item.TryGetProperty("capDuyet", out var capProp) ? capProp.GetInt32() : 0,
                        NguoiDuyetId = item.TryGetProperty("nguoiDuyetId", out var ndProp) ? ndProp.GetInt32() : 0,
                        TinhTrang = item.TryGetProperty("tinhTrang", out var ttProp) ? ttProp.GetInt32() : 0,
                        GhiChu = item.TryGetProperty("ghiChu", out var gcProp) ? gcProp.GetString() : null,
                    };

                    pheDuyetList.Add(phe);
                }
            }
            if (pheDuyetList.Count > 0)
            {
                // gọi repo bmpheduyet addlist
                await _pheDuyetRepo.AddListAsync(pheDuyetList, existing.Idphieu);
            }


            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var exists = await _repo.ExistsAsync(id);
            if (!exists) return false;
            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<BmPhieu?> CloneAsync(Guid id, JsonElement formData)
        {
            try
            {
                // 1. Lấy phiếu gốc để lấy VersionClone hiện tại
                var phieuGoc = await _repo.GetByIdAsync(id);
                if (phieuGoc == null) return null;

                // phieuGoc.IsLock = 1;
                // await _repo.UpdateAsync(phieuGoc);
                // 2. Tạo mới record từ formData (giống như hàm CreateAsync)
                var phieu = await _repo.AddAsync(formData);
                if (phieu == null) return null;

                // 3. Update các trường clone cho record mới tạo
                phieu.IsClone = true;
                phieu.VersionClone = (phieuGoc.VersionClone ?? 0) + 1;
                phieu.ID_PhieuGoc = id;
                await _repo.UpdateAsync(phieu);

                return phieu;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> ChangeStatusAsync(Guid id, int status){
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            // if(status == 4) {
            //     existing.IsLock = 1;
            // }
            existing.TinhTrang = status;
            await _repo.UpdateAsync(existing);

            // if(existing.ID_PhieuGoc != null) {
            //     var phieuGoc = await _repo.GetByIdAsync(existing.ID_PhieuGoc.Value);
            //     if (phieuGoc == null) return false;
            //     phieuGoc.IsLock = 0;
            //     await _repo.UpdateAsync(phieuGoc);
            // }

            return true;
        }
    }
}
