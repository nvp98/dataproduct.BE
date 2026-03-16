using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;
using dataproduct.api.Services;
using dataproduct.api.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using System.Text.Json;

namespace dataproduct.api.Business
{
    public class PhieuService
    {
        private readonly IPhieuRepository _repo;
        private readonly ISTD_NXT_HRC2Repository _std_nxt_hrc2Repo;
        private readonly IBMPheDuyetRepository _pheDuyetRepo;
        private readonly BmPheDuyetService _pheDuyetService;
        private readonly ProductFormContext _context;
        private readonly DLNMHRC2Service _dlnmHrc2Service;

        public PhieuService(
            IPhieuRepository repo,
            ISTD_NXT_HRC2Repository std_nxt_hrc2Repo,
            IBMPheDuyetRepository pheDuyetRepo,
            BmPheDuyetService pheDuyetService,
            ProductFormContext context,
            DLNMHRC2Service dlnmHrc2Service)
        {
            _repo = repo;
            _std_nxt_hrc2Repo = std_nxt_hrc2Repo;
            _pheDuyetRepo = pheDuyetRepo;
            _pheDuyetService = pheDuyetService;
            _context = context;
            _dlnmHrc2Service = dlnmHrc2Service;
        }

        public async Task<IEnumerable<PhieuDto>> GetAllAsync(string? MaBM, int? NguoiTaoID, int? NguoiDuyetID, int? isCheckDuyet)
        {
            var data = (await _repo.GetAllAsync(MaBM, NguoiTaoID)).ToList();
            var listduyet = (await _pheDuyetService.GetAllAsync(NguoiDuyetID, isCheckDuyet)).ToList();

            if (NguoiDuyetID != null) // lọc theo user được duyệt
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
                                  Scope = p.Scope,
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
                    Scope = p.Scope,
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
                Scope = item.Scope,
                NgayTao = item.NgayTao,
                NgaySX = item.NgaySX,
                MayDuc = item.MayDuc,
                NguoiTaoId = item.NguoiTaoId,
                TinhTrang = item.TinhTrang,
                //DataJson = item.DataJson,
                JsonData = item.DataJson != null ? jsonObject : null,
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
            await CheckDuplicateAsync(formData);

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
            // Cho phép update khi ĐangLuu (0), Đã thu hồi (3) hoặc Hiệu chỉnh (7 = phiếu clone đang chỉnh sửa). Lưu ở trạng thái 7 không đổi TinhTrang.
            if (existing == null || (existing.TinhTrang != 0 && existing.TinhTrang != 3 && existing.TinhTrang != 7)) return null;

            // save data khi them cho hrc2
            string bm = formData.GetProperty("maBm").GetString();
            if (bm == "HRC2_BB_NauLuyen_BOF" || bm == "HRC2_BB_NauLuyen_LF" || bm == "HRC2_BB_NauLuyen_RH")
            {
                await _dlnmHrc2Service.SaveHRC2ManualFromPhieuFormAsync(formData);
            }

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

        public async Task<BmPhieu?> UpdateNguoiTaoAsync(Guid id, int? NguoiTaoID)
        {
            // 1. Lấy phiếu hiện tại
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;



            int nguoiTaoId = NguoiTaoID ?? 0;

            // 3. Lấy NgaySX, Ca, MaBm từ phiếu hiện tại
            var ngaySX = existing.NgaySX;
            var ca = existing.Ca;
            var maBm = existing.MaBm;

            if (!ngaySX.HasValue || !ca.HasValue || string.IsNullOrEmpty(maBm))
            {
                return null; // Thiếu thông tin cần thiết
            }

            // 4. Tìm tất cả các phiếu cùng NgaySX, Ca và MaBm
            var phieusToUpdate = await _context.BmPhieus
                .Where(x => x.NgaySX == ngaySX.Value
                         && x.Ca == ca.Value
                         && x.MaBm == maBm
                         && x.IsDelete != 1) // Không cập nhật phiếu đã xóa
                .ToListAsync();

            if (!phieusToUpdate.Any())
            {
                return null;
            }

            // 5. Cập nhật NguoiTaoId cho tất cả phiếu
            foreach (var phieu in phieusToUpdate)
            {
                phieu.NguoiTaoId = nguoiTaoId;
            }

            // 6. Lưu thay đổi
            _context.BmPhieus.UpdateRange(phieusToUpdate);
            await _context.SaveChangesAsync();

            // 7. Cập nhật hoặc tạo mới BM_PheDuyet cho người tạo (CapDuyet = 0)
            foreach (var phieu in phieusToUpdate)
            {
                // Tìm bản ghi phê duyệt với CapDuyet = 0
                var pheDuyetCapDuyet0 = await _context.BmPheDuyets
                    .FirstOrDefaultAsync(x => x.PhieuId == phieu.Idphieu && x.CapDuyet == 0);

                if (pheDuyetCapDuyet0 != null)
                {
                    // Nếu đã tồn tại, cập nhật NguoiDuyetId
                    pheDuyetCapDuyet0.NguoiDuyetId = nguoiTaoId;
                    _context.BmPheDuyets.Update(pheDuyetCapDuyet0);
                }
                else
                {
                    // Nếu chưa tồn tại, tạo mới
                    var newPheDuyet = new BmPheDuyet
                    {
                        PhieuId = phieu.Idphieu,
                        CapDuyet = 0,
                        NguoiDuyetId = nguoiTaoId,
                        TinhTrang = 0,
                        GhiChu = null
                    };
                    await _context.BmPheDuyets.AddAsync(newPheDuyet);
                }
            }

            // 8. Lưu thay đổi BM_PheDuyet
            await _context.SaveChangesAsync();

            // 9. Trả về phiếu hiện tại đã được cập nhật
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
                // 1. Lấy phiếu được bấm "Đề nghị hiệu chỉnh" (có thể là phiếu gốc hoặc phiếu clone),
                // chỉ cho clone khi đang Hoàn thành (2) hoặc Đang phê duyệt (6)
                var phieuGoc = await _repo.GetByIdAsync(id);
                if (phieuGoc == null) return null;
                if (phieuGoc.IsLock == 1)
                    throw new InvalidOperationException("Đã tồn tại phiếu hiệu chỉnh cho phiếu này. Vui lòng từ chối hoặc hoàn tất phiếu hiệu chỉnh hiện tại trước khi tạo mới.");
                PhieuStatusHelper.CheckAllowStatusChange(phieuGoc.TinhTrang ?? 0, 7);

                // 2. Phiếu cha chỉ IsLock = 1 để ẩn khỏi trang, không đổi TinhTrang
                phieuGoc.IsLock = 1;
                await _repo.UpdateAsync(phieuGoc);

                // 3. Tạo phiếu clone từ formData (copy dữ liệu y như phiếu cũ)
                var phieu = await _repo.AddAsync(formData);
                if (phieu == null) return null;

                // 4. Số phiếu clone = SoPhieu gốc + đuôi _HieuChinh_{VersionClone} (max 50 ký tự)
                var nextVersion = (phieuGoc.VersionClone ?? 0) + 1;
                var suffix = $"_HC_{nextVersion}";
                var soPhieuBase = (phieuGoc.SoPhieu ?? "").Trim();
                if (soPhieuBase.Length + suffix.Length > 50)
                    soPhieuBase = soPhieuBase.Substring(0, 50 - suffix.Length);
                phieu.SoPhieu = soPhieuBase + suffix;

                // 5. Clone mang trạng thái Hiệu chỉnh (7), hiện nút Lưu / Lưu và Gửi như Đang lưu
                phieu.IsClone = true;
                phieu.VersionClone = nextVersion;
                // ID_PhieuGoc luôn trỏ về phiếu cha (phiếu bị bấm clone), hỗ trợ clone nhiều tầng: A -> A1 -> A2...
                phieu.ID_PhieuGoc = phieuGoc.Idphieu;
                phieu.TinhTrang = 7;
                await _repo.UpdateAsync(phieu);

                return phieu;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> ChangeStatusAsync(Guid id, int status)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await CheckAllowPheDuyetAsync(existing.TinhTrang ?? 0, status);
            if (status == 1)
            {
                var allPheDuyet = await _pheDuyetRepo.GetByIdPhieuAsync(id);
                var isCreatorZero = allPheDuyet.Where(x => x.CapDuyet == 0).FirstOrDefault();
                if (isCreatorZero == null) return false;
                isCreatorZero.TinhTrang = 1;
                await _pheDuyetRepo.UpdateAsync(isCreatorZero);
            }
            //if(status == 2) {
            //var allPheDuyet = await _pheDuyetRepo.GetByIdPhieuAsync(id);
            //if(allPheDuyet == null || !allPheDuyet.Any()) return false;
            //var isCreatorZero = allPheDuyet.Any(x => x.CapDuyet == 0);
            //if(isCreatorZero) return false;
            //foreach(var item in allPheDuyet) {
            // item.TinhTrang = 1;
            // await _pheDuyetRepo.UpdateAsync(item);
            //}
            //}

            existing.TinhTrang = status;
            await _repo.UpdateAsync(existing);

            return true;
        }

        public async Task<bool> UpdateStatusExtendedAsync(Guid id, int? status, int? isLock, int? isDelete)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            if (status.HasValue) existing.TinhTrang = status.Value;
            if (isLock.HasValue) existing.IsLock = isLock.Value;
            if (isDelete.HasValue) existing.IsDelete = isDelete.Value;
            await _repo.UpdateAsync(existing);
            return true;
        }

