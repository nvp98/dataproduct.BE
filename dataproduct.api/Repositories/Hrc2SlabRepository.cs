using dataproduct.api.DTOs;
using dataproduct.api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class Hrc2SlabRepository : IHrc2SlabRepository
    {
        private readonly ProductFormContext _context;
        private const string MaBm = "HRC2_BBGN_PhoiTam";

        public Hrc2SlabRepository(ProductFormContext context)
        {
            _context = context;
        }

        // ── Search ────────────────────────────────────────────────────────────

        public async Task<(IEnumerable<Hrc2SlabItem> Data, int TotalCount)> SearchAsync(Hrc2SlabSearchRequest req)
        {
            var query = _context.BkHrc2Slabs.AsNoTracking();

            if (!string.IsNullOrEmpty(req.CaSanXuat))
                query = query.Where(s => s.CaSanXuat == req.CaSanXuat);
            if (!string.IsNullOrEmpty(req.Kip))
                query = query.Where(s => s.KipSanXuat == req.Kip);
            if (!string.IsNullOrEmpty(req.MeThep))
                query = query.Where(s => s.MeThep != null && s.MeThep.Contains(req.MeThep));
            if (!string.IsNullOrEmpty(req.MacThep))
                query = query.Where(s => s.MacThep != null && s.MacThep.Contains(req.MacThep));
            if (req.IdSlabs != null && req.IdSlabs.Count > 0)
                query = query.Where(s => req.IdSlabs.Contains(s.IdSlab!));
            if (req.IsChot.HasValue)
                query = query.Where(s => s.IsChot == req.IsChot.Value);
            if (req.IsTrungIDSlab.HasValue)
                query = query.Where(s => s.IsTrungIDSlab == req.IsTrungIDSlab.Value);
            if (req.IsDiffMacThep.HasValue)
                query = query.Where(s => s.IsDiffMacThep == req.IsDiffMacThep.Value);
            if (req.IsSaiLotName.HasValue)
                query = query.Where(s => s.IsSaiLotName == req.IsSaiLotName.Value);

            // Date filter via NgaySanXuat
            if (DateOnly.TryParse(req.TuNgay, out var tuNgay))
                query = query.Where(s => s.NgaySanXuat >= tuNgay);
            if (DateOnly.TryParse(req.DenNgay, out var denNgay))
                query = query.Where(s => s.NgaySanXuat < denNgay.AddDays(1));

            // Workflow filter
            if (req.TrangThaiKCS.HasValue)
                query = req.TrangThaiKCS == 0
                    ? query.Where(s => !_context.BkHrc2SlabTrangThais.Any(t => t.IdSlab == s.Id && t.TrangThaiKCS == 1))
                    : query.Where(s => _context.BkHrc2SlabTrangThais.Any(t => t.IdSlab == s.Id && t.TrangThaiKCS == req.TrangThaiKCS));
            if (req.TrangThaiDuc.HasValue)
                query = req.TrangThaiDuc == 0
                    ? query.Where(s => !_context.BkHrc2SlabTrangThais.Any(t => t.IdSlab == s.Id && t.TrangThaiDuc == 1))
                    : query.Where(s => _context.BkHrc2SlabTrangThais.Any(t => t.IdSlab == s.Id && t.TrangThaiDuc == req.TrangThaiDuc));
            if (req.TrangThaiKho.HasValue)
                query = req.TrangThaiKho == 0
                    ? query.Where(s => !_context.BkHrc2SlabTrangThais.Any(t => t.IdSlab == s.Id && t.TrangThaiKho == 1))
                    : query.Where(s => _context.BkHrc2SlabTrangThais.Any(t => t.IdSlab == s.Id && t.TrangThaiKho == req.TrangThaiKho));
            if (req.TrangThaiPKH.HasValue)
                query = req.TrangThaiPKH == 0
                    ? query.Where(s => !_context.BkHrc2SlabTrangThais.Any(t => t.IdSlab == s.Id && t.TrangThaiPKH == 1))
                    : query.Where(s => _context.BkHrc2SlabTrangThais.Any(t => t.IdSlab == s.Id && t.TrangThaiPKH == req.TrangThaiPKH));

            var total = await query.CountAsync();

            var slabs = await query
                .OrderByDescending(s => s.BkmisId)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .ToListAsync();

            var items = await AttachTrangThaiAsync(slabs, null);
            return (items, total);
        }

        // ── Tổng hợp (GROUP BY) ───────────────────────────────────────────────

        public async Task<IEnumerable<Hrc2SlabTongHopItem>> GetTongHopAsync(
            string? tuNgay, string? denNgay, string? ca, string? kip)
        {
            var query = _context.BkHrc2Slabs.AsNoTracking();

            if (!string.IsNullOrEmpty(ca))  query = query.Where(s => s.CaSanXuat == ca);
            if (!string.IsNullOrEmpty(kip)) query = query.Where(s => s.KipSanXuat == kip);
            if (DateOnly.TryParse(tuNgay, out var fromDt))
                query = query.Where(s => s.NgaySanXuat >= fromDt);
            if (DateOnly.TryParse(denNgay, out var toDt))
                query = query.Where(s => s.NgaySanXuat < toDt.AddDays(1));

            return await query
                .GroupBy(s => new { s.MeThep, s.MacThep, s.ChieuDay, s.ChieuRong, s.ChieuDai, s.PhanLoai })
                .Select(g => new Hrc2SlabTongHopItem
                {
                    MeThep        = g.Key.MeThep,
                    MacThep       = g.Key.MacThep,
                    ChieuDay      = g.Key.ChieuDay,
                    ChieuRong     = g.Key.ChieuRong,
                    ChieuDai      = g.Key.ChieuDai,
                    PhanLoai      = g.Key.PhanLoai,
                    SoLuong       = g.Count(),
                    TongKhoiLuong = g.Sum(s => s.KhoiLuong),
                })
                .OrderBy(x => x.MeThep)
                .ThenBy(x => x.MacThep)
                .ToListAsync();
        }

        // ── Danh sách phiếu BBSL ─────────────────────────────────────────────

        public async Task<IEnumerable<Hrc2PhieuBBSLItem>> GetPhieuBBSLAsync(string? kip, int? ca)
        {
            var query = _context.BmPhieus.AsNoTracking()
                .Where(p => p.MaBm == MaBm && p.TinhTrang != 5 && p.IsLock != 1
                            && (p.IsDelete == null || p.IsDelete == 0));

            if (!string.IsNullOrEmpty(kip)) query = query.Where(p => p.Kip == kip);
            if (ca.HasValue) query = query.Where(p => p.Ca == ca);

            var phieus = await query.OrderByDescending(p => p.NgaySX).ToListAsync();
            if (phieus.Count == 0) return [];

            var phieuIds = phieus.Select(p => p.Idphieu).ToList();
            var allTT = await _context.BkHrc2SlabTrangThais
                .AsNoTracking()
                .Where(t => t.IdPhieuBBSL != null && phieuIds.Contains(t.IdPhieuBBSL!.Value))
                .ToListAsync();

            var ttByPhieu = allTT.GroupBy(t => t.IdPhieuBBSL!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            return phieus.Select(p =>
            {
                var ttList = ttByPhieu.TryGetValue(p.Idphieu, out var list) ? list : [];
                return new Hrc2PhieuBBSLItem
                {
                    IdPhieu      = p.Idphieu,
                    SoPhieu      = p.SoPhieu,
                    NgaySX       = p.NgaySX,
                    Ca           = p.Ca,
                    Kip          = p.Kip,
                    TinhTrang    = p.TinhTrang,
                    SoSlabDaChot = ttList.Count,
                    SoSlabKCS    = ttList.Count(t => t.TrangThaiKCS == 1),
                    SoSlabDuc    = ttList.Count(t => t.TrangThaiDuc == 1),
                    SoSlabKho    = ttList.Count(t => t.TrangThaiKho == 1),
                    SoSlabPKH    = ttList.Count(t => t.TrangThaiPKH == 1),
                };
            });
        }

        // ── Ruột phiếu (GROUP BY) ─────────────────────────────────────────────

        public async Task<IEnumerable<Hrc2SlabTongHopItem>> GetRuotPhieuAsync(Guid idPhieu)
        {
            var slabs = await LoadPhieuSlabsAsync(idPhieu);

            return slabs
                .GroupBy(s => new { s.MeThep, s.MacThep, s.ChieuDay, s.ChieuRong, s.ChieuDai, s.PhanLoai })
                .Select(g => new Hrc2SlabTongHopItem
                {
                    MeThep        = g.Key.MeThep,
                    MacThep       = g.Key.MacThep,
                    ChieuDay      = g.Key.ChieuDay,
                    ChieuRong     = g.Key.ChieuRong,
                    ChieuDai      = g.Key.ChieuDai,
                    PhanLoai      = g.Key.PhanLoai,
                    SoLuong       = g.Count(),
                    TongKhoiLuong = g.Sum(s => s.KhoiLuong),
                })
                .OrderBy(x => x.MeThep)
                .ThenBy(x => x.MacThep)
                .ToList();
        }

        public async Task<SyncStatusItem> SyncAsync(DateOnly? ngayBatDau, DateOnly? ngayKetThuc)
        {
            var p1 = ngayBatDau.HasValue
                ? new SqlParameter("@NgayBatDau", ngayBatDau.Value.ToDateTime(TimeOnly.MinValue))
                : new SqlParameter("@NgayBatDau", DBNull.Value);
            var p2 = ngayKetThuc.HasValue
                ? new SqlParameter("@NgayKetThuc", ngayKetThuc.Value.ToDateTime(TimeOnly.MinValue))
                : new SqlParameter("@NgayKetThuc", DBNull.Value);

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [dbo].[usp_SyncBK_HRC2_Slab] @NgayBatDau, @NgayKetThuc", p1, p2);

            return await GetSyncStatusAsync() ?? new SyncStatusItem { TrangThai = "DONE" };
        }

        public async Task<SyncStatusItem?> GetSyncStatusAsync()
        {
            var latest = await _context.BkSyncHrc2SlabControls
                .OrderByDescending(x => x.BatDauLuc)
                .FirstOrDefaultAsync();

            if (latest == null) return null;

            return new SyncStatusItem
            {
                Id = latest.Id,
                TrangThai = latest.TrangThai,
                NgayBatDau = latest.NgayBatDau,
                NgayKetThuc = latest.NgayKetThuc,
                BatDauLuc = latest.BatDauLuc,
                KetThucLuc = latest.KetThucLuc,
                SoRecordSync = latest.SoRecordSync,
                GhiChu = latest.GhiChu,
            };
        }

        // ── Chi tiết slab trong phiếu ─────────────────────────────────────────

        public async Task<IEnumerable<Hrc2SlabItem>> GetSlabsByPhieuAsync(Guid idPhieu)
        {
            var phieu = await _context.BmPhieus.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Idphieu == idPhieu && p.MaBm == MaBm)
                ?? throw new InvalidOperationException("Phiếu không tồn tại");

            // Lấy TrangThai theo IdPhieuBBSL
            var ttList = await _context.BkHrc2SlabTrangThais
                .AsNoTracking()
                .Where(t => t.IdPhieuBBSL == idPhieu)
                .ToListAsync();

            if (ttList.Count == 0) return [];

            var slabIds = ttList.Select(t => t.IdSlab).ToList();
            var slabs = await _context.BkHrc2Slabs
                .AsNoTracking()
                .Where(s => slabIds.Contains(s.Id))
                .ToListAsync();

            var ttMap = ttList.ToDictionary(t => t.IdSlab);

            return slabs
                .OrderBy(s => s.ShiftName)
                .ThenBy(s => s.IdSlab)
                .Select(s =>
                {
                    ttMap.TryGetValue(s.Id, out var tt);
                    return MapToItem(s, tt, phieu);
                })
                .ToList();
        }

        // ── Workflow: Xác nhận ────────────────────────────────────────────────

        public async Task XacNhanAsync(List<int> idSlabs, string loaiXacNhan, int nguoiThucHien)
        {
            var ttMap = await _context.BkHrc2SlabTrangThais
                .Where(t => idSlabs.Contains(t.IdSlab))
                .ToDictionaryAsync(t => t.IdSlab);

            var now = DateTime.Now;
            foreach (var id in idSlabs)
            {
                if (!ttMap.TryGetValue(id, out var tt))
                {
                    tt = new BkHrc2SlabTrangThai { IdSlab = id, NgayTao = now };
                    _context.BkHrc2SlabTrangThais.Add(tt);
                }

                if (tt.TrangThaiPKH == 1) continue;

                switch (loaiXacNhan)
                {
                    case "KCS":
                        tt.TrangThaiKCS = 1;
                        tt.NguoiChuyenKCS = nguoiThucHien;
                        tt.NgayChuyenKCS = now;
                        break;
                    case "Duc":
                        tt.TrangThaiDuc = 1;
                        tt.NguoiXacNhanDuc = nguoiThucHien;
                        tt.NgayXacNhanDuc = now;
                        break;
                    case "Kho":
                        tt.TrangThaiKho = 1;
                        tt.NguoiXacNhanKho = nguoiThucHien;
                        tt.NgayXacNhanKho = now;
                        break;
                    case "PKH":
                        tt.TrangThaiPKH = 1;
                        tt.NguoiChotPKH = nguoiThucHien;
                        tt.NgayChotPKH = now;
                        break;
                }
            }

            await _context.SaveChangesAsync();
        }

        // ── Workflow: Hủy xác nhận ────────────────────────────────────────────

        public async Task HuyXacNhanAsync(List<int> idSlabs, string loaiXacNhan, int nguoiThucHien)
        {
            if (loaiXacNhan == "PKH")
            {
                var records = await _context.BkHrc2SlabTrangThais
                    .Where(t => idSlabs.Contains(t.IdSlab) && t.TrangThaiPKH == 1)
                    .ToListAsync();
                foreach (var t in records)
                {
                    t.TrangThaiPKH = 0;
                    t.NguoiChotPKH = null;
                    t.NgayChotPKH = null;
                }
            }
            else
            {
                var records = await _context.BkHrc2SlabTrangThais
                    .Where(t => idSlabs.Contains(t.IdSlab) && t.TrangThaiPKH == 0)
                    .ToListAsync();
                foreach (var t in records)
                {
                    switch (loaiXacNhan)
                    {
                        case "KCS":
                            t.TrangThaiKCS = 0;
                            t.NguoiChuyenKCS = null;
                            t.NgayChuyenKCS = null;
                            break;
                        case "Duc":
                            t.TrangThaiDuc = 0;
                            t.NguoiXacNhanDuc = null;
                            t.NgayXacNhanDuc = null;
                            break;
                        case "Kho":
                            t.TrangThaiKho = 0;
                            t.NguoiXacNhanKho = null;
                            t.NgayXacNhanKho = null;
                            break;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        // ── PKH: Chốt phiếu ──────────────────────────────────────────────────

        public async Task ChotPhieuAsync(Guid idPhieu, int nguoiThucHien)
        {
            var phieu = await _context.BmPhieus.FindAsync(idPhieu)
                ?? throw new InvalidOperationException("Phiếu không tồn tại");

            var records = await _context.BkHrc2SlabTrangThais
                .Where(t => t.IdPhieuBBSL == idPhieu)
                .ToListAsync();

            // Đúc và Kho đồng cấp, song song → chỉ chốt được khi cả 2 bên đã xác nhận hết
            var chuaXacNhan = records.Count(t => t.TrangThaiDuc != 1 || t.TrangThaiKho != 1);
            if (chuaXacNhan > 0)
                throw new InvalidOperationException(
                    $"Còn {chuaXacNhan} slab chưa được Đúc xác nhận và Kho xác nhận, không thể chốt phiếu.");

            var now = DateTime.Now;
            foreach (var t in records)
            {
                t.TrangThaiPKH = 1;
                t.NguoiChotPKH = nguoiThucHien;
                t.NgayChotPKH = now;
            }

            phieu.TinhTrang = 5;
            phieu.IsLock = 1;

            await _context.SaveChangesAsync();
        }

        // ── PKH: Hủy chốt phiếu ──────────────────────────────────────────────

        public async Task HuyChotPhieuAsync(Guid idPhieu, int nguoiThucHien)
        {
            var phieu = await _context.BmPhieus.FindAsync(idPhieu)
                ?? throw new InvalidOperationException("Phiếu không tồn tại");

            var records = await _context.BkHrc2SlabTrangThais
                .Where(t => t.IdPhieuBBSL == idPhieu)
                .ToListAsync();

            foreach (var t in records)
            {
                t.TrangThaiPKH = 0;
                t.NguoiChotPKH = null;
                t.NgayChotPKH = null;
            }

            phieu.TinhTrang = 1;
            phieu.IsLock = 0;

            await _context.SaveChangesAsync();
        }

        // ── Chuyển slab vào phiếu ────────────────────────────────────────────

        public async Task<int> ChuyenBbslAsync(List<int> idSlabs, Guid idPhieu, int nguoiThucHien)
        {
            var phieu = await _context.BmPhieus
                .FirstOrDefaultAsync(p => p.Idphieu == idPhieu && p.MaBm == MaBm)
                ?? throw new InvalidOperationException("Phiếu không tồn tại");

            if (phieu.TinhTrang == 5 || phieu.IsLock == 1)
                throw new InvalidOperationException("Phiếu đã chốt, không thể chuyển slab vào.");

            var ttMap = await _context.BkHrc2SlabTrangThais
                .Where(t => idSlabs.Contains(t.IdSlab))
                .ToDictionaryAsync(t => t.IdSlab);

            var now = DateTime.Now;
            int affected = 0;
            foreach (var id in idSlabs)
            {
                if (!ttMap.TryGetValue(id, out var tt))
                {
                    _context.BkHrc2SlabTrangThais.Add(new BkHrc2SlabTrangThai
                    {
                        IdSlab         = id,
                        IdPhieuBBSL    = idPhieu,
                        TrangThaiKCS   = 1,
                        NguoiChuyenKCS = nguoiThucHien,
                        NgayChuyenKCS  = now,
                        NgayTao        = now,
                    });
                }
                else
                {
                    tt.IdPhieuBBSL    = idPhieu;
                    tt.TrangThaiKCS   = 1;
                    tt.NguoiChuyenKCS = nguoiThucHien;
                    tt.NgayChuyenKCS  = now;
                }
                affected++;
            }

            await _context.SaveChangesAsync();
            return affected;
        }

        // ── Thu hồi slab khỏi phiếu ─────────────────────────────────────────

        public async Task<int> ThuHoiAsync(List<int> idSlabs, int nguoiThucHien)
        {
            var records = await _context.BkHrc2SlabTrangThais
                .Where(t => idSlabs.Contains(t.IdSlab) && t.TrangThaiPKH == 0)
                .ToListAsync();

            foreach (var t in records)
            {
                t.IdPhieuBBSL    = null;
                t.TrangThaiKCS   = 0;
                t.NguoiChuyenKCS = null;
                t.NgayChuyenKCS  = null;
            }

            await _context.SaveChangesAsync();
            return records.Count;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        internal async Task<List<BkHrc2Slab>> LoadPhieuSlabsAsync(Guid idPhieu)
        {
            var ttList = await _context.BkHrc2SlabTrangThais
                .AsNoTracking()
                .Where(t => t.IdPhieuBBSL == idPhieu)
                .ToListAsync();

            if (ttList.Count == 0) return [];

            var slabIds = ttList.Select(t => t.IdSlab).ToList();
            return await _context.BkHrc2Slabs
                .AsNoTracking()
                .Where(s => slabIds.Contains(s.Id))
                .OrderBy(s => s.ShiftName)
                .ThenBy(s => s.IdSlab)
                .ToListAsync();
        }

        private async Task<List<Hrc2SlabItem>> AttachTrangThaiAsync(List<BkHrc2Slab> slabs, BmPhieu? phieu)
        {
            if (slabs.Count == 0) return [];

            var slabIds = slabs.Select(s => s.Id).ToList();
            var ttMap = await _context.BkHrc2SlabTrangThais
                .AsNoTracking()
                .Where(t => slabIds.Contains(t.IdSlab))
                .ToDictionaryAsync(t => t.IdSlab);

            // Nếu có phiếu, lấy thêm phiếu cho slab có IdPhieuBBSL khác
            var phieuIds = ttMap.Values
                .Where(t => t.IdPhieuBBSL != null)
                .Select(t => t.IdPhieuBBSL!.Value)
                .Distinct()
                .ToList();
            var phieuMap = phieuIds.Count > 0
                ? await _context.BmPhieus.AsNoTracking()
                    .Where(p => phieuIds.Contains(p.Idphieu) && p.MaBm == MaBm)
                    .ToDictionaryAsync(p => p.Idphieu)
                : [];

            return slabs.Select(s =>
            {
                ttMap.TryGetValue(s.Id, out var tt);
                BmPhieu? linkedPhieu = phieu;
                if (linkedPhieu == null && tt?.IdPhieuBBSL != null)
                    phieuMap.TryGetValue(tt.IdPhieuBBSL.Value, out linkedPhieu);
                return MapToItem(s, tt, linkedPhieu);
            }).ToList();
        }

        private static Hrc2SlabItem MapToItem(BkHrc2Slab s, BkHrc2SlabTrangThai? tt, BmPhieu? phieu)
        {
            return new Hrc2SlabItem
            {
                Id                 = s.Id,
                BkmisId            = s.BkmisId,
                NgaySanXuat        = s.NgaySanXuat?.ToString("yyyy-MM-dd"),
                ShiftName          = s.ShiftName,
                CaSanXuat          = s.CaSanXuat,
                KipSanXuat         = s.KipSanXuat,
                MeThep             = s.MeThep,
                IdSlab             = s.IdSlab,
                MacThep            = s.MacThep,
                ChatLuong          = s.ChatLuong,
                ChieuDay           = s.ChieuDay,
                ChieuRong          = s.ChieuRong,
                ChieuDai           = s.ChieuDai,
                KhoiLuong          = s.KhoiLuong,
                KhoiLuongTinhToan  = s.KhoiLuongTinhToan,
                ChatLuongTPHH      = s.ChatLuongTPHH,
                ThongTinPhoi       = s.ThongTinPhoi,
                TpKhongDatGangLong = s.TpKhongDatGangLong,
                GhiChu             = s.GhiChu,
                LoaiPhoi           = s.LoaiPhoi,
                SapCode            = s.SapCode,
                SapDescription     = s.SapDescription,
                SoLo               = s.SoLo,
                OrderId            = s.OrderId,
                MayDuc             = s.MayDuc,
                IsTrungIDSlab      = s.IsTrungIDSlab,
                IsDiffMacThep      = s.IsDiffMacThep,
                IsSaiLotName       = s.IsSaiLotName,
                Line               = s.Line,
                SapLastTime        = s.SapLastTime,
                IsChot             = s.IsChot,
                NgayTao            = s.NgayTao,
                PhanLoai           = s.PhanLoai,
                TrangThaiKCS       = tt?.TrangThaiKCS ?? 0,
                TrangThaiDuc       = tt?.TrangThaiDuc ?? 0,
                TrangThaiKho       = tt?.TrangThaiKho ?? 0,
                TrangThaiPKH       = tt?.TrangThaiPKH ?? 0,
                IdPhieuBBSL        = phieu?.Idphieu.ToString(),
                SoPhieuBBSL        = phieu?.SoPhieu,
                NgayXuLy           = phieu?.NgaySX?.ToString("yyyy-MM-dd"),
                CaBBSL             = phieu?.Ca,
                KipBBSL            = phieu?.Kip,
            };
        }
    }
}
