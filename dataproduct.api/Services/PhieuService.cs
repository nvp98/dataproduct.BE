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

        public PhieuService(IPhieuRepository repo, ISTD_NXT_HRC2Repository std_nxt_hrc2Repo, IBMPheDuyetRepository pheDuyetRepo, BmPheDuyetService pheDuyetService, ProductFormContext context)
        {
            _repo = repo;
            _std_nxt_hrc2Repo = std_nxt_hrc2Repo;
            _pheDuyetRepo = pheDuyetRepo;
            _pheDuyetService = pheDuyetService;
            _context = context;
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

            // save data khi them cho hrc2
            string bm = formData.GetProperty("maBm").GetString();
            if (bm == "HRC2_BB_NauLuyen_BOF" || bm == "HRC2_BB_NauLuyen_LF" || bm == "HRC2_BB_NauLuyen_RH")
            {
                var data = await BuildModelToInsert(formData);
                await SaveHRC2ManualDataAsync(data);
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
                // 1. Lấy phiếu gốc để lấy VersionClone hiện tại
                var phieuGoc = await _repo.GetByIdAsync(id);
                if (phieuGoc == null) return null;

                phieuGoc.IsLock = 1;
                await _repo.UpdateAsync(phieuGoc);
                // 2. Tạo mới record từ formData (giống như hàm CreateAsync)
                var phieu = await _repo.AddAsync(formData);
                if (phieu == null) return null;

                // 3. Update các trường clone cho record mới tạo
                phieu.IsClone = true;
                phieu.VersionClone = (phieuGoc.VersionClone ?? 0) + 1;
                phieu.ID_PhieuGoc = id;
                phieu.TinhTrang = 0;
                await _repo.UpdateAsync(phieu);

                return phieu;
            }
            catch (Exception ex)
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

        public async Task<List<HRC2InsertModel>> BuildModelToInsert(JsonElement formData)
        {
            var result = new List<HRC2InsertModel>();

            string bm = formData.GetProperty("maBm").GetString();
            int scope = formData.GetProperty("scope").GetInt32();
            int ca = formData.GetProperty("ca").GetInt32();

            string ngaySXstr = formData.TryGetProperty("NgaySX", out var nsxProp)
                ? nsxProp.GetString()
                : null;

            DateTime ngaySX = !string.IsNullOrEmpty(ngaySXstr)
                ? DateTime.Parse(ngaySXstr)
                : DateTime.Now;

            var table1 = formData.GetProperty("table1").EnumerateArray().ToList();
            var dynamicRoot = formData.GetProperty("table1DynamicColumns");

            // BM → nhóm cột dynamic
            var bmColumnMap = new Dictionary<string, List<string>>
            {
                { "HRC2_BB_NauLuyen_BOF", new List<string> { "BOF_PhuGia", "others", "adjust" } },
                { "HRC2_BB_NauLuyen_LF", new List<string> { "PG", "KL", "others", "adjust" } },
                { "HRC2_BB_NauLuyen_RH", new List<string> { "PG", "KL", "others", "adjust" } }
            };

            if (!bmColumnMap.TryGetValue(bm, out var colGroups))
                return result;
            string LoaiBM = "";
            if (bm == "HRC2_BB_NauLuyen_BOF") LoaiBM = "BOF";
            else if (bm == "HRC2_BB_NauLuyen_LF") LoaiBM = "LF";
            else LoaiBM = "RH";
            // Gom tất cả dynamic columns
            var dynamicCols = new List<JsonElement>();
            foreach (var g in colGroups)
                if (dynamicRoot.TryGetProperty(g, out var grp))
                    dynamicCols.AddRange(grp.EnumerateArray());

            // Lấy các row nhập tay
            var rowsNhapTay = table1
                .Where(r => r.TryGetProperty("IsNM", out var flag)
                            && flag.ValueKind == JsonValueKind.False)
                .ToList();

            foreach (var row in rowsNhapTay)
            {
                string meThoi = row.GetProperty("meThoi").GetString();
                string macThep = row.TryGetProperty("macThep", out var mt) ? mt.GetString() : null;

                double? o2 = TryGetDouble(row, "o2");
                double? n2 = TryGetDouble(row, "n2");
                double? ar_RH = TryGetDouble(row, "ar_RH");
                double? ar_LF = TryGetDouble(row, "ar_LF");
                double? ar_BOF = TryGetDouble(row, "ar_BOF");

                double? gang = TryGetDouble(row, "klGangLong");
                double? phe = TryGetDouble(row, "klThepPhe");
                double? klThepLong = TryGetDouble(row, "klThepLong");
                int? id = row.TryGetProperty("id", out var idRow) ? idRow.GetInt32() : null;

                // ==========================
                // TẠO 1 DÒNG MODEL CHÍNH
                // ==========================
                var model = new HRC2InsertModel
                {
                    Id = id,
                    Ngay = ngaySX,
                    Ca = ca,
                    BieuMau = LoaiBM,
                    Scope = scope,

                    MeThoi = meThoi,
                    MacThep = macThep,

                    O2 = o2,
                    N2 = n2,
                    AR_BOF = ar_BOF,
                    AR_LF = ar_LF,
                    AR_RH = ar_RH,

                    KLGangLong = gang,
                    KLThepPhe = phe,
                    KlThepLong = klThepLong,

                    IsNM = false,
                    IsChuyenCa = false
                };

                // ==========================
                // XỬ LÝ PHỤ LIỆU
                // ==========================
                foreach (var col in dynamicCols)
                {
                    string dataIndex = col.GetProperty("dataIndex").GetString();

                    if (!row.TryGetProperty(dataIndex, out var valProp))
                        continue;

                    double? klPhuGia = TryConvertNumeric(valProp);
                    if (klPhuGia == null)
                        continue;

                    // label & headerKeyId
                    string label = col.TryGetProperty("label", out var lblProp)
                        ? lblProp.GetString()
                        : null;

                    int? headerKeyId = null;
                    if (col.TryGetProperty("headerKeyId", out var hkProp)
                        && hkProp.ValueKind == JsonValueKind.Number)
                    {
                        headerKeyId = hkProp.GetInt32();
                    }

                    // mappingPayload → lấy ID_PhuLieu & TenPhuLieu
                    int? idPhuLieu = null;
                    string tenPhuLieu = null;

                    if (col.TryGetProperty("mappingPayload", out var mp)
                        && mp.ValueKind != JsonValueKind.Null)
                    {
                        if (mp.TryGetProperty("idPhuLieu", out var idp)
                            && idp.ValueKind == JsonValueKind.Number)
                        {
                            idPhuLieu = idp.GetInt32();
                        }

                        tenPhuLieu = mp.TryGetProperty("tenPhuLieu", out var tnp)
                            ? tnp.GetString()
                            : null;
                    }

                    model.hRC2_PhuLieus.Add(new HRC2_PhuLieuInSertModel
                    {
                        MeThoi = meThoi,
                        BieuMau = LoaiBM,
                        ID_PhuLieu = idPhuLieu,
                        TenPhuLieu = tenPhuLieu,
                        KLPhuGia = klPhuGia,
                        ID_HeaderKey = headerKeyId,
                        TenHienThi = label
                    });
                }

                result.Add(model);
            }
            return result;
        }

        // public async Task SaveHRC2ManualDataAsync(List<HRC2InsertModel> models)
        // {
        //     if (models == null || !models.Any())
        //         return;

        //     var dlnmMap = new Dictionary<Guid, DLNM_HRC2>(); // Key = RowKey

        //     foreach (var model in models)
        //     {
        //         // Tìm DLNM cha cũ (nếu có)
        //         DLNM_HRC2 existing = null;
        //         if (model.Id != null && model.Id > 0)
        //         {
        //             existing = await _context.DLNM_HRC2s
        //                 .FirstOrDefaultAsync(x => x.ID == model.Id && x.IsNM == false);
        //         }

        //         // Kiểm tra trùng mẻ
        //         var existingMeThoi = await _context.DLNM_HRC2s
        //             .Where(x => x.MeThoi == model.MeThoi &&
        //                         x.BieuMau == model.BieuMau &&
        //                         x.IsNM == false)
        //             .ToListAsync();

        //         bool isTrung = existingMeThoi.Any();

        //         if (isTrung)
        //         {
        //             foreach (var item in existingMeThoi)
        //             {
        //                 item.IsTrungMeThoi = true;
        //             }
        //             _context.DLNM_HRC2s.UpdateRange(existingMeThoi);
        //         }

        //         DLNM_HRC2 dlnm;

        //         if (existing == null)
        //         {
        //             // Tạo mới DLNM
        //             dlnm = new DLNM_HRC2
        //             {
        //                 REPORT_NO = null,
        //                 NgaySx = model.Ngay,
        //                 Ngay = model.Ngay,
        //                 Ca = model.Ca,
        //                 BieuMau = model.BieuMau,
        //                 Scope = model.Scope,
        //                 MeThoi = model.MeThoi,
        //                 MacThep = model.MacThep,
        //                 O2 = model.O2,
        //                 N2 = model.N2,
        //                 AR_RH = model.AR_RH,
        //                 AR_LF = model.AR_LF,
        //                 AR_BOF = model.AR_BOF,
        //                 KLGangLong = model.KLGangLong,
        //                 KLThepPhe = model.KLThepPhe,
        //                 KLThepLong = model.KlThepLong,
        //                 IsNM = false,
        //                 IsChuyenCa = model.IsChuyenCa,
        //                 IsTrungMeThoi = isTrung,

        //                 TempKey = Guid.NewGuid()  // ⭐ Quan trọng để map phụ liệu
        //             };

        //             await _context.DLNM_HRC2s.AddAsync(dlnm);
        //         }
        //         else
        //         {
        //             // Update DLNM
        //             dlnm = existing;

        //             dlnm.MeThoi = model.MeThoi;
        //             dlnm.MacThep = model.MacThep;
        //             dlnm.O2 = model.O2;
        //             dlnm.N2 = model.N2;
        //             dlnm.AR_RH = model.AR_RH;
        //             dlnm.AR_LF = model.AR_LF;
        //             dlnm.AR_BOF = model.AR_BOF;
        //             dlnm.KLGangLong = model.KLGangLong;
        //             dlnm.KLThepPhe = model.KLThepPhe;
        //             dlnm.KLThepLong = model.KlThepLong;
        //             dlnm.IsChuyenCa = model.IsChuyenCa;
        //             dlnm.NgaySx = model.Ngay;
        //             dlnm.IsTrungMeThoi = isTrung;

        //             if (dlnm.TempKey == Guid.Empty)
        //                 dlnm.TempKey = Guid.NewGuid();

        //             _context.DLNM_HRC2s.Update(dlnm);
        //         }

        //         // Lưu vào Map bằng RowKey (DUY NHẤT)
        //         dlnmMap[model.RowKey] = dlnm;
        //     }

        //     await _context.SaveChangesAsync();

        //     foreach (var model in models)
        //     {
        //         var dlnm = dlnmMap[model.RowKey];

        //         // XÓA phụ liệu cũ theo ID cha (an toàn tuyệt đối)
        //         var oldPL = await _context.PhuLieu_HRC2s
        //             .Where(x => x.ID_MeThoi == dlnm.ID)
        //             .ToListAsync();

        //         _context.PhuLieu_HRC2s.RemoveRange(oldPL);

        //         // THÊM phụ liệu mới
        //         foreach (var pl in model.hRC2_PhuLieus)
        //         {
        //             _context.PhuLieu_HRC2s.Add(new PhuLieu_HRC2
        //             {
        //                 BieuMau = model.BieuMau,
        //                 MeThoi = model.MeThoi,
        //                 ID_PhuLieu = pl.ID_PhuLieu,
        //                 TenPhuLieu = pl.TenPhuLieu,
        //                 KLPhuGia = pl.KLPhuGia,
        //                 ID_HeaderKey = pl.ID_HeaderKey,
        //                 TenHienThi = pl.TenHienThi,
        //                 ID_MeThoi = dlnm.ID  
        //             });
        //         }
        //     }

        //     // SAVE 2 — lưu phụ liệu
        //     await _context.SaveChangesAsync();
        // }
        public async Task SaveHRC2ManualDataAsync(List<HRC2InsertModel> models)
        {
            if (models == null || !models.Any())
                return;

            var dlnmMap = new Dictionary<Guid, DLNM_HRC2>(); // Key = RowKey

            foreach (var model in models)
            {
                // Tìm DLNM cha cũ (nếu có)
                DLNM_HRC2 existing = null;
                if (model.Id != null && model.Id > 0)
                {
                    existing = await _context.DLNM_HRC2s
                        .FirstOrDefaultAsync(x => x.ID == model.Id && x.IsNM == false);
                }

                // ====== KIỂM TRA TRÙNG MẺ THỎI (IsNM = false) ======
                var sameMeThoi = await _context.DLNM_HRC2s
                    .Where(x =>
                        x.MeThoi == model.MeThoi &&
                        x.BieuMau == model.BieuMau)
                    .ToListAsync();

                bool isTrung = false;

                if (existing == null)
                {
                    // INSERT → nếu có record khác trùng → trùng
                    if (sameMeThoi.Any())
                        isTrung = true;
                }
                else
                {
                    // UPDATE → loại bỏ chính nó khỏi danh sách kiểm tra
                    var others = sameMeThoi.Where(x => x.ID != existing.ID).ToList();
                    if (others.Any())
                        isTrung = true;
                }

                // ====== UPDATE IsTrungMeThoi CHO TẤT CẢ BẢN GHI TRÙNG ======
                if (isTrung)
                {
                    foreach (var item in sameMeThoi)
                        item.IsTrungMeThoi = true;

                    _context.DLNM_HRC2s.UpdateRange(sameMeThoi);
                }
                else
                {
                    // Nếu không trùng → reset toàn bộ về false
                    foreach (var item in sameMeThoi)
                        item.IsTrungMeThoi = false;

                    _context.DLNM_HRC2s.UpdateRange(sameMeThoi);
                }

                // ====== TẠO HOẶC UPDATE DLNM ======
                DLNM_HRC2 dlnm;

                if (existing == null)
                {
                    dlnm = new DLNM_HRC2
                    {
                        REPORT_NO = null,
                        NgaySx = model.Ngay,
                        Ngay = model.Ngay,
                        Ca = model.Ca,
                        BieuMau = model.BieuMau,
                        Scope = model.Scope,
                        MeThoi = model.MeThoi,
                        MacThep = model.MacThep,
                        O2 = model.O2,
                        N2 = model.N2,
                        AR_RH = model.AR_RH,
                        AR_LF = model.AR_LF,
                        AR_BOF = model.AR_BOF,
                        KLGangLong = model.KLGangLong,
                        KLThepPhe = model.KLThepPhe,
                        KLThepLong = model.KlThepLong,
                        IsNM = false,
                        IsChuyenCa = model.IsChuyenCa,
                        IsTrungMeThoi = isTrung,

                        TempKey = Guid.NewGuid()
                    };

                    await _context.DLNM_HRC2s.AddAsync(dlnm);
                }
                else
                {
                    dlnm = existing;

                    dlnm.MeThoi = model.MeThoi;
                    dlnm.MacThep = model.MacThep;
                    dlnm.O2 = model.O2;
                    dlnm.N2 = model.N2;
                    dlnm.AR_RH = model.AR_RH;
                    dlnm.AR_LF = model.AR_LF;
                    dlnm.AR_BOF = model.AR_BOF;
                    dlnm.KLGangLong = model.KLGangLong;
                    dlnm.KLThepPhe = model.KLThepPhe;
                    dlnm.KLThepLong = model.KlThepLong;
                    dlnm.IsChuyenCa = model.IsChuyenCa;
                    dlnm.NgaySx = model.Ngay;
                    dlnm.IsTrungMeThoi = isTrung;

                    if (dlnm.TempKey == Guid.Empty)
                        dlnm.TempKey = Guid.NewGuid();

                    _context.DLNM_HRC2s.Update(dlnm);
                }

                // Lưu map
                dlnmMap[model.RowKey] = dlnm;
            }

            // ====== SAVE 1 (lưu DLNM + IsTrung) ======
            await _context.SaveChangesAsync();

            // ====== XỬ LÝ PHỤ LIỆU ======
            foreach (var model in models)
            {
                var dlnm = dlnmMap[model.RowKey];

                // Xóa phụ liệu cũ
                var oldPL = await _context.PhuLieu_HRC2s
                    .Where(x => x.ID_MeThoi == dlnm.ID)
                    .ToListAsync();

                _context.PhuLieu_HRC2s.RemoveRange(oldPL);

                // Thêm mới
                foreach (var pl in model.hRC2_PhuLieus)
                {
                    _context.PhuLieu_HRC2s.Add(new PhuLieu_HRC2
                    {
                        BieuMau = model.BieuMau,
                        MeThoi = model.MeThoi,
                        ID_PhuLieu = pl.ID_PhuLieu,
                        TenPhuLieu = pl.TenPhuLieu,
                        KLPhuGia = pl.KLPhuGia,
                        ID_HeaderKey = pl.ID_HeaderKey,
                        TenHienThi = pl.TenHienThi,
                        ID_MeThoi = dlnm.ID
                    });
                }
            }

            // SAVE 2 — lưu phụ liệu
            await _context.SaveChangesAsync();
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



    }
}

