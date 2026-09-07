using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories.NMTKVV
{
    public class TKVV_TonSiloRepository : ITKVV_TonSiloRepository
    {
        private readonly ProductFormContext _context;
        private readonly ITKVV_BCSL_ChiPhiRepository _bcslRepo;
        private readonly ITKVV_NvlBbgnMappingRepository _nvlBbgnRepo;

        public TKVV_TonSiloRepository(
            ProductFormContext context,
            ITKVV_BCSL_ChiPhiRepository bcslRepo,
            ITKVV_NvlBbgnMappingRepository nvlBbgnRepo)
        {
            _context = context;
            _bcslRepo = bcslRepo;
            _nvlBbgnRepo = nvlBbgnRepo;
        }

        // Silo.MaSilo là string ("1".."21") — sắp xếp numeric-aware để khớp đúng thứ tự
        // trên tờ giấy (1,2,3…21), tránh sort chuỗi thô ("10" đứng trước "2").
        private static int MaSiloSortKey(string? maSilo)
            => int.TryParse(maSilo, out var n) ? n : int.MaxValue;

        public async Task<List<TKVVTonSiloRowDto>> InitRowsAsync(InitTonSiloRowsRequestDto request)
        {
            var ngaySX = request.NgaySX;
            var ca = request.Ca;
            var scope = request.Scope;
            var scopeStr = scope.ToString();

            // ── 1. Danh sách Silo theo scope ────────────────────────────────────────
            var silos = await _context.TKVV_Silo
                .Where(x => x.Scope == scopeStr && x.TrangThai)
                .ToListAsync();
            silos = silos
                .OrderBy(x => MaSiloSortKey(x.MaSilo))
                .ThenBy(x => x.MaSilo)
                .ToList();
            var siloIds = silos.Select(s => s.ID).ToList();

            // ── 2. NVL mapping gần nhất cho từng Silo ───────────────────────────────
            var mappingCandidates = await _context.TKVV_NVL_SiloMapping
                .Where(m => m.SiloID.HasValue && siloIds.Contains(m.SiloID.Value)
                         && m.Ca == ca && m.NgaySX <= ngaySX && m.TrangThai)
                .ToListAsync();
            var nvlList = await _context.TKVV_NguyenVatLieu.ToListAsync();
            var nearestMappingBySilo = mappingCandidates
                .GroupBy(m => m.SiloID!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.NgaySX).First());

            // ── 3. Tồn đầu carry-forward: TonCuoi của kíp gần nhất TRƯỚC kíp này ──
            var tonSiloCandidates = await _context.TKVV_TonSilo
                .Where(t => siloIds.Contains(t.SiloID) && !t.IsDelete
                         && (t.NgaySX < ngaySX || (t.NgaySX == ngaySX && t.Ca < ca)))
                .ToListAsync();
            // Tồn đầu = TonCuoi của dòng cuối cùng (theo ThuTu) trong ca gần nhất —
            // khi có tách liệu, lấy dòng có ThuTu lớn nhất (dòng cuối cùng của Silo đó).
            var lastTonCuoiBySilo = tonSiloCandidates
                .GroupBy(t => t.SiloID)
                .ToDictionary(
                    g => g.Key,
                    g => {
                        var latest = g.OrderByDescending(t => t.NgaySX).ThenByDescending(t => t.Ca).First();
                        return g
                            .Where(t => t.NgaySX == latest.NgaySX && t.Ca == latest.Ca)
                            .OrderByDescending(t => t.ID)
                            .First().TonCuoi; 
                    });

            // ── 4. TonCuoiAuto/XuatAuto từ SP_TKVV_GetDuLieuCan (cùng 1 lần gọi, theo Silo) ──
            var tonCuoiAutoBySilo = new Dictionary<int, decimal>();
            var xuatAutoBySilo = new Dictionary<int, decimal>();
            try
            {
                var ngay = new DateTime(ngaySX.Year, ngaySX.Month, ngaySX.Day);
                var autoRows = await _bcslRepo.GetDuLieuCanAsync(ngay, ca, "TKVV_TONSILO", "TONSILO", scope);
                foreach (var g in autoRows.Where(r => r.SiloID.HasValue).GroupBy(r => r.SiloID!.Value))
                {
                    var first = g.First();
                    tonCuoiAutoBySilo[g.Key] = first.GiaTri;
                    xuatAutoBySilo[g.Key] = first.GiaTriXuat;
                }
            }
            catch { /* SP lỗi hoặc chưa có dữ liệu — không block */ }

            // ── 4b. NhapAuto/DoAm từ sp_TKVV_Get_NVL_BBGN (theo NVL, không theo Silo) ──
            // SP trả N dòng (N lần giao nhận) cho 1 TKVV_NVL_ID trong kíp — gọi ĐÚNG 1 LẦN
            // cho mỗi NVL (group trước khi gọi), không phải 1 lần / Silo. Không cộng dồn:
            // mỗi dòng SP đổ trực tiếp (DoAm_W, KhoiLuong_BG) vào ĐÚNG 1 Silo đang giữ NVL
            // đó, theo thứ tự — dòng SP thứ i → Silo thứ i (theo thứ tự MaSilo đã sort).
            // Silo thừa (không đủ dòng SP) để trống; dòng SP thừa (nhiều hơn số Silo) bỏ qua.
            var siloIdsByNvl = new Dictionary<int, List<int>>();
            foreach (var silo in silos)
            {
                if (!nearestMappingBySilo.TryGetValue(silo.ID, out var m) || m.NguyenVatLieuID <= 0) continue;
                if (!siloIdsByNvl.TryGetValue(m.NguyenVatLieuID, out var list))
                    siloIdsByNvl[m.NguyenVatLieuID] = list = new List<int>();
                list.Add(silo.ID);
            }

            var nhapAutoBySilo = new Dictionary<int, decimal>();
            var doAmAutoBySilo = new Dictionary<int, decimal>();
            var doAmTextBySilo = new Dictionary<int, string>();
            // Silo từ thứ 2 trở đi cùng NVL → Nhap/NhapAuto phải được xóa
            var nonFirstNvlSiloIds = new HashSet<int>();
            foreach (var (nvlId, siloIdsForNvl) in siloIdsByNvl)
            {
                // Track các silo không phải đầu tiên (dù SP có dữ liệu hay không)
                for (int i = 1; i < siloIdsForNvl.Count; i++)
                    nonFirstNvlSiloIds.Add(siloIdsForNvl[i]);

                try
                {
                    var ngay = new DateTime(ngaySX.Year, ngaySX.Month, ngaySX.Day);
                    var bbgnRows = await _nvlBbgnRepo.GetNvlBbgnDataAsync(ngay, ca, nvlId, scope);
                    if (bbgnRows.Count == 0) continue;

                    // Sum toàn bộ KhoiLuongBG → 1 giá trị NhapAuto chung cho cả NVL
                    var sumNhap = bbgnRows.Where(r => r.KhoiLuongBG.HasValue).Sum(r => r.KhoiLuongBG!.Value);
                    // Nối tất cả DoAmW → DoAmText hiển thị (vd "12.5, 13.0")
                    var doAmParts = bbgnRows.Where(r => r.DoAmW.HasValue)
                                            .Select(r => r.DoAmW!.Value.ToString("0.##"))
                                            .ToList();
                    var doAmText = string.Join(", ", doAmParts);
                    var firstDoAm = bbgnRows.FirstOrDefault(r => r.DoAmW.HasValue)?.DoAmW;

                    // DoAm, Nhap, DoAmText → chỉ silo đầu tiên của NVL
                    var firstSiloId = siloIdsForNvl[0];
                    if (firstDoAm.HasValue) doAmAutoBySilo[firstSiloId] = firstDoAm.Value;
                    if (sumNhap > 0) nhapAutoBySilo[firstSiloId] = sumNhap;
                    if (!string.IsNullOrEmpty(doAmText)) doAmTextBySilo[firstSiloId] = doAmText;
                }
                catch { /* SP lỗi hoặc chưa có mapping/dữ liệu cho NVL này — không block */ }
            }

            // ── 5. Bản ghi đã tồn tại trong DB cho ngaySX+Ca+Scope này ─────────────
            // Dùng List vì 1 Silo có thể có nhiều dòng do tách liệu.
            var existingRows = await _context.TKVV_TonSilo
                .Where(t => siloIds.Contains(t.SiloID)
                         && t.NgaySX == ngaySX && t.Ca == ca && t.Scope == scope
                         && !t.IsDelete)
                .ToListAsync();
            var existingBySilo = existingRows
                .GroupBy(t => t.SiloID)
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.ID).ToList());

            // ── 6. Upsert: INSERT mới hoặc UPDATE dòng gốc; dòng tách chỉ cập nhật TonDau ──
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var silo in silos)
                {
                    lastTonCuoiBySilo.TryGetValue(silo.ID, out var carryForward);
                    decimal? tonCuoiAuto = tonCuoiAutoBySilo.TryGetValue(silo.ID, out var av) ? av : null;
                    nearestMappingBySilo.TryGetValue(silo.ID, out var mapping);

                    decimal? nhapAuto = nhapAutoBySilo.TryGetValue(silo.ID, out var na) ? na : null;
                    decimal? doAmAuto = doAmAutoBySilo.TryGetValue(silo.ID, out var da) ? da : null;
                    string? doAmText = doAmTextBySilo.TryGetValue(silo.ID, out var dt) ? dt : null;
                    decimal? xuatAuto = xuatAutoBySilo.TryGetValue(silo.ID, out var xa) ? xa : null;

                    if (!existingBySilo.TryGetValue(silo.ID, out var siloRecs) || siloRecs.Count == 0)
                    {
                        // Chưa có bản ghi — INSERT với dữ liệu khởi tạo
                        var tonCuoi = tonCuoiAuto ?? 0m;
                        var rec = new TKVV_TonSilo
                        {
                            PhieuID = request.PhieuID,
                            NgaySX = ngaySX,
                            Ca = (byte)ca,
                            Scope = scope,
                            SiloID = silo.ID,
                            NguyenVatLieuID = mapping?.NguyenVatLieuID,
                            ThuTu = silos.IndexOf(silo) + 1,
                            TonDau = carryForward,
                            DoAm = doAmAuto,
                            DoAmText = doAmText,
                            Nhap = nhapAuto,
                            NhapAuto = nhapAuto,
                            Xuat = xuatAuto,
                            XuatAuto = xuatAuto,
                            TonCuoiAuto = tonCuoiAuto,
                            TonCuoi = tonCuoi,
                            IsAdjusted = false,
                            CreatedDate = DateTime.Now,
                            CreatedBy = request.CurrentUserId,
                        };
                        _context.TKVV_TonSilo.Add(rec);
                        existingBySilo[silo.ID] = new List<TKVV_TonSilo> { rec };
                    }
                    else
                    {
                        // Dòng gốc (ID nhỏ nhất) — cập nhật auto values
                        var rec = siloRecs[0];
                        var tonCuoi = rec.TonCuoi ?? tonCuoiAuto ?? 0m;
                        var isAdj = tonCuoiAuto.HasValue && tonCuoi != tonCuoiAuto.Value;
                        if (nonFirstNvlSiloIds.Contains(silo.ID))
                        {
                            rec.Nhap = null;
                            rec.NhapAuto = null;
                            rec.DoAm = null;
                            rec.DoAmText = null;
                        }
                        else
                        {
                            rec.Nhap ??= nhapAuto;
                            rec.NhapAuto = nhapAuto;
                            rec.DoAm ??= doAmAuto;
                            rec.DoAmText = doAmText;
                        }
                        rec.Xuat ??= xuatAuto;
                        rec.XuatAuto = xuatAuto;
                        rec.TonDau = carryForward;
                        rec.TonCuoiAuto = tonCuoiAuto;
                        rec.TonCuoi = tonCuoi;
                        rec.IsAdjusted = isAdj;
                        rec.AdjustedBy = isAdj ? request.CurrentUserId : null;
                        rec.AdjustedDate = isAdj ? DateTime.Now : null;
                        rec.UpdatedDate = DateTime.Now;

                        // Dòng tách (ID lớn hơn) — chỉ cập nhật TonDau, giữ nguyên phần còn lại
                        foreach (var tachRec in siloRecs.Skip(1))
                        {
                            tachRec.TonDau = carryForward;
                            tachRec.UpdatedDate = DateTime.Now;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            // ── 7. Trả về DTO đầy đủ — bao gồm tất cả dòng tách liệu ───────────────
            var result = new List<TKVVTonSiloRowDto>();
            foreach (var silo in silos)
            {
                nearestMappingBySilo.TryGetValue(silo.ID, out var mapping);
                if (!existingBySilo.TryGetValue(silo.ID, out var siloRecs) || siloRecs.Count == 0)
                {
                    result.Add(new TKVVTonSiloRowDto
                    {
                        SiloID = silo.ID,
                        MaSilo = silo.MaSilo,
                        TenSilo = silo.TenSilo,
                        NgaySX = ngaySX,
                        Ca = ca,
                        Scope = scope,
                    });
                    continue;
                }

                foreach (var rec in siloRecs)
                {
                    var nvl = rec.NguyenVatLieuID.HasValue
                        ? nvlList.FirstOrDefault(n => n.ID == rec.NguyenVatLieuID)
                        : null;
                    result.Add(new TKVVTonSiloRowDto
                    {
                        Id = rec.ID,
                        PhieuID = rec.PhieuID,
                        NgaySX = ngaySX,
                        Ca = ca,
                        Scope = scope,
                        ThuTu = rec.ThuTu,
                        SiloID = silo.ID,
                        MaSilo = silo.MaSilo,
                        TenSilo = silo.TenSilo,
                        NguyenVatLieuID = rec.NguyenVatLieuID,
                        TenNVL = nvl?.TenNVL,
                        DoAm = rec.DoAm,
                        DoAmText = rec.DoAmText,
                        TonDau = rec.TonDau,
                        Nhap = rec.Nhap,
                        NhapAuto = rec.NhapAuto,
                        Xuat = rec.Xuat,
                        XuatAuto = rec.XuatAuto,
                        TonCuoi = rec.TonCuoi,
                        TonCuoiAuto = rec.TonCuoiAuto,
                        GhiChu = rec.GhiChu,
                        IsAdjusted = rec.IsAdjusted,
                        IsTachLieu = rec.IsTachLieu,
                        AdjustedBy = rec.AdjustedBy,
                        AdjustedDate = rec.AdjustedDate,
                    });
                }
            }
            return result;
        }


        public async Task<List<TKVVTonSiloRowDto>> GetRowsByPhieuIdAsync(Guid phieuId)
        {
            var rows = await _context.TKVV_TonSilo
                .Where(t => t.PhieuID == phieuId && !t.IsDelete)
                .OrderBy(t => t.ThuTu)
                .ToListAsync();

            if (rows.Count == 0) return new List<TKVVTonSiloRowDto>();

            var siloIds = rows.Select(r => r.SiloID).Distinct().ToList();
            var silos = await _context.TKVV_Silo
                .Where(s => siloIds.Contains(s.ID))
                .ToDictionaryAsync(s => s.ID);

            var nvlIds = rows.Where(r => r.NguyenVatLieuID.HasValue)
                             .Select(r => r.NguyenVatLieuID!.Value).Distinct().ToList();
            var nvlList = await _context.TKVV_NguyenVatLieu
                .Where(n => nvlIds.Contains(n.ID))
                .ToDictionaryAsync(n => n.ID);

            return rows.Select(r =>
            {
                silos.TryGetValue(r.SiloID, out var silo);
                var nvl = r.NguyenVatLieuID.HasValue && nvlList.TryGetValue(r.NguyenVatLieuID.Value, out var n) ? n : null;
                return new TKVVTonSiloRowDto
                {
                    Id = r.ID,
                    PhieuID = r.PhieuID,
                    NgaySX = r.NgaySX,
                    Ca = r.Ca,
                    Scope = r.Scope,
                    ThuTu = r.ThuTu,
                    SiloID = r.SiloID,
                    MaSilo = silo?.MaSilo,
                    TenSilo = silo?.TenSilo,
                    NguyenVatLieuID = r.NguyenVatLieuID,
                    TenNVL = nvl?.TenNVL,
                    DoAm = r.DoAm,
                    DoAmText = r.DoAmText,
                    TonDau = r.TonDau,
                    Nhap = r.Nhap,
                    NhapAuto = r.NhapAuto,
                    Xuat = r.Xuat,
                    XuatAuto = r.XuatAuto,
                    TonCuoi = r.TonCuoi,
                    TonCuoiAuto = r.TonCuoiAuto,
                    GhiChu = r.GhiChu,
                    IsAdjusted = r.IsAdjusted,
                    IsTachLieu = r.IsTachLieu,
                    AdjustedBy = r.AdjustedBy,
                    AdjustedDate = r.AdjustedDate,
                };
            }).ToList();
        }

        public async Task SavePhieuRowsAsync(SaveTonSiloPhieuRequestDto request)
        {
            if (request.Rows.Count == 0) return;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in request.Rows)
                {
                    TKVV_TonSilo? rec = null;

                    if (row.Id.HasValue && row.Id > 0)
                        rec = await _context.TKVV_TonSilo.FindAsync(row.Id.Value);

                    // Fallback theo (NgaySX, Ca, Scope, SiloID, NguyenVatLieuID) — thêm NVL vào key
                    // để dòng tách cùng Silo nhưng khác NVL không bị ghi đè lên nhau → INSERT mới.
                    rec ??= await _context.TKVV_TonSilo.FirstOrDefaultAsync(x =>
                        x.NgaySX == row.NgaySX &&
                        x.Ca == row.Ca &&
                        x.Scope == row.Scope &&
                        x.SiloID == row.SiloID &&
                        x.NguyenVatLieuID == row.NguyenVatLieuID &&
                        !x.IsDelete);

                    var tonCuoi = row.TonCuoi ?? row.TonCuoiAuto ?? 0m;
                    var isAdjusted = row.IsAdjusted || (row.TonCuoiAuto.HasValue && tonCuoi != row.TonCuoiAuto.Value);

                    if (rec == null)
                    {
                        _context.TKVV_TonSilo.Add(new TKVV_TonSilo
                        {
                            PhieuID = request.PhieuID,
                            NgaySX = row.NgaySX,
                            Ca = (byte)row.Ca,
                            Scope = row.Scope,
                            SiloID = row.SiloID,
                            NguyenVatLieuID = row.NguyenVatLieuID,
                            Kip = row.Kip,
                            ThuTu = row.ThuTu,
                            DoAm = row.DoAm,
                            DoAmText = row.DoAmText,
                            TonDau = row.TonDau,
                            Nhap = row.Nhap,
                            NhapAuto = row.NhapAuto,
                            Xuat = row.Xuat,
                            XuatAuto = row.XuatAuto,
                            TonCuoi = tonCuoi,
                            TonCuoiAuto = row.TonCuoiAuto,
                            GhiChu = row.GhiChu,
                            IsAdjusted = isAdjusted,
                            IsTachLieu = row.IsTachLieu,
                            AdjustedBy = isAdjusted ? request.CurrentUserId : null,
                            AdjustedDate = isAdjusted ? DateTime.Now : null,
                            CreatedDate = DateTime.Now,
                            CreatedBy = request.CurrentUserId,
                        });
                    }
                    else
                    {
                        rec.PhieuID = request.PhieuID;
                        rec.NguyenVatLieuID = row.NguyenVatLieuID;
                        rec.Kip = row.Kip;
                        rec.ThuTu = row.ThuTu;
                        rec.DoAm = row.DoAm;
                        rec.DoAmText = row.DoAmText;
                        rec.TonDau = row.TonDau;
                        rec.Nhap = row.Nhap;
                        rec.NhapAuto = row.NhapAuto;
                        rec.Xuat = row.Xuat;
                        rec.XuatAuto = row.XuatAuto;
                        rec.TonCuoi = tonCuoi;
                        rec.TonCuoiAuto = row.TonCuoiAuto;
                        rec.GhiChu = row.GhiChu;
                        rec.IsAdjusted = isAdjusted;
                        rec.IsTachLieu = row.IsTachLieu;
                        rec.AdjustedBy = isAdjusted ? request.CurrentUserId : null;
                        rec.AdjustedDate = isAdjusted ? DateTime.Now : null;
                        rec.UpdatedDate = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
