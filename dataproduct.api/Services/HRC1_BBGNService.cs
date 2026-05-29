using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;
using System.Text.Json;
using System.Collections.Generic;

namespace dataproduct.api.Services
{
    public class HRC1_BBGNService
    {
        private readonly IHRC1_BBGNRepository _repo;
        private readonly BBGN_ThepLongService _bbgnSvc;

        public HRC1_BBGNService(IHRC1_BBGNRepository repo, BBGN_ThepLongService bbgnSvc)
        {
            _repo = repo;
            _bbgnSvc = bbgnSvc;
        }

        // -------------------------------------------------------
        // GET — phiếu bất kỳ công đoạn (lo_thoi | tinh_luyen | duc)
        // loSo: lọc mẻ theo lò thổi (cho lo_thoi phiếu); scopePhieu: lọc theo TL scope (tinh_luyen); idMayDuc: override scope cho duc
        // -------------------------------------------------------
        public async Task<HRC1_PhieuDataVm> GetPhieuAsync(Guid idPhieu, int? loSo = null, int? scopePhieu = null, int? idMayDuc = null)
        {
            var phieu = await _repo.GetBmPhieuAsync(idPhieu)
                ?? throw new KeyNotFoundException($"Không tìm thấy phiếu {idPhieu}");

            var congDoan = phieu.MaBm switch
            {
                "HRC1_LoThoi"         => "lo_thoi",
                "HRC1_TinhLuyen"      => "tinh_luyen",
                "HRC1_BBGN_ThepLong"  => "duc",
                _                     => phieu.MaBm ?? string.Empty
            };

            var mayDucs = await _repo.GetMayDucsHRC1Async();

            List<HRC1_MeThepVm> danhSachMe;
            List<HRC1_MeThep> rawMes;

            if (congDoan == "duc")
            {
                var effectiveMayDuc = idMayDuc ?? phieu.Scope;
                rawMes = phieu.NgaySX.HasValue && phieu.Ca.HasValue && effectiveMayDuc.HasValue
                    ? await _repo.GetMeThepsByMayDucAsync(phieu.NgaySX.Value, phieu.Ca.Value, effectiveMayDuc.Value)
                    : new List<HRC1_MeThep>();

                danhSachMe = rawMes
                    .Select(m => MapToVm(m, new HRC1_MePhanCong { MeId = m.Id, IdPhieu = idPhieu, CongDoan = "duc" }, mayDucs))
                    .ToList();
            }
            else
            {
                // lo_thoi: lọc theo loSo; tinh_luyen: lọc theo scopePhieu; null = trả tất cả
                int? effectiveScope = congDoan == "lo_thoi" ? loSo : scopePhieu;
                var phanCongs = await _repo.GetMePhanCongsByPhieuAsync(idPhieu, congDoan, effectiveScope);
                var meIds     = phanCongs.Select(pc => pc.MeId).Distinct().ToList();
                rawMes        = await _repo.GetMeThepsByIdsAsync(meIds);
                var meDict    = rawMes.ToDictionary(m => m.Id);

                danhSachMe = phanCongs.Select(pc =>
                {
                    meDict.TryGetValue(pc.MeId, out var m);
                    return MapToVm(m, pc, mayDucs);
                }).ToList();

                if (congDoan == "lo_thoi" && meIds.Count > 0)
                {
                    var tlScopes = await _repo.GetTLScopesByMeIdsAsync(meIds);
                    foreach (var meVm in danhSachMe)
                        if (tlScopes.TryGetValue(meVm.Id, out var scope))
                            meVm.SoTinhLuyenNhan = scope;
                }
            }

            // Tra cứu tên người sửa cuối theo công đoạn
            var rawMeDict = rawMes.ToDictionary(m => m.Id);
            var auditIds = danhSachMe
                .Select(vm => congDoan switch
                {
                    "lo_thoi"    => rawMeDict.GetValueOrDefault(vm.Id)?.CapNhatBoi,
                    "tinh_luyen" => rawMeDict.GetValueOrDefault(vm.Id)?.CapNhatBoiTL,
                    "duc"        => rawMeDict.GetValueOrDefault(vm.Id)?.CapNhatBoiDuc,
                    _            => null
                })
                .Where(id => id.HasValue).Select(id => id!.Value)
                .Distinct().ToList();

            var userNames = auditIds.Count > 0
                ? await _repo.GetUserNamesByIdsAsync(auditIds)
                : new Dictionary<int, string?>();

            foreach (var meVm in danhSachMe)
            {
                var raw = rawMeDict.GetValueOrDefault(meVm.Id);
                int? uid = congDoan switch
                {
                    "lo_thoi"    => raw?.CapNhatBoi,
                    "tinh_luyen" => raw?.CapNhatBoiTL,
                    "duc"        => raw?.CapNhatBoiDuc,
                    _            => null
                };
                if (uid.HasValue && userNames.TryGetValue(uid.Value, out var name))
                    meVm.TenCapNhatBoi = name;
            }

            var vm = new HRC1_PhieuDataVm
            {
                IdPhieu      = phieu.Idphieu,
                SoPhieu      = phieu.SoPhieu,
                MaBm         = phieu.MaBm,
                CongDoan     = congDoan,
                Scope        = phieu.Scope,
                NgaySX       = phieu.NgaySX,
                Ca           = phieu.Ca,
                Kip          = phieu.Kip,
                TinhTrang    = phieu.TinhTrang,
                DanhSachMe   = danhSachMe,
                DanhSachMayDuc = mayDucs.Select(md => new HRC1_MayDucOptionVm
                {
                    Id        = md.Id,
                    TenMayDuc = md.TenMayDuc ?? string.Empty
                }).ToList()
            };

            if (congDoan == "tinh_luyen")
                vm.ChoNhan = (await _repo.GetChoNhanAsync())
                    .Select(m => new HRC1_ChoNhanMeVm
                    {
                        MeId        = m.Id,
                        MaMe        = m.MaMe,
                        ThungSo     = m.ThungSo,
                        LoSo        = m.LoSo,
                        ThoiGian    = m.ThoiGian,
                        KlThepLong  = m.KlThepLong,
                        TLDichSo    = m.TLDichSo
                    }).ToList();

            return vm;
        }