        public Task<bool> CheckAllowPheDuyetAsync(int currentStatus, int status)
        {
            PhieuStatusHelper.CheckAllowStatusChange(currentStatus, status);
            return Task.FromResult(true);
        }

        public async Task<bool> CheckExistsAsync(string maBm, DateOnly ngaySX, int ca, int? scope, int? mayduc)
        {
            return await _repo.CheckExistsAsync(maBm, ngaySX, ca, scope, mayduc);
        }

        public async Task<PagedResult<SearchPhieuResponseModel>> SearchWithPagingAsync(SearchPhieuRequest request)
        {
            var (data, totalCount) = await _repo.SearchWithPagingAsync(request);
            return new PagedResult<SearchPhieuResponseModel>
            {
                Data = data.ToList(),
                TotalRecords = totalCount,
                Page = request.page,
                PageSize = request.pageSize
            };
        }


        public async Task<bool> InitializeAsync(Guid phieuId)
        {
            using var tran = await _context.Database.BeginTransactionAsync();

            try
            {
                var phieu = await _context.BmPhieus.FirstOrDefaultAsync(x => x.Idphieu == phieuId);

                if (phieu == null)
                {
                    return false;
                }

                // Xác định loại biểu mẫu để chạy logic riêng
                switch (phieu.MaBm)
                {
                    case "HRC2_STD_NXT":
                        await InitializeHRC2_STD_NXTAsync(phieu);
                        break;
                    // them các biểu mẫu khác nếu cần khởi tạo default
                    default:
                        break;
                }
                await _context.SaveChangesAsync();
                await tran.CommitAsync();


                return true;
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return false;
            }
        }
        public async Task InitializeHRC2_STD_NXTAsync(BmPhieu phieu)
        {
            await _std_nxt_hrc2Repo.InitializeHRC2_STD_NXTAsync(phieu);
        }
        // Helper
        private double? TryGetDouble(JsonElement row, string key)
        {
            if (row.TryGetProperty(key, out var p))
                return TryConvertNumeric(p);
            return null;
        }

