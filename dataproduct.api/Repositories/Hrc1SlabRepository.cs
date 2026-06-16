using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Services;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class Hrc1SlabRepository : IHrc1SlabRepository
    {
        private readonly ProductFormContext _context;
        private readonly SyncPhanLoaiService _syncPhanLoai;
        private const string MaBm = "HRC1_BBGN_PhoiTam";

        public Hrc1SlabRepository(ProductFormContext context, SyncPhanLoaiService syncPhanLoai)
        {
            _context = context;
            _syncPhanLoai = syncPhanLoai;
        }

        // ── Upsert từ TSC API ─────────────────────────────────────────────────

        public async Task<Hrc1SlabSyncResult> UpsertFromApiAsync(List<TscSlabItem> items)
        {
            // Lấy MacThep trực tiếp từ SP (Linked Server) thay vì tra HRC1_MeThep
            var heatIds = items
                .Select(x => x.HEAT_ID)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList()!;

            var macThepMap = await _syncPhanLoai.GetMacThepMapAsync(heatIds!);

            // Load existing slabs
            var slabIds = items.Select(i => i.SLAB_ID).Where(x => x != null).ToList();
            var existing = await _context.Hrc1Slabs
                .Where(s => slabIds.Contains(s.IDSlab))
                .ToDictionaryAsync(s => s.IDSlab);

            int upserted = 0;
            int macThepFilled = 0;
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.SLAB_ID)) continue;

                macThepMap.TryGetValue(item.HEAT_ID ?? "", out var macThep);
                var ngaySX = ParseNgaySX(item.CA);
                var caSX = item.CA?.Length > 0 ? item.CA[..1] : null;
                var kipSX = item.CA?.Length > 1 ? item.CA[1..2] : null;

                if (existing.TryGetValue(item.SLAB_ID, out var slab))
                {
                    if (slab.CutDate.HasValue) continue; // Bỏ qua record đã chốt

                    slab.IDPiece = item.PIECE_ID;
                    slab.MaMe = item.HEAT_ID;
                    if (macThep != null && slab.MacThep == null) macThepFilled++;
                    slab.MacThep = macThep ?? slab.MacThep;
                    slab.NgaySX = ngaySX;
                    slab.CaSX = caSX;
                    slab.KipSX = kipSX;
                    slab.MayDuc = item.TSC_NO;
                    slab.CutDate = item.CUT_DATE;
                    slab.ChieuDay = item.THICKNESS;
                    slab.ChieuRong = item.WIDTH_HEAD;
                    slab.ChieuDai = item.LENGTH;
                    slab.KhoiLuong = item.WEIGHT;
                    slab.NgayCapNhat = DateTime.Now;
                }
                else
                {
                    if (macThep != null) macThepFilled++;
                    _context.Hrc1Slabs.Add(new Hrc1Slab
                    {
                        IDSlab = item.SLAB_ID,
                        IDPiece = item.PIECE_ID,
                        MaMe = item.HEAT_ID,
                        MacThep = macThep,
                        NgaySX = ngaySX,
                        CaSX = caSX,
                        KipSX = kipSX,
                        MayDuc = item.TSC_NO,
                        CutDate = item.CUT_DATE,
                        ChieuDay = item.THICKNESS,
                        ChieuRong = item.WIDTH_HEAD,
                        ChieuDai = item.LENGTH,
                        KhoiLuong = item.WEIGHT,
                    });
                }
                upserted++;
            }

            await _context.SaveChangesAsync();

            return new Hrc1SlabSyncResult
            {
                Success = true,
                TotalFromApi = items.Count,
                RowsUpserted = upserted,
                MacThepFilled = macThepFilled,
                Message = "Sync hoàn thành"
            };
        }

        private static DateOnly? ParseNgaySX(string? ca)
        {
            if (string.IsNullOrEmpty(ca) || ca.Length < 10) return null;
            var dateStr = ca[^10..]; // "10.06.2026"
            if (DateOnly.TryParseExact(dateStr, "dd.MM.yyyy", null,
                System.Globalization.DateTimeStyles.None, out var result))
                return result;
            return null;
        }

        // ── Search ────────────────────────────────────────────────────────────

        public async Task<(IEnumerable<Hrc1SlabItem> Data, int TotalCount)> SearchAsync(Hrc1SlabSearchRequest req)
        {
            var query = _context.Hrc1Slabs.AsNoTracking();

            if (req.TuNgay.HasValue)  query = query.Where(s => s.NgaySX >= req.TuNgay);
            if (req.DenNgay.HasValue) query = query.Where(s => s.NgaySX <= req.DenNgay);
            if (!string.IsNullOrEmpty(req.CaSX))   query = query.Where(s => s.CaSX == req.CaSX);
            if (!string.IsNullOrEmpty(req.KipSX))  query = query.Where(s => s.KipSX == req.KipSX);
            if (!string.IsNullOrEmpty(req.MayDuc)) query = query.Where(s => s.MayDuc == req.MayDuc);
            if (!string.IsNullOrEmpty(req.MaMe))   query = query.Where(s => s.MaMe!.Contains(req.MaMe));
            if (!string.IsNullOrEmpty(req.IDSlab)) query = query.Where(s => s.IDSlab.Contains(req.IDSlab));
            if (!string.IsNullOrEmpty(req.MacThep)) query = query.Where(s => s.MacThep!.Contains(req.MacThep));
            if (req.IsChot.HasValue)
                query = req.IsChot.Value
                    ? query.Where(s => s.CutDate != null)
                    : query.Where(s => s.CutDate == null);

            // Filter theo trạng thái workflow — khi == 0, phải bao gồm cả record chưa có TrangThai (null)
            if (req.TrangThaiKCS.HasValue)
                query = query.Where(s => s.TrangThai != null && s.TrangThai.TrangThaiKCS == req.TrangThaiKCS
                                      || req.TrangThaiKCS == 0 && s.TrangThai == null);
            if (req.TrangThaiDuc.HasValue)
                query = query.Where(s => s.TrangThai != null && s.TrangThai.TrangThaiDuc == req.TrangThaiDuc
                                      || req.TrangThaiDuc == 0 && s.TrangThai == null);
            if (req.TrangThaiKho.HasValue)
                query = query.Where(s => s.TrangThai != null && s.TrangThai.TrangThaiKho == req.TrangThaiKho
                                      || req.TrangThaiKho == 0 && s.TrangThai == null);
            if (req.TrangThaiPKH.HasValue)
                query = query.Where(s => s.TrangThai != null && s.TrangThai.TrangThaiPKH == req.TrangThaiPKH
                                      || req.TrangThaiPKH == 0 && s.TrangThai == null);

            var total = await query.CountAsync();

            var slabs = await query
                .OrderByDescending(s => s.NgaySX)
                .ThenByDescending(s => s.CaSX)
                .ThenBy(s => s.MayDuc)
                .ThenBy(s => s.IDSlab)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Include(s => s.TrangThai)
                .ToListAsync();

            // Load phiếu BBSL info
            var phieuIds = slabs
                .Where(s => s.TrangThai?.IdPhieuBBSL != null)
                .Select(s => s.TrangThai!.IdPhieuBBSL!.Value)
                .Distinct()
                .ToList();

            var phieuMap = phieuIds.Count > 0
                ? await _context.BmPhieus
                    .Where(p => phieuIds.Contains(p.Idphieu) && p.MaBm == MaBm)
                    .ToDictionaryAsync(p => p.Idphieu, p => p)
                : [];

            var items = slabs.Select(s =>
            {
                var tt = s.TrangThai;
                BmPhieu? phieu = null;
                if (tt?.IdPhieuBBSL != null) phieuMap.TryGetValue(tt.IdPhieuBBSL.Value, out phieu);

                return new Hrc1SlabItem
                {
                    Id = s.Id,
                    IDSlab = s.IDSlab,
                    IDPiece = s.IDPiece,
                    MaMe = s.MaMe,
                    MacThep = s.MacThep,
                    NgaySX = s.NgaySX,
                    CaSX = s.CaSX,
                    KipSX = s.KipSX,
                    MayDuc = s.MayDuc,
                    CutDate = s.CutDate,
                    ChieuDay = s.ChieuDay,
                    ChieuRong = s.ChieuRong,
                    ChieuDai = s.ChieuDai,
                    KhoiLuong = s.KhoiLuong,
                    NgayTao = s.NgayTao,
                    NgayCapNhat = s.NgayCapNhat,
                    TrangThaiKCS = tt?.TrangThaiKCS ?? 0,
                    TrangThaiDuc = tt?.TrangThaiDuc ?? 0,
                    TrangThaiKho = tt?.TrangThaiKho ?? 0,
                    TrangThaiPKH = tt?.TrangThaiPKH ?? 0,
                    IdPhieuBBSL = tt?.IdPhieuBBSL,
                    SoPhieuBBSL = phieu?.SoPhieu,
                    NgayXuLy = phieu?.NgaySX,
                    CaBBSL = phieu?.Ca,
                    KipBBSL = phieu?.Kip,
                };
            });

            return (items, total);
        }

        // ── Tổng hợp (GROUP BY) ───────────────────────────────────────────────

        public async Task<IEnumerable<Hrc1SlabTongHopItem>> GetTongHopAsync(
            DateOnly? tuNgay, DateOnly? denNgay, string? ca, string? kip)
        {
            var query = _context.Hrc1Slabs.AsNoTracking()
                .Include(s => s.TrangThai)
                .Where(s => s.TrangThai != null && s.TrangThai.TrangThaiKCS == 1);

            if (tuNgay.HasValue)  query = query.Where(s => s.NgaySX >= tuNgay);
            if (denNgay.HasValue) query = query.Where(s => s.NgaySX <= denNgay);
            if (!string.IsNullOrEmpty(ca))  query = query.Where(s => s.CaSX == ca);
            if (!string.IsNullOrEmpty(kip)) query = query.Where(s => s.KipSX == kip);

            var result = await query
                .GroupBy(s => new { s.MaMe, s.MacThep, s.ChieuDay, s.ChieuRong, s.ChieuDai, s.MayDuc })
                .Select(g => new Hrc1SlabTongHopItem
                {
                    MaMe = g.Key.MaMe,
                    MacThep = g.Key.MacThep,
                    ChieuDay = g.Key.ChieuDay,
                    ChieuRong = g.Key.ChieuRong,
                    ChieuDai = g.Key.ChieuDai,
                    MayDuc = g.Key.MayDuc,
                    SoLuong = g.Count(),
                    TongKhoiLuong = g.Sum(s => s.KhoiLuong),
                })
                .OrderBy(x => x.MaMe)
                .ThenBy(x => x.MacThep)
                .ToListAsync();

            return result;
        }

        // ── Danh sách phiếu BBSL chưa chốt ──────────────────────────────────

        public async Task<IEnumerable<Hrc1PhieuBBSLItem>> GetPhieuBBSLAsync(string? kip, int? ca)
        {
            var query = _context.BmPhieus.AsNoTracking()
                .Where(p => p.MaBm == MaBm && p.TinhTrang != 5 && p.IsLock != 1
                            && (p.IsDelete == null || p.IsDelete == 0));

            if (!string.IsNullOrEmpty(kip)) query = query.Where(p => p.Kip == kip);
            if (ca.HasValue) query = query.Where(p => p.Ca == ca);

            var phieus = await query.OrderByDescending(p => p.NgaySX).ToListAsync();
            var ids = phieus.Select(p => p.Idphieu).ToList();

            // Đếm slab theo từng phiếu
            var counts = await _context.Hrc1SlabTrangThais.AsNoTracking()
                .Where(t => t.IdPhieuBBSL != null && ids.Contains(t.IdPhieuBBSL!.Value))
                .GroupBy(t => t.IdPhieuBBSL!.Value)
                .Select(g => new
                {
                    IdPhieu = g.Key,
                    Total = g.Count(),
                    Duc = g.Count(t => t.TrangThaiDuc == 1),
                    Kho = g.Count(t => t.TrangThaiKho == 1),
                    PKH = g.Count(t => t.TrangThaiPKH == 1),
                })
                .ToDictionaryAsync(x => x.IdPhieu);

            return phieus.Select(p =>
            {
                counts.TryGetValue(p.Idphieu, out var c);
                return new Hrc1PhieuBBSLItem
                {
                    IdPhieu = p.Idphieu,
                    SoPhieu = p.SoPhieu,
                    NgaySX = p.NgaySX,
                    Ca = p.Ca,
                    Kip = p.Kip,
                    TinhTrang = p.TinhTrang,
                    SoSlabDaChot = c?.Total ?? 0,
                    SoSlabDuc = c?.Duc ?? 0,
                    SoSlabKho = c?.Kho ?? 0,
                    SoSlabPKH = c?.PKH ?? 0,
                };
            });
        }

        // ── Ruột phiếu (GROUP BY) ─────────────────────────────────────────────

        public async Task<IEnumerable<Hrc1SlabTongHopItem>> GetRuotPhieuAsync(Guid idPhieu)
        {
            return await _context.Hrc1SlabTrangThais.AsNoTracking()
                .Include(t => t.Slab)
                .Where(t => t.IdPhieuBBSL == idPhieu && t.TrangThaiKCS == 1)
                .GroupBy(t => new
                {
                    t.Slab.MaMe, t.Slab.MacThep,
                    t.Slab.ChieuDay, t.Slab.ChieuRong, t.Slab.ChieuDai, t.Slab.MayDuc
                })
                .Select(g => new Hrc1SlabTongHopItem
                {
                    MaMe = g.Key.MaMe,
                    MacThep = g.Key.MacThep,
                    ChieuDay = g.Key.ChieuDay,
                    ChieuRong = g.Key.ChieuRong,
                    ChieuDai = g.Key.ChieuDai,
                    MayDuc = g.Key.MayDuc,
                    SoLuong = g.Count(),
                    TongKhoiLuong = g.Sum(t => t.Slab.KhoiLuong),
                })
                .OrderBy(x => x.MaMe)
                .ToListAsync();
        }

        // ── Slab cá nhân trong phiếu ──────────────────────────────────────────

        public async Task<IEnumerable<Hrc1SlabItem>> GetSlabsByPhieuAsync(Guid idPhieu)
        {
            var slabs = await _context.Hrc1SlabTrangThais.AsNoTracking()
                .Include(t => t.Slab)
                .Where(t => t.IdPhieuBBSL == idPhieu)
                .ToListAsync();

            return slabs.Select(t => MapToItem(t.Slab, t, null));
        }

        // ── KCS: Chuyển slab lên phiếu ───────────────────────────────────────

        public async Task ChuyenBBSLAsync(List<int> idSlabs, Guid idPhieu, int nguoiThucHien)
        {
            var slabs = await _context.Hrc1Slabs
                .Include(s => s.TrangThai)
                .Where(s => idSlabs.Contains(s.Id))
                .ToListAsync();

            foreach (var slab in slabs)
            {
                if (slab.TrangThai == null)
                {
                    _context.Hrc1SlabTrangThais.Add(new Hrc1SlabTrangThai
                    {
                        IdSlab = slab.Id,
                        IdPhieuBBSL = idPhieu,
                        TrangThaiKCS = 1,
                        NguoiChuyenKCS = nguoiThucHien,
                        NgayChuyenKCS = DateTime.Now,
                    });
                }
                else
                {
                    slab.TrangThai.IdPhieuBBSL = idPhieu;
                    slab.TrangThai.TrangThaiKCS = 1;
                    slab.TrangThai.NguoiChuyenKCS = nguoiThucHien;
                    slab.TrangThai.NgayChuyenKCS = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
        }

        // ── KCS: Thu hồi ─────────────────────────────────────────────────────

        public async Task ThuHoiAsync(List<int> idSlabs, int nguoiThucHien)
        {
            var records = await _context.Hrc1SlabTrangThais
                .Where(t => idSlabs.Contains(t.IdSlab) && t.TrangThaiPKH == 0)
                .ToListAsync();

            foreach (var t in records)
            {
                t.IdPhieuBBSL = null;
                t.TrangThaiKCS = 0;
                t.NguoiChuyenKCS = null;
                t.NgayChuyenKCS = null;
            }

            await _context.SaveChangesAsync();
        }

        // ── Đúc/Kho: Xác nhận ────────────────────────────────────────────────

        public async Task XacNhanAsync(List<int> idSlabs, string loaiXacNhan, int nguoiThucHien)
        {
            var records = await _context.Hrc1SlabTrangThais
                .Where(t => idSlabs.Contains(t.IdSlab) && t.TrangThaiPKH == 0)
                .ToListAsync();

            var now = DateTime.Now;
            foreach (var t in records)
            {
                if (loaiXacNhan == "Duc")
                {
                    t.TrangThaiDuc = 1;
                    t.NguoiXacNhanDuc = nguoiThucHien;
                    t.NgayXacNhanDuc = now;
                }
                else if (loaiXacNhan == "Kho")
                {
                    t.TrangThaiKho = 1;
                    t.NguoiXacNhanKho = nguoiThucHien;
                    t.NgayXacNhanKho = now;
                }
            }

            await _context.SaveChangesAsync();
        }

        // ── Đúc/Kho: Hủy xác nhận ────────────────────────────────────────────

        public async Task HuyXacNhanAsync(List<int> idSlabs, string loaiXacNhan, int nguoiThucHien)
        {
            var records = await _context.Hrc1SlabTrangThais
                .Where(t => idSlabs.Contains(t.IdSlab) && t.TrangThaiPKH == 0)
                .ToListAsync();

            foreach (var t in records)
            {
                if (loaiXacNhan == "Duc")
                {
                    t.TrangThaiDuc = 0;
                    t.NguoiXacNhanDuc = null;
                    t.NgayXacNhanDuc = null;
                }
                else if (loaiXacNhan == "Kho")
                {
                    t.TrangThaiKho = 0;
                    t.NguoiXacNhanKho = null;
                    t.NgayXacNhanKho = null;
                }
            }

            await _context.SaveChangesAsync();
        }

        // ── PKH: Chốt phiếu ──────────────────────────────────────────────────

        public async Task ChotPhieuAsync(Guid idPhieu, int nguoiThucHien)
        {
            var records = await _context.Hrc1SlabTrangThais
                .Where(t => t.IdPhieuBBSL == idPhieu)
                .ToListAsync();

            var now = DateTime.Now;
            foreach (var t in records)
            {
                t.TrangThaiPKH = 1;
                t.NguoiChotPKH = nguoiThucHien;
                t.NgayChotPKH = now;
            }

            var phieu = await _context.BmPhieus.FindAsync(idPhieu);
            if (phieu != null)
            {
                phieu.TinhTrang = 5;
                phieu.IsLock = 1;
            }

            await _context.SaveChangesAsync();
        }

        // ── PKH: Hủy chốt phiếu ──────────────────────────────────────────────

        public async Task HuyChotPhieuAsync(Guid idPhieu, int nguoiThucHien)
        {
            var records = await _context.Hrc1SlabTrangThais
                .Where(t => t.IdPhieuBBSL == idPhieu)
                .ToListAsync();

            foreach (var t in records)
            {
                t.TrangThaiPKH = 0;
                t.NguoiChotPKH = null;
                t.NgayChotPKH = null;
            }

            var phieu = await _context.BmPhieus.FindAsync(idPhieu);
            if (phieu != null)
            {
                phieu.TinhTrang = 1;
                phieu.IsLock = 0;
            }

            await _context.SaveChangesAsync();
        }

        // ── Fill MacThep trực tiếp từ SP (không qua HRC1_MeThep) ──────────────

        public async Task<int> FillMacThepAsync()
        {
            var slabs = await _context.Hrc1Slabs
                .Where(s => s.MacThep == null && s.MaMe != null)
                .ToListAsync();

            if (slabs.Count == 0) return 0;

            var maMes = slabs.Select(s => s.MaMe!).Distinct().ToList();
            var macThepMap = await _syncPhanLoai.GetMacThepMapAsync(maMes);

            if (macThepMap.Count == 0) return 0;

            int updated = 0;
            var now = DateTime.Now;
            foreach (var slab in slabs)
            {
                if (macThepMap.TryGetValue(slab.MaMe!, out var macThep))
                {
                    slab.MacThep = macThep;
                    slab.NgayCapNhat = now;
                    updated++;
                }
            }

            if (updated > 0)
                await _context.SaveChangesAsync();

            return updated;
        }

        // ── Update GhiChu / MaVatTu per slab ────────────────────────────────

        public async Task UpdateSlabAsync(int id, Hrc1SlabUpdateRequest req)
        {
            var slab = await _context.Hrc1Slabs.FindAsync(id)
                ?? throw new InvalidOperationException($"Slab {id} không tồn tại.");
            slab.GhiChu = req.GhiChu;
            slab.MaVatTu = req.MaVatTu;
            slab.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        // ── Tổng hợp ghi chú (per phiếu × MacThep × KichThuoc) ──────────────

        public async Task<IEnumerable<Hrc1TongHopGhiChuItem>> GetTongHopGhiChuAsync(Guid idPhieu)
        {
            return await _context.Hrc1BbslTongHopGhiChus
                .AsNoTracking()
                .Where(x => x.IdPhieuBBSL == idPhieu)
                .Select(x => new Hrc1TongHopGhiChuItem
                {
                    MacThep  = x.MacThep,
                    KichThuoc = x.KichThuoc,
                    GhiChu   = x.GhiChu,
                })
                .ToListAsync();
        }

        public async Task SaveTongHopGhiChuAsync(Hrc1SaveTongHopGhiChuRequest req)
        {
            var existing = await _context.Hrc1BbslTongHopGhiChus
                .FirstOrDefaultAsync(x =>
                    x.IdPhieuBBSL == req.IdPhieuBBSL &&
                    x.MacThep == req.MacThep &&
                    x.KichThuoc == req.KichThuoc);

            if (existing == null)
            {
                _context.Hrc1BbslTongHopGhiChus.Add(new Hrc1BbslTongHopGhiChu
                {
                    IdPhieuBBSL = req.IdPhieuBBSL,
                    MacThep     = req.MacThep,
                    KichThuoc   = req.KichThuoc,
                    GhiChu      = req.GhiChu,
                    NgayCapNhat = DateTime.Now,
                });
            }
            else
            {
                existing.GhiChu      = req.GhiChu;
                existing.NgayCapNhat = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static Hrc1SlabItem MapToItem(Hrc1Slab s, Hrc1SlabTrangThai? tt, BmPhieu? phieu)
        {
            return new Hrc1SlabItem
            {
                Id = s.Id,
                IDSlab = s.IDSlab,
                IDPiece = s.IDPiece,
                MaMe = s.MaMe,
                MacThep = s.MacThep,
                NgaySX = s.NgaySX,
                CaSX = s.CaSX,
                KipSX = s.KipSX,
                MayDuc = s.MayDuc,
                CutDate = s.CutDate,
                ChieuDay = s.ChieuDay,
                ChieuRong = s.ChieuRong,
                ChieuDai = s.ChieuDai,
                KhoiLuong = s.KhoiLuong,
                NgayTao = s.NgayTao,
                NgayCapNhat = s.NgayCapNhat,
                GhiChu = s.GhiChu,
                MaVatTu = s.MaVatTu,
                TrangThaiKCS = tt?.TrangThaiKCS ?? 0,
                TrangThaiDuc = tt?.TrangThaiDuc ?? 0,
                TrangThaiKho = tt?.TrangThaiKho ?? 0,
                TrangThaiPKH = tt?.TrangThaiPKH ?? 0,
                IdPhieuBBSL = tt?.IdPhieuBBSL,
                SoPhieuBBSL = phieu?.SoPhieu,
                NgayXuLy = phieu?.NgaySX,
                CaBBSL = phieu?.Ca,
                KipBBSL = phieu?.Kip,
            };
        }
    }
}