        // -------------------------------------------------------
        // GET — danh sách mẻ chờ TL nhận có filter + phân trang
        // -------------------------------------------------------
        public Task<HRC1_PagedResult<HRC1_ChoNhanMeVm>> GetMeChoNhanPagedAsync(HRC1_GetMeChoNhanQuery q) =>
            _repo.GetMeChoNhanPagedAsync(q.TuNgay, q.DenNgay, q.Ca, q.MaMe, q.ThungSo, q.LoSo, q.Page, q.PageSize);

        // -------------------------------------------------------
        // GET — danh sách mẻ chờ TL nhận (dùng riêng nếu cần)
        // -------------------------------------------------------
        public async Task<List<HRC1_ChoNhanMeVm>> GetChoNhanAsync() =>
            (await _repo.GetChoNhanAsync())
                .Select(m => new HRC1_ChoNhanMeVm
                {
                    MeId        = m.Id,
                    MaMe        = m.MaMe,
                    ThungSo     = m.ThungSo,
                    LoSo        = m.LoSo,
                    ThoiGian    = m.ThoiGian,
                    KlThepLong  = m.KlThepLong,
                    TLDichSo    = m.TLDichSo
                }).ToList();

        // -------------------------------------------------------
        // LÒ THỔI
        // -------------------------------------------------------
        public async Task UpdateMeAsync(int meId, HRC1_LoThoiUpdateRequest req, int userId)
        {
            var me = await _repo.GetMeByIdAsync(meId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ {meId}");
            if (me.TrangThaiLo >= 1)
                throw new InvalidOperationException("Mẻ đã xác nhận, không thể chỉnh sửa.");

            // Nếu TL đã nhận mẻ thì lò thổi không được chuyển sang lên thẳng nữa
            if (req.DichChuyen == "len_thang" && (me.TrangThaiTL ?? 0) >= 1)
                throw new InvalidOperationException("Tinh luyện đã nhận mẻ này. Chỉ có thể chọn tinh luyện để tham khảo, không thể chuyển sang lên thẳng.");

            var oldDich = me.DichChuyen;
            var old = Snapshot(me);

            me.ThungSo        = req.ThungSo        ?? me.ThungSo;
            me.KLLFSauThep    = req.KLLFSauThep    ?? me.KLLFSauThep;
            me.KlLan3         = req.KlLan3         ?? me.KlLan3;
            if (req.DichChuyen is not null)
            {
                var previouslyLenThang = me.DichChuyen == "len_thang";
                me.DichChuyen = req.DichChuyen;

                if (req.DichChuyen == "len_thang")
                {
                    // Lên thẳng: lò thổi chỉ định máy đúc trực tiếp; xóa TL đích
                    me.IdMayDucDich = req.IdMayDucDich;
                    me.TLDichSo     = null;
                }
                else // "tinh_luyen"
                {
                    me.TLDichSo = req.TLDichSo;
                    // IdMayDucDich thuộc về tinh luyện — không ghi đè.
                    // Ngoại lệ: nếu trước đó là len_thang thì reset để TL tự chọn lại.
                    if (previouslyLenThang)
                        me.IdMayDucDich = null;
                }
            }
            else
            {
                me.TLDichSo     = req.TLDichSo     ?? me.TLDichSo;
                me.IdMayDucDich = req.IdMayDucDich ?? me.IdMayDucDich;
            }
            // Lò thổi tự nhập ThoiGian, KlLan2 & KlThepLong khi mẻ đi thẳng lên máy đúc (không qua TL)
            if (me.DichChuyen == "len_thang")
            {
                me.ThoiGian   = req.ThoiGian   ?? me.ThoiGian;
                me.KlLan2     = req.KlLan2     ?? me.KlLan2;
                me.KlThepLong = req.KlThepLong ?? me.KlThepLong;
            }
            me.IsThuNghiem    = req.IsThuNghiem    ?? me.IsThuNghiem;
            me.IsTrungMeThoi  = req.IsTrungMeThoi  ?? me.IsTrungMeThoi;
            me.GhiChuLo       = req.GhiChuLo       ?? me.GhiChuLo;
            me.CapNhatBoi     = userId;
            me.CapNhatLuc     = DateTime.Now;

            _repo.AddLichSu(new HRC1_LichSu
            {
                MeId       = me.Id,
                TaiKhoanId = userId,
                HanhDong   = "chinh_sua",
                DuLieuCu   = old,
                DuLieuMoi  = Snapshot(me),
                Luc        = DateTime.Now
            });
            await _repo.SaveChangesAsync();

            // Nếu trạng thái len_thang thay đổi (thêm vào hoặc bỏ ra), recalc IsTrungMeThoi
            bool lenThangChanged = (oldDich == "len_thang") != (me.DichChuyen == "len_thang");
            if (lenThangChanged && !string.IsNullOrEmpty(me.MaMe))
            {
                await RecalcTrungMeByMaMeAsync(me.MaMe);
                await _repo.SaveChangesAsync();
            }
        }

        public async Task XacNhanLoThoiAsync(int meId, int userId)
        {
            var me = await _repo.GetMeByIdAsync(meId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ {meId}");
            if (me.TrangThaiLo >= 1)
                throw new InvalidOperationException("Mẻ đã xác nhận.");

            me.TrangThaiLo = 1;
            me.CapNhatBoi  = userId;
            me.CapNhatLuc  = DateTime.Now;

            _repo.AddLichSu(new HRC1_LichSu
            {
                MeId       = me.Id,
                TaiKhoanId = userId,
                HanhDong   = "xac_nhan",
                DuLieuMoi  = Snapshot(me),
                Luc        = DateTime.Now
            });
            await _repo.SaveChangesAsync();
        }

        public async Task BoXacNhanLoThoiAsync(int meId, int userId)
        {
            var me = await _repo.GetMeByIdAsync(meId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ {meId}");
            if (me.TrangThaiLo != 1)
                throw new InvalidOperationException("Mẻ chưa ở trạng thái xác nhận.");
            if (me.TrangThaiTL >= 1)
                throw new InvalidOperationException("TL đã nhận mẻ, không thể bỏ xác nhận.");

            me.TrangThaiLo = 0;
            me.CapNhatBoi  = userId;
            me.CapNhatLuc  = DateTime.Now;

            _repo.AddLichSu(new HRC1_LichSu
            {
                MeId       = me.Id,
                TaiKhoanId = userId,
                HanhDong   = "bo_xac_nhan",
                DuLieuMoi  = Snapshot(me),
                Luc        = DateTime.Now
            });
            await _repo.SaveChangesAsync();
        }

        public async Task LamMoiAsync(int meId, int userId)
        {
            var me = await _repo.GetMeByIdAsync(meId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ {meId}");
            if (me.TrangThaiTL >= 1 || me.TrangThaiDuc >= 1)
                throw new InvalidOperationException("TL hoặc đúc đã xác nhận, không thể làm mới.");

            var old = Snapshot(me);

            me.KLLFSauThep   = null;
            me.KlLan3        = null;
            me.DichChuyen    = null;
            me.TLDichSo      = null;
            me.IdMayDucDich  = null;
            me.IsThuNghiem   = null;
            me.IsTrungMeThoi = null;
            me.GhiChuLo      = null;
            me.TrangThaiLo   = null;
            me.CapNhatBoi    = userId;
            me.CapNhatLuc    = DateTime.Now;

            _repo.AddLichSu(new HRC1_LichSu
            {
                MeId       = me.Id,
                TaiKhoanId = userId,
                HanhDong   = "lam_moi",
                DuLieuCu   = old,
                DuLieuMoi  = Snapshot(me),
                Luc        = DateTime.Now
            });
            await _repo.SaveChangesAsync();
        }

        // -------------------------------------------------------
        // TINH LUYỆN
        // -------------------------------------------------------
        public async Task NhanMeAsync(HRC1_NhanMeRequest req, int userId)
        {
            var me = await _repo.GetMeByIdAsync(req.MeId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ {req.MeId}");

            if (me.DichChuyen == "len_thang")
                throw new InvalidOperationException("Lò thổi đã chỉ định mẻ này lên thẳng máy đúc, không thể nhận vào tinh luyện.");

            if (await _repo.ExistsMePhanCongAsync(req.MeId, "tinh_luyen"))
                throw new InvalidOperationException("Mẻ đã được nhận vào tinh luyện.");

            // Ghi nhận thời điểm TL nhận mẻ theo ngày/ca của phiếu TL.
            // GetMeThepsByMayDucAsync dùng NgayNhanTL để lọc ca cho mẻ tinh_luyen,
            // giữ nguyên NgayTao (ngày tạo thực tế từ sync gang lỏng).
            var phieuTL = await _repo.GetBmPhieuAsync(req.IdPhieu);
            if (phieuTL?.NgaySX.HasValue == true && phieuTL.Ca.HasValue)
            {
                me.NgayNhanTL = phieuTL.Ca.Value == 1
                    ? phieuTL.NgaySX.Value.ToDateTime(new TimeOnly(6, 0))
                    : phieuTL.NgaySX.Value.ToDateTime(new TimeOnly(18, 0));
            }

            _repo.AddMePhanCong(new HRC1_MePhanCong
            {
                MeId       = req.MeId,
                IdPhieu    = req.IdPhieu,
                CongDoan   = "tinh_luyen",
                ThuTuTL    = null,
                ScopePhieu = req.ScopePhieu
            });

            me.TrangThaiTL   = 1;
            me.CapNhatBoi    = userId;
            me.CapNhatLuc    = DateTime.Now;
            me.CapNhatBoiTL  = userId;

            _repo.AddLichSu(new HRC1_LichSu
            {
                MeId       = me.Id,
                TaiKhoanId = userId,
                HanhDong   = "nhan_me",
                DuLieuMoi  = Snapshot(me),
                Luc        = DateTime.Now
            });
            await _repo.SaveChangesAsync();

            // Recalc trùng: mẻ vừa được TL nhận có thể conflict với len_thang hoặc TL khác
            await RecalcTrungMeByMaMeAsync(me.MaMe);
            await _repo.SaveChangesAsync();
        }

        public async Task UpdateMePhanCongAsync(int mePhanCongId, HRC1_TinhLuyenUpdateRequest req, int userId)
        {
            var pc = await _repo.GetMePhanCongByIdAsync(mePhanCongId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ phân công {mePhanCongId}");

            var me = await _repo.GetMeByIdAsync(pc.MeId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ {pc.MeId}");
            if (me.TrangThaiDuc >= 1)
                throw new InvalidOperationException("Mẻ đã được máy đúc xác nhận, không thể chỉnh sửa.");

            var old = Snapshot(me);

            me.ThoiGian      = req.ThoiGian      ?? me.ThoiGian;
            me.KlLan1        = req.KlLan1        ?? me.KlLan1;
            me.KlLan2        = req.KlLan2        ?? me.KlLan2;
            me.KlThepLong    = req.KlThepLong    ?? me.KlThepLong;
            me.IdMayDucDich  = req.IdMayDucDich  ?? me.IdMayDucDich;
            me.PhanLoai      = req.PhanLoai      ?? me.PhanLoai;
            me.MacThep       = req.MacThep       ?? me.MacThep;
            me.MacThepBKMIS  = req.MacThepBKMIS  ?? me.MacThepBKMIS;
            me.IdMacThep     = req.IdMacThep     ?? me.IdMacThep;
            me.GhiChuTL      = req.GhiChuTL      ?? me.GhiChuTL;
            me.CapNhatBoi    = userId;
            me.CapNhatLuc    = DateTime.Now;
            me.CapNhatBoiTL  = userId;

            _repo.AddLichSu(new HRC1_LichSu
            {
                MeId       = me.Id,
                TaiKhoanId = userId,
                HanhDong   = "chinh_sua",
                DuLieuCu   = old,
                DuLieuMoi  = Snapshot(me),
                Luc        = DateTime.Now
            });
            await _repo.SaveChangesAsync();
        }

        public async Task ThemDongAsync(HRC1_ThemDongTLRequest req, int userId)
        {
            var me = await _repo.GetMeByIdAsync(req.MeId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ {req.MeId}");
            if ((me.TrangThaiTL ?? 0) < 1)
                throw new InvalidOperationException("Mẻ phải được nhận vào tinh luyện trước khi thêm dòng.");

            var phieu = await _repo.GetBmPhieuAsync(req.IdPhieu)
                ?? throw new KeyNotFoundException($"Không tìm thấy phiếu {req.IdPhieu}");

            int nextThuTu = await _repo.GetMaxThuTuTLAsync(req.MeId) + 1;

            _repo.AddMePhanCong(new HRC1_MePhanCong
            {
                MeId     = req.MeId,
                IdPhieu  = req.IdPhieu,
                CongDoan = "tinh_luyen",
                ThuTuTL  = nextThuTu
            });

            _repo.AddLichSu(new HRC1_LichSu
            {
                MeId       = me.Id,
                TaiKhoanId = userId,
                HanhDong   = "chinh_sua",
                DuLieuMoi  = $"{{\"them_dong_tl\":{nextThuTu}}}",
                Luc        = DateTime.Now
            });
            await _repo.SaveChangesAsync();
        }

        public async Task HuyNhanMeAsync(HRC1_HuyNhanMeRequest req, int userId)
        {
            var me = await _repo.GetMeByIdAsync(req.MeId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ {req.MeId}");

            if (me.IsManualTL == true)
                throw new InvalidOperationException("Dùng chức năng xóa mẻ tay để xóa mẻ thêm thủ công.");

            if ((me.TrangThaiTL ?? 0) < 1)
                throw new InvalidOperationException("Mẻ chưa được nhận vào tinh luyện.");

            if ((me.TrangThaiDuc ?? 0) >= 1)
                throw new InvalidOperationException("Máy đúc đã xác nhận mẻ này, không thể hủy nhận.");

            var old = Snapshot(me);

            var pcs = await _repo.GetTLPhanCongsByMePhieuAsync(req.MeId, req.IdPhieu, req.ScopePhieu);
            if (!pcs.Any())
                throw new InvalidOperationException("Không tìm thấy dòng nhận nào để hủy.");
            _repo.RemoveMePhanCongs(pcs);

            me.TrangThaiTL   = null;
            me.NgayNhanTL    = null;
            me.ThoiGian      = null;
            me.KlLan1        = null;
            me.KlLan2        = null;
            me.KlThepLong    = null;
            me.PhanLoai      = null;
            me.MacThep       = null;
            me.MacThepBKMIS  = null;
            me.IdMacThep     = null;
            me.GhiChuTL      = null;
            me.IsTrungMeThoi = null;   // mẻ này rời khỏi TL — không còn là bên trùng
            // if (me.DichChuyen == "tinh_luyen")
                me.IdMayDucDich = null;
            me.CapNhatBoi    = userId;
            me.CapNhatLuc    = DateTime.Now;
            me.CapNhatBoiTL  = userId;

            _repo.AddLichSu(new HRC1_LichSu
            {
                MeId       = me.Id,
                TaiKhoanId = userId,
                HanhDong   = "huy_nhan_me",
                DuLieuCu   = old,
                DuLieuMoi  = Snapshot(me),
                Luc        = DateTime.Now
            });
            await _repo.SaveChangesAsync();

            // Recalc trùng cho các mẻ cùng MaMe còn lại (bao gồm cả len_thang)
            await RecalcTrungMeByMaMeAsync(me.MaMe);
            await _repo.SaveChangesAsync();
        }

        // -------------------------------------------------------
        // MÁY ĐÚC
        // -------------------------------------------------------
        public async Task XacNhanDucAsync(HRC1_DucXacNhanRequest req, int userId)
        {
            if (req.MeIds.Count == 0) return;

            var meTheps = await _repo.GetMeThepsByIdsAsync(req.MeIds);
            var now = DateTime.Now;

            foreach (var me in meTheps)
            {
                if (me.TrangThaiDuc >= 1) continue;

                me.TrangThaiDuc  = 1;
                me.CapNhatBoi    = userId;
                me.CapNhatLuc    = now;
                me.CapNhatBoiDuc = userId;

                _repo.AddLichSu(new HRC1_LichSu
                {
                    MeId       = me.Id,
                    TaiKhoanId = userId,
                    HanhDong   = "xac_nhan",
                    DuLieuMoi  = Snapshot(me),
                    Luc        = now
                });
            }
            await _repo.SaveChangesAsync();
        }

        public async Task BoXacNhanDucAsync(HRC1_DucBoXacNhanRequest req, int userId)
        {
            if (req.MeIds.Count == 0) return;

            var meTheps = await _repo.GetMeThepsByIdsAsync(req.MeIds);
            var now = DateTime.Now;

            foreach (var me in meTheps)
            {
                if (me.TrangThaiDuc != 1) continue;

                me.TrangThaiDuc  = 0;
                me.CapNhatBoi    = userId;
                me.CapNhatLuc    = now;
                me.CapNhatBoiDuc = userId;

                _repo.AddLichSu(new HRC1_LichSu
                {
                    MeId       = me.Id,
                    TaiKhoanId = userId,
                    HanhDong   = "bo_xac_nhan",
                    DuLieuMoi  = Snapshot(me),
                    Luc        = now
                });
            }
            await _repo.SaveChangesAsync();
        }

        // -------------------------------------------------------
        // Đồng bộ mẻ thổi từ gang lỏng — chỉ dành cho phiếu lò thổi
        // loSo: lò thổi số cần đồng bộ (1–5)
        // -------------------------------------------------------
        public async Task<HRC1_PhieuDataVm> SyncMeThoiLoThoiAsync(Guid idPhieu, int loSo)
        {
            var phieu = await _repo.GetBmPhieuAsync(idPhieu)
                ?? throw new KeyNotFoundException($"Không tìm thấy phiếu {idPhieu}");
            if (phieu.MaBm != "HRC1_LoThoi")
                throw new InvalidOperationException("Chỉ đồng bộ được cho phiếu lò thổi.");

            // Lấy danh sách mẻ từ gang lỏng (lọc theo loSo thay vì phieu.Scope)
            List<string> gangMaMes;
            try
            {
                gangMaMes = await _bbgnSvc.FetchMeThoiHRC1Async(new HRC1_FetchMeThoiRequest
                {
                    NgaySX   = phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today),
                    Ca       = phieu.Ca ?? 1,
                    IdLoThoi = loSo
                });
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"Không kết nối được hệ thống gang lỏng: {ex.Message}", ex);
            }

            // Lấy mẻ hiện có trong phiếu cho lò này (filter by ScopePhieu = loSo)
            var phanCongs   = await _repo.GetMePhanCongsByPhieuAsync(idPhieu, "lo_thoi", loSo);
            var existingIds = phanCongs.Select(pc => pc.MeId).Distinct().ToList();
            var existingMes = existingIds.Count > 0
                ? await _repo.GetMeThepsByIdsAsync(existingIds)
                : new List<HRC1_MeThep>();

            var gangSet        = new HashSet<string>(gangMaMes, StringComparer.OrdinalIgnoreCase);
            var existingByMaMe = existingMes
                .Where(m => !string.IsNullOrEmpty(m.MaMe))
                .ToDictionary(m => m.MaMe!, StringComparer.OrdinalIgnoreCase);

            // 1. Mẻ gang trả lại → reset ghost
            foreach (var me in existingMes.Where(m =>
                !string.IsNullOrEmpty(m.MaMe) && gangSet.Contains(m.MaMe!) && m.IsGhost == true))
                me.IsGhost = false;

            // 2. Mẻ không còn bên gang → đánh ghost
            foreach (var me in existingMes.Where(m =>
                !string.IsNullOrEmpty(m.MaMe) && !gangSet.Contains(m.MaMe!) && m.IsGhost != true))
                me.IsGhost = true;

            // 3. Mẻ mới từ gang chưa có → insert, NgayTao = thời điểm bắt đầu ca
            var ngayTaoMe = phieu.NgaySX.HasValue && phieu.Ca.HasValue
                ? (phieu.Ca.Value == 1
                    ? phieu.NgaySX.Value.ToDateTime(new TimeOnly(6, 0))
                    : phieu.NgaySX.Value.ToDateTime(new TimeOnly(18, 0)))
                : DateTime.Now;

            var newMes = gangMaMes
                .Where(ma => !existingByMaMe.ContainsKey(ma))
                .Select(ma => new HRC1_MeThep { MaMe = ma, LoSo = loSo, NgayTao = ngayTaoMe })
                .ToList();

            foreach (var me in newMes)
                _repo.AddMeThep(me);

            await _repo.SaveChangesAsync();

            foreach (var me in newMes)
                _repo.AddMePhanCong(new HRC1_MePhanCong
                {
                    MeId       = me.Id,
                    IdPhieu    = idPhieu,
                    CongDoan   = "lo_thoi",
                    ScopePhieu = loSo
                });

            if (newMes.Count > 0)
                await _repo.SaveChangesAsync();

            return await GetPhieuAsync(idPhieu, loSo);
        }

        // -------------------------------------------------------
        // THÊM/XÓA MẺ TAY (Tinh luyện thêm thủ công)
        // -------------------------------------------------------

        public Task<List<HRC1_MeThepSearchVm>> SearchMeThepAsync(string q) =>
            _repo.SearchMeThepAsync(q.Trim(), 20)
                .ContinueWith(t => t.Result.Select(m => new HRC1_MeThepSearchVm
                {
                    MeId    = m.Id,
                    MaMe    = m.MaMe ?? string.Empty,
                    ThungSo = m.ThungSo,
                    LoSo    = m.LoSo
                }).ToList());

        public async Task<HRC1_ThemMeTayResult> ThemMeTayAsync(HRC1_ThemMeTayRequest req, int userId)
        {
            // Lấy mẻ gốc để copy thông tin cơ bản (MaMe, ThungSo, LoSo)
            var source = await _repo.GetMeByMaMeAsync(req.MaMe)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ '{req.MaMe}'.");

            // Check trùng: phiếu TL đã nhận + mẻ đang lên thẳng máy đúc
            var trungTL       = await _repo.GetAllTinhLuyenPhieuByMaMeAsync(req.MaMe);
            var trungLenThang = await _repo.GetLenThangMesByMaMeAsync(req.MaMe);
            var trungInfos = trungTL
                .Concat(trungLenThang.Select(m => new HRC1_TrungMeInfo
                {
                    SoPhieu      = m.MaMe ?? $"ME-{m.Id}",
                    TenTinhLuyen = "Lên thẳng máy đúc"
                }))
                .ToList();
            if (trungInfos.Count > 0 && !req.XacNhanTrung)
                return new HRC1_ThemMeTayResult { TrungVoi = trungInfos, DaThemVao = false };

            var phieu = await _repo.GetBmPhieuAsync(req.IdPhieu)
                ?? throw new KeyNotFoundException($"Không tìm thấy phiếu {req.IdPhieu}.");

            var ngayNhanTL = (phieu.NgaySX.HasValue && phieu.Ca.HasValue)
                ? (phieu.Ca.Value == 1
                    ? phieu.NgaySX.Value.ToDateTime(new TimeOnly(6, 0))
                    : phieu.NgaySX.Value.ToDateTime(new TimeOnly(18, 0)))
                : DateTime.Now;

            // Tạo HRC1_MeThep mới — độc lập với mẻ gốc lò thổi
            var newMe = new HRC1_MeThep
            {
                MaMe         = source.MaMe,
                ThungSo      = source.ThungSo,
                LoSo         = source.LoSo,
                IsManualTL   = true,
                TrangThaiTL  = 1,
                NgayNhanTL   = ngayNhanTL,
                NgayTao      = ngayNhanTL,
                CapNhatBoi   = userId,
                CapNhatLuc   = DateTime.Now,
                CapNhatBoiTL = userId
            };
            _repo.AddMeThep(newMe);
            await _repo.SaveChangesAsync(); // EF gán Id cho newMe

            _repo.AddMePhanCong(new HRC1_MePhanCong
            {
                MeId       = newMe.Id,
                IdPhieu    = req.IdPhieu,
                CongDoan   = "tinh_luyen",
                IsManualTL = true,
                ScopePhieu = req.ScopePhieu
            });

            _repo.AddLichSu(new HRC1_LichSu
            {
                MeId       = newMe.Id,
                TaiKhoanId = userId,
                HanhDong   = "them_me_tay",
                DuLieuMoi  = Snapshot(newMe),
                Luc        = DateTime.Now
            });
            await _repo.SaveChangesAsync();

            // Recalc IsTrungMeThoi cho tất cả mẻ có cùng MaMe (TL + len_thang)
            await RecalcTrungMeByMaMeAsync(source.MaMe);
            await _repo.SaveChangesAsync();

            return new HRC1_ThemMeTayResult { TrungVoi = trungInfos, DaThemVao = true };
        }

        public async Task XoaMeTayAsync(int mePhanCongId, int userId)
        {
            var pc = await _repo.GetMePhanCongByIdAsync(mePhanCongId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ phân công {mePhanCongId}.");
            if (pc.IsManualTL != true)
                throw new InvalidOperationException("Chỉ có thể xóa dòng được thêm thủ công.");

            var me = await _repo.GetMeByIdAsync(pc.MeId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ {pc.MeId}.");
            if (me.IsManualTL != true)
                throw new InvalidOperationException("Mẻ này không phải do tinh luyện tạo tay.");
            if ((me.TrangThaiDuc ?? 0) >= 1)
                throw new InvalidOperationException("Mẻ đã được máy đúc xác nhận, không thể xóa.");

            var maMe = me.MaMe;

            // Xóa toàn bộ MePhanCong và chính HRC1_MeThep (mẻ này độc lập, do TL tạo)
            var allPcs = await _repo.GetAllMePhanCongsByMeIdAsync(me.Id);
            _repo.RemoveMePhanCongs(allPcs);
            _repo.RemoveMeThep(me);
            await _repo.SaveChangesAsync();

            // Recalc trùng cho các mẻ cùng MaMe còn lại (bao gồm cả len_thang)
            await RecalcTrungMeByMaMeAsync(maMe);
            await _repo.SaveChangesAsync();
        }

        // -------------------------------------------------------
        // MÁY ĐÚC — CHỐT / BỎ CHỐT MẺ (P.KH)
        // -------------------------------------------------------
        public async Task ChotMeAsync(HRC1_DucChotMeRequest req, int userId)
        {
            if (req.MeIds.Count == 0) return;
            var meTheps = await _repo.GetMeThepsByIdsAsync(req.MeIds);
            var now = DateTime.Now;

            foreach (var me in meTheps)
            {
                if (me.IsChot == true) continue;
                me.IsChot        = true;
                me.CapNhatBoi    = userId;
                me.CapNhatLuc    = now;
                me.CapNhatBoiDuc = userId;
                _repo.AddLichSu(new HRC1_LichSu
                {
                    MeId       = me.Id,
                    TaiKhoanId = userId,
                    HanhDong   = "chot",
                    DuLieuMoi  = Snapshot(me),
                    Luc        = now
                });
            }
            await _repo.SaveChangesAsync();
            await UpdateDucPhieuTinhTrangAsync(req.IdPhieu, req.IdMayDuc);
        }

        public async Task BoChotMeAsync(HRC1_DucBoChotMeRequest req, int userId)
        {
            if (req.MeIds.Count == 0) return;
            var meTheps = await _repo.GetMeThepsByIdsAsync(req.MeIds);
            var now = DateTime.Now;

            foreach (var me in meTheps)
            {
                if (me.IsChot != true) continue;
                me.IsChot        = false;
                me.CapNhatBoi    = userId;
                me.CapNhatLuc    = now;
                me.CapNhatBoiDuc = userId;
                _repo.AddLichSu(new HRC1_LichSu
                {
                    MeId       = me.Id,
                    TaiKhoanId = userId,
                    HanhDong   = "bo_chot",
                    DuLieuMoi  = Snapshot(me),
                    Luc        = now
                });
            }
            await _repo.SaveChangesAsync();
            await UpdateDucPhieuTinhTrangAsync(req.IdPhieu, req.IdMayDuc);
        }

        private async Task UpdateDucPhieuTinhTrangAsync(Guid idPhieu, int idMayDuc)
        {
            var phieu = await _repo.GetBmPhieuAsync(idPhieu);
            if (phieu == null || !phieu.NgaySX.HasValue || !phieu.Ca.HasValue) return;

            var allMes = await _repo.GetMeThepsByMayDucAsync(phieu.NgaySX.Value, phieu.Ca.Value, idMayDuc);
            if (allMes.Count == 0) return;

            int newTinhTrang = allMes.All(m => m.IsChot == true) ? 5
                             : allMes.Any(m => (m.TrangThaiDuc ?? 0) >= 1) ? 2
                             : 0;

            if (phieu.TinhTrang != newTinhTrang)
            {
                phieu.TinhTrang = newTinhTrang;
                await _repo.SaveChangesAsync();
            }
        }

        // -------------------------------------------------------
        // CHỐT/HỦY CHỐT PHIẾU BATCH — dành cho P.KH chốt từ danh sách ThongKe
        // Điều kiện: tất cả mẻ trong phiếu phải đã được máy đúc xác nhận (TrangThaiDuc >= 1)
        // -------------------------------------------------------
        public async Task<HRC1_ChotPhieuBatchResult> ChotPhieuBatchAsync(List<Guid> idPhieuList, int userId)
        {
            var result = new HRC1_ChotPhieuBatchResult();
            var now = DateTime.Now;

            foreach (var idPhieu in idPhieuList)
            {
                var phieu = await _repo.GetBmPhieuAsync(idPhieu);
                if (phieu == null || !phieu.NgaySX.HasValue || !phieu.Ca.HasValue || !phieu.Scope.HasValue)
                {
                    result.ThatBai.Add(new HRC1_ChotPhieuBatchThatBai
                    {
                        IdPhieu = idPhieu,
                        SoPhieu = phieu?.SoPhieu ?? idPhieu.ToString(),
                        LyDo = new List<string> { "Phiếu không hợp lệ hoặc thiếu thông tin" }
                    });
                    continue;
                }

                var mes = await _repo.GetMeThepsByMayDucAsync(phieu.NgaySX.Value, phieu.Ca.Value, phieu.Scope.Value);

                if (mes.Count == 0)
                {
                    result.ThatBai.Add(new HRC1_ChotPhieuBatchThatBai
                    {
                        IdPhieu = idPhieu,
                        SoPhieu = phieu.SoPhieu ?? idPhieu.ToString(),
                        LyDo = new List<string> { "Phiếu chưa có mẻ nào" }
                    });
                    continue;
                }

                var chuaXacNhan = mes.Where(m => (m.TrangThaiDuc ?? 0) < 1).ToList();
                if (chuaXacNhan.Count > 0)
                {
                    var dsMa = string.Join(", ", chuaXacNhan.Take(5).Select(m => m.MaMe ?? $"ID:{m.Id}"));
                    result.ThatBai.Add(new HRC1_ChotPhieuBatchThatBai
                    {
                        IdPhieu = idPhieu,
                        SoPhieu = phieu.SoPhieu ?? idPhieu.ToString(),
                        LyDo = new List<string> { $"{chuaXacNhan.Count} mẻ chưa xác nhận: {dsMa}" }
                    });
                    continue;
                }

                foreach (var me in mes)
                {
                    if (me.IsChot == true) continue;
                    me.IsChot = true;
                    me.CapNhatBoi = userId;
                    me.CapNhatLuc = now;
                    me.CapNhatBoiDuc = userId;
                    _repo.AddLichSu(new HRC1_LichSu
                    {
                        MeId = me.Id, TaiKhoanId = userId,
                        HanhDong = "chot", DuLieuMoi = Snapshot(me), Luc = now
                    });
                }
                await _repo.SaveChangesAsync();
                await UpdateDucPhieuTinhTrangAsync(idPhieu, phieu.Scope.Value);

                result.ThanhCong.Add(phieu.SoPhieu ?? idPhieu.ToString());
            }

            return result;
        }

        public async Task<HRC1_ChotPhieuBatchResult> HuyChotPhieuBatchAsync(List<Guid> idPhieuList, int userId)
        {
            var result = new HRC1_ChotPhieuBatchResult();
            var now = DateTime.Now;

            foreach (var idPhieu in idPhieuList)
            {
                var phieu = await _repo.GetBmPhieuAsync(idPhieu);
                if (phieu == null || !phieu.NgaySX.HasValue || !phieu.Ca.HasValue || !phieu.Scope.HasValue)
                {
                    result.ThatBai.Add(new HRC1_ChotPhieuBatchThatBai
                    {
                        IdPhieu = idPhieu,
                        SoPhieu = phieu?.SoPhieu ?? idPhieu.ToString(),
                        LyDo = new List<string> { "Phiếu không hợp lệ hoặc thiếu thông tin" }
                    });
                    continue;
                }

                var mes = await _repo.GetMeThepsByMayDucAsync(phieu.NgaySX.Value, phieu.Ca.Value, phieu.Scope.Value);

                foreach (var me in mes.Where(m => m.IsChot == true))
                {
                    me.IsChot = false;
                    me.CapNhatBoi = userId;
                    me.CapNhatLuc = now;
                    me.CapNhatBoiDuc = userId;
                    _repo.AddLichSu(new HRC1_LichSu
                    {
                        MeId = me.Id, TaiKhoanId = userId,
                        HanhDong = "bo_chot", DuLieuMoi = Snapshot(me), Luc = now
                    });
                }
                await _repo.SaveChangesAsync();
                await UpdateDucPhieuTinhTrangAsync(idPhieu, phieu.Scope.Value);

                result.ThanhCong.Add(phieu.SoPhieu ?? idPhieu.ToString());
            }

            return result;
        }

        // -------------------------------------------------------
        // GHI CHÚ — cập nhật ghi chú chung, dùng cho cả 3 công đoạn
        // -------------------------------------------------------
        public async Task UpdateGhiChuAsync(int meId, string? ghiChu, int userId)
        {
            var me = await _repo.GetMeByIdAsync(meId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ {meId}");
            if (me.IsChot == true)
                throw new InvalidOperationException("Mẻ đã chốt, không thể chỉnh sửa.");

            me.GhiChuLo   = ghiChu;
            me.CapNhatBoi = userId;
            me.CapNhatLuc = DateTime.Now;
            await _repo.SaveChangesAsync();
        }

        // Xóa cứng mẻ ghost (user chủ động xóa sau khi xác nhận bên gang thực sự xóa)
        public async Task XoaMeGhostAsync(int meId)
        {
            var me = await _repo.GetMeByIdAsync(meId)
                ?? throw new KeyNotFoundException($"Không tìm thấy mẻ {meId}");
            if (me.IsGhost != true)
                throw new InvalidOperationException("Chỉ có thể xóa mẻ đã được đánh dấu ghost.");
            if ((me.TrangThaiTL ?? 0) >= 1)
                throw new InvalidOperationException("Không thể xóa mẻ: tinh luyện đã nhận mẻ này.");
            if ((me.TrangThaiDuc ?? 0) >= 1)
                throw new InvalidOperationException("Không thể xóa mẻ: máy đúc đã xác nhận mẻ này.");

            var pcs = await _repo.GetAllMePhanCongsByMeIdAsync(meId);
            _repo.RemoveMePhanCongs(pcs);
            _repo.RemoveMeThep(me);
            await _repo.SaveChangesAsync();
        }

        // -------------------------------------------------------
        // IsTrungMeThoi helper
        // Gọi SAU khi đã SaveChanges để DB phản ánh trạng thái mới.
        // -------------------------------------------------------
        private async Task RecalcTrungMeByMaMeAsync(string? maMe)
        {
            if (string.IsNullOrEmpty(maMe)) return;
            // Mẻ "active conflict": TrangThaiTL >= 1 hoặc DichChuyen = "len_thang"
            var active = await _repo.GetActiveConflictMesByMaMeAsync(maMe);
            if (active.Count >= 2)
                foreach (var m in active) m.IsTrungMeThoi = true;
            else if (active.Count == 1)
                active[0].IsTrungMeThoi = null;
            // Count == 0: không cần làm gì
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------
        private static HRC1_MeThepVm MapToVm(HRC1_MeThep? m, HRC1_MePhanCong pc, List<MayDuc> mayDucs)
        {
            if (m is null) return new HRC1_MeThepVm { MePhanCongId = pc.Id };

            string? tenMayDucDich = m.IdMayDucDich.HasValue
                ? mayDucs.FirstOrDefault(d => d.Id == m.IdMayDucDich.Value)?.TenMayDuc
                : null;

            return new HRC1_MeThepVm
            {
                Id             = m.Id,
                MePhanCongId   = pc.Id,
                ThuTuTL        = pc.ThuTuTL,
                MaMe           = m.MaMe,
                ThungSo        = m.ThungSo,
                LoSo           = m.LoSo,
                ThoiGian       = m.ThoiGian,
                KLLFSauThep    = m.KLLFSauThep,
                KlLan1         = m.KlLan1,
                KlLan2         = m.KlLan2,
                KlLan3         = m.KlLan3,
                KlThepLong     = m.KlThepLong,
                DichChuyen     = m.DichChuyen,
                TLDichSo       = m.TLDichSo,
                IdMayDucDich   = m.IdMayDucDich,
                TenMayDucDich  = tenMayDucDich,
                IsThuNghiem    = m.IsThuNghiem,
                IsTrungMeThoi  = m.IsTrungMeThoi,
                IsGhost        = m.IsGhost,
                IsChot         = m.IsChot,
                IsManualTL     = m?.IsManualTL,
                GhiChuLo       = m.GhiChuLo,
                PhanLoai       = m.PhanLoai,
                MacThep        = m.MacThep,
                MacThepBKMIS   = m.MacThepBKMIS,
                IdMacThep      = m.IdMacThep,
                GhiChuTL       = m.GhiChuTL,
                TrangThaiLo    = m.TrangThaiLo,
                TrangThaiTL    = m.TrangThaiTL,
                TrangThaiDuc   = m.TrangThaiDuc,
                CapNhatBoi     = m.CapNhatBoi,
                CapNhatLuc     = m.CapNhatLuc,
                XacNhanBoi     = pc.XacNhanBoi,
                XacNhanLuc     = pc.XacNhanLuc
            };
        }

        private static string Snapshot(HRC1_MeThep m) =>
            JsonSerializer.Serialize(m, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
    }
}
