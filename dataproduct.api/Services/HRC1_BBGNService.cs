using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;
using System.Text.Json;

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
        // -------------------------------------------------------
        public async Task<HRC1_PhieuDataVm> GetPhieuAsync(Guid idPhieu)
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
                rawMes = phieu.NgaySX.HasValue && phieu.Ca.HasValue && phieu.Scope.HasValue
                    ? await _repo.GetMeThepsByMayDucAsync(phieu.NgaySX.Value, phieu.Ca.Value, phieu.Scope.Value)
                    : new List<HRC1_MeThep>();

                danhSachMe = rawMes
                    .Select(m => MapToVm(m, new HRC1_MePhanCong { MeId = m.Id, IdPhieu = idPhieu, CongDoan = "duc" }, mayDucs))
                    .ToList();
            }
            else
            {
                var phanCongs = await _repo.GetMePhanCongsByPhieuAsync(idPhieu, congDoan);
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

            var old = Snapshot(me);

            me.ThungSo        = req.ThungSo        ?? me.ThungSo;
            me.KLLFSauThep    = req.KLLFSauThep    ?? me.KLLFSauThep;
            me.KlLan3         = req.KlLan3         ?? me.KlLan3;
            // Khi DichChuyen được cung cấp, ghi đè cả nhóm (cho phép null-clear TLDichSo / IdMayDucDich)
            if (req.DichChuyen is not null)
            {
                me.DichChuyen   = req.DichChuyen;
                me.TLDichSo     = req.TLDichSo;
                me.IdMayDucDich = req.IdMayDucDich;
            }
            else
            {
                me.TLDichSo     = req.TLDichSo     ?? me.TLDichSo;
                me.IdMayDucDich = req.IdMayDucDich ?? me.IdMayDucDich;
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
                MeId     = req.MeId,
                IdPhieu  = req.IdPhieu,
                CongDoan = "tinh_luyen",
                ThuTuTL  = null
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

            if ((me.TrangThaiTL ?? 0) < 1)
                throw new InvalidOperationException("Mẻ chưa được nhận vào tinh luyện.");

            if ((me.TrangThaiDuc ?? 0) >= 1)
                throw new InvalidOperationException("Máy đúc đã xác nhận mẻ này, không thể hủy nhận.");

            var old = Snapshot(me);

            // Xóa tất cả MePhanCong tinh_luyen của mẻ trong phiếu này (cả dòng lần 2+)
            var pcs = await _repo.GetTLPhanCongsByMePhieuAsync(req.MeId, req.IdPhieu);
            _repo.RemoveMePhanCongs(pcs);

            // Reset trạng thái và toàn bộ dữ liệu TL đã nhập
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
            // IdMayDucDich chỉ clear nếu do TL chọn (tinh_luyen); len_thang do LT chọn → giữ nguyên
            if (me.DichChuyen == "tinh_luyen")
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
        // -------------------------------------------------------
        public async Task<HRC1_PhieuDataVm> SyncMeThoiLoThoiAsync(Guid idPhieu)
        {
            var phieu = await _repo.GetBmPhieuAsync(idPhieu)
                ?? throw new KeyNotFoundException($"Không tìm thấy phiếu {idPhieu}");
            if (phieu.MaBm != "HRC1_LoThoi")
                throw new InvalidOperationException("Chỉ đồng bộ được cho phiếu lò thổi.");

            // Lấy danh sách mẻ từ gang lỏng
            List<string> gangMaMes;
            try
            {
                gangMaMes = await _bbgnSvc.FetchMeThoiHRC1Async(new HRC1_FetchMeThoiRequest
                {
                    NgaySX   = phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today),
                    Ca       = phieu.Ca ?? 1,
                    IdLoThoi = phieu.Scope
                });
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"Không kết nối được hệ thống gang lỏng: {ex.Message}", ex);
            }

            // Lấy danh sách mẻ hiện có trong phiếu này
            var phanCongs   = await _repo.GetMePhanCongsByPhieuAsync(idPhieu, "lo_thoi");
            var existingIds = phanCongs.Select(pc => pc.MeId).Distinct().ToList();
            var existingMes = existingIds.Count > 0
                ? await _repo.GetMeThepsByIdsAsync(existingIds)
                : new List<HRC1_MeThep>();

            var gangSet       = new HashSet<string>(gangMaMes, StringComparer.OrdinalIgnoreCase);
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

            // 3. Mẻ mới từ gang chưa có trong phiếu → insert
            // NgayTao = thời điểm bắt đầu ca của phiếu (không dùng DateTime.Now vì sync có thể chạy muộn)
            var ngayTaoMe = phieu.NgaySX.HasValue && phieu.Ca.HasValue
                ? (phieu.Ca.Value == 1
                    ? phieu.NgaySX.Value.ToDateTime(new TimeOnly(6, 0))
                    : phieu.NgaySX.Value.ToDateTime(new TimeOnly(18, 0)))
                : DateTime.Now;

            var newMes = gangMaMes
                .Where(ma => !existingByMaMe.ContainsKey(ma))
                .Select(ma => new HRC1_MeThep { MaMe = ma, LoSo = phieu.Scope, NgayTao = ngayTaoMe })
                .ToList();

            foreach (var me in newMes)
                _repo.AddMeThep(me);

            await _repo.SaveChangesAsync(); // EF gán Id cho newMes

            foreach (var me in newMes)
                _repo.AddMePhanCong(new HRC1_MePhanCong
                {
                    MeId     = me.Id,
                    IdPhieu  = idPhieu,
                    CongDoan = "lo_thoi"
                });

            if (newMes.Count > 0)
                await _repo.SaveChangesAsync();

            return await GetPhieuAsync(idPhieu);
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