        private double? TryConvertNumeric(JsonElement val)
        {
            if (val.ValueKind == JsonValueKind.Number)
                return val.GetDouble();

            if (val.ValueKind == JsonValueKind.String &&
                double.TryParse(val.GetString(), out var d))
                return d;

            return null;
        }
        private async Task CheckDuplicateAsync(JsonElement formData)
        {
            string maBM = formData.TryGetProperty("maBm", out var mBm)
                ? mBm.GetString()
                : null;

            if (string.IsNullOrEmpty(maBM))
                return;

            int Ca = formData.TryGetProperty("ca", out var ca)
                ? ca.GetInt32()
                : 0;

            int? Scope = formData.TryGetProperty("scope", out var scope) && scope.ValueKind != JsonValueKind.Null
                ? scope.GetInt32()
                : null;

            int? MayDuc = formData.TryGetProperty("mayduc", out var md) && md.ValueKind != JsonValueKind.Null
                ? md.GetInt32()
                : null;

            DateOnly? NgaySX = formData.TryGetProperty("NgaySX", out var nsx) && nsx.ValueKind != JsonValueKind.Null
                ? DateOnly.FromDateTime(nsx.GetDateTime())
                : null;

            if (!NgaySX.HasValue)
                return;

            bool exists = await _repo.CheckExistsAsync(maBM, NgaySX.Value, Ca, Scope, MayDuc);

            if (exists)
            {
                throw new InvalidOperationException(
                    $"Đã tồn tại phiếu {maBM} cho ngày {NgaySX:dd/MM/yyyy} ca {Ca}"
                );
            }
        }
    }
}

