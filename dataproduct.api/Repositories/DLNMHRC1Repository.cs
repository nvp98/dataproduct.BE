using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class DLNMHRC1Repository : IDLNMHRC1Repository
    {
        private readonly ProductFormContext _context;

        public DLNMHRC1Repository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Hrc1TieuHaoBof>> GetAllAsync(DateOnly? ngaySanXuat, int? ca, int? scope, string? bieuMau = "BOF")
        {
            var query = _context.Hrc1TieuHaoBofs.Where(x => x.IsDeleted == false).AsQueryable();

            if (ngaySanXuat.HasValue)
                query = query.Where(x => x.NgaySanXuat == ngaySanXuat.Value);

            if (ca.HasValue)
                query = query.Where(x => x.Ca == ca.Value);

            if (scope.HasValue)
                query = query.Where(x => x.Scope == scope.Value);

            if (!string.IsNullOrEmpty(bieuMau))
                query = query.Where(x => x.BieuMau == bieuMau);

            return await query.OrderBy(x => x.MeThoi).ToListAsync();
        }

        public async Task<List<Hrc1GroupedByMeThoiModel>> GetAllGroupedBatchAsync(IEnumerable<Hrc1TieuHaoBof> baseList)
        {
            var bases = baseList.ToList();
            if (bases.Count == 0) return new List<Hrc1GroupedByMeThoiModel>();

            var ids = bases.Select(x => x.ID).ToList();

            // Toàn bộ dòng phụ liệu (13 phụ liệu cố định + manual_col_*) cho các mẻ này
            var allPhuLieuRaw = await _context.Hrc1PhuLieus
                .Where(x => ids.Contains(x.MeID) && x.IsDeleted == false)
                .ToListAsync();

            // Danh mục 13 phụ liệu cố định — lấy tên hiển thị nếu dòng chưa denormalize TenPhuLieu
            var phuLieuNmIds = allPhuLieuRaw
                .Where(x => x.PhuLieuID.HasValue)
                .Select(x => x.PhuLieuID!.Value)
                .Distinct()
                .ToList();
            var phuLieuNmMap = phuLieuNmIds.Count > 0
                ? await _context.Hrc1PhuLieuNms.Where(n => phuLieuNmIds.Contains(n.ID)).ToDictionaryAsync(n => n.ID)
                : new Dictionary<int, Hrc1PhuLieuNm>();

            // Header_Key (bảng dùng chung với HRC2) cho các cột điều chỉnh tự do (manual_col_*)
            var headerKeyIds = allPhuLieuRaw
                .Where(x => x.ID_HeaderKey.HasValue)
                .Select(x => x.ID_HeaderKey!.Value)
                .Distinct()
                .ToList();
            var headerKeyMap = headerKeyIds.Count > 0
                ? await _context.Header_Keys.Where(k => headerKeyIds.Contains(k.Id)).ToDictionaryAsync(k => k.Id)
                : new Dictionary<int, Header_Key>();

            var byMeId = allPhuLieuRaw.GroupBy(x => x.MeID).ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<Hrc1GroupedByMeThoiModel>(bases.Count);
            foreach (var b in bases)
            {
                var model = new Hrc1GroupedByMeThoiModel { data = MapData(b) };

                if (byMeId.TryGetValue(b.ID, out var rows))
                {
                    foreach (var r in rows)
                    {
                        if (r.PhuLieuID.HasValue)
                        {
                            phuLieuNmMap.TryGetValue(r.PhuLieuID.Value, out var nm);
                            var tenPhuLieu = r.TenPhuLieu ?? nm?.TenPhuLieu;

                            model.phuLieus.Add(new HeaderKeyGroupedByReportNoModel
                            {
                                ID_PhuLieu = r.PhuLieuID.Value,
                                TenPhuLieu = tenPhuLieu,
                                KLPhuGia = (double?)r.KLPhuGia,
                                KLPhuGiaTotal = (double?)r.KLPhuGia,
                                IsManual = r.IsManual,
                                KLPhuGia_Manual = (double?)r.KLPhuGia_Manual,
                                ThuTu = nm?.ThuTu,
                            });

                            // KLPhanBo: 1 cột duy nhất trên cùng dòng (không phải record riêng như HRC2)
                            if (r.KLPhanBo.HasValue)
                            {
                                model.phanBoPhulieus.Add(new HeaderKeyGroupedByReportNoModel
                                {
                                    ID_PhuLieu = r.PhuLieuID.Value,
                                    TenPhuLieu = tenPhuLieu,
                                    KLPhuGia = (double?)r.KLPhanBo,
                                    KLPhuGiaTotal = (double?)r.KLPhanBo,
                                });
                            }
                        }
                        else if (r.IsManual && r.IsAddManual && r.ID_HeaderKey.HasValue)
                        {
                            headerKeyMap.TryGetValue(r.ID_HeaderKey.Value, out var hk);
                            model.manualAdjustPhulieus.Add(new HeaderKeyGroupedByReportNoModel
                            {
                                ID_HeaderKey = r.ID_HeaderKey.Value,
                                TenHienThi = hk?.TenHienThi,
                                KLPhuGia_Manual = (double?)r.KLPhuGia_Manual,
                                IsManual = true,
                            });
                        }
                    }
                }

                result.Add(model);
            }

            return result;
        }

        // =========================================================
        // Thống kê tiêu hao BOF (search / sum) — dùng cho ThongKeTieuHaoBOF.tsx
        // =========================================================

        private IQueryable<Hrc1TieuHaoBof> BuildThongKeQuery(SearchThongKeHrc1 dto)
        {
            var query = _context.Hrc1TieuHaoBofs.Where(x => x.BieuMau == "BOF").AsQueryable();

            if (dto.TuNgay.HasValue)
                query = query.Where(x => x.NgaySanXuat >= DateOnly.FromDateTime(dto.TuNgay.Value));
            if (dto.DenNgay.HasValue)
                query = query.Where(x => x.NgaySanXuat <= DateOnly.FromDateTime(dto.DenNgay.Value));
            if (dto.Ca.HasValue)
                query = query.Where(x => x.Ca == dto.Ca.Value);
            if (dto.Scope.HasValue)
                query = query.Where(x => x.Scope == dto.Scope.Value);
            if (!string.IsNullOrWhiteSpace(dto.SearchText))
                query = query.Where(x => x.MeThoi != null && x.MeThoi.Contains(dto.SearchText));
            if (dto.IsTrungMeThoi == true)
                query = query.Where(x => x.IsTrungMeThoi == true);

            query = dto.IsDelete == true ? query.Where(x => x.IsDeleted) : query.Where(x => !x.IsDeleted);

            return query;
        }

        private static double ComputeEffectiveTotal(Hrc1PhuLieu p)
        {
            var effective = p.IsManual ? (double)(p.KLPhuGia_Manual ?? 0) : (double)(p.KLPhuGia ?? 0);
            return effective + (double)(p.KLPhanBo ?? 0);
        }

        public async Task<SearchThongKeHrc1ApiResponse> SearchThongKeApiAsync(SearchThongKeHrc1 dto)
        {
            var query = BuildThongKeQuery(dto)
                .OrderByDescending(x => x.NgaySanXuat).ThenBy(x => x.Ca).ThenBy(x => x.Scope).ThenBy(x => x.MeThoi);

            var totalRecords = await query.CountAsync();

            var page = dto.Page is > 0 ? dto.Page.Value : 1;
            var pageSize = dto.PageSize is > 0 ? dto.PageSize.Value : 20;
            var pageItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var headerTables = await _context.Hrc1PhuLieuNms
                .Where(x => x.DangSuDung)
                .OrderBy(x => x.ThuTu ?? int.MaxValue).ThenBy(x => x.ID)
                .Select(x => new Hrc1PhuLieuHeaderTable { PhuLieuID = x.ID, TenPhuLieu = x.TenPhuLieu, ThuTu = x.ThuTu })
                .ToListAsync();

            var meIds = pageItems.Select(x => x.ID).ToList();
            var plByMeId = meIds.Count > 0
                ? (await _context.Hrc1PhuLieus
                    .Where(x => meIds.Contains(x.MeID) && !x.IsDeleted && x.PhuLieuID.HasValue)
                    .ToListAsync())
                    .GroupBy(x => x.MeID)
                    .ToDictionary(g => g.Key, g => g.ToList())
                : new Dictionary<int, List<Hrc1PhuLieu>>();

            var rows = pageItems.Select(b =>
            {
                var row = new Hrc1ThongKeRow { Data = MapData(b) };
                if (plByMeId.TryGetValue(b.ID, out var pls))
                {
                    row.Values = pls.Select(p => new Hrc1ThongKeValue
                    {
                        PhuLieuID = p.PhuLieuID!.Value,
                        KLPhuGia = (double?)p.KLPhuGia,
                        KLPhuGia_Manual = (double?)p.KLPhuGia_Manual,
                        IsManual = p.IsManual,
                        KLPhanBo = (double?)p.KLPhanBo,
                        TotalKLPhuGia = ComputeEffectiveTotal(p),
                    }).ToList();
                }
                return row;
            }).ToList();

            return new SearchThongKeHrc1ApiResponse
            {
                PhuLieuHeaderTables = headerTables,
                Data = rows,
                TotalRecords = totalRecords,
                Page = page,
                PageSize = pageSize,
                TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 0,
            };
        }

        public async Task<List<ThongKeSumItemHrc1>> GetThongKeSumAsync(SearchThongKeHrc1 dto)
        {
            var meIds = await BuildThongKeQuery(dto).Select(x => x.ID).ToListAsync();
            if (meIds.Count == 0) return new List<ThongKeSumItemHrc1>();

            var plRows = await _context.Hrc1PhuLieus
                .Where(x => meIds.Contains(x.MeID) && !x.IsDeleted && x.PhuLieuID.HasValue)
                .ToListAsync();
            if (plRows.Count == 0) return new List<ThongKeSumItemHrc1>();

            var nmNames = await _context.Hrc1PhuLieuNms.ToDictionaryAsync(x => x.ID, x => x.TenPhuLieu);

            return plRows
                .GroupBy(x => x.PhuLieuID!.Value)
                .Select(g => new ThongKeSumItemHrc1
                {
                    PhuLieuID = g.Key,
                    TenPhuLieu = nmNames.TryGetValue(g.Key, out var n) ? n : null,
                    TotalKLPhuGia = g.Sum(ComputeEffectiveTotal),
                })
                .ToList();
        }

        private static Hrc1TieuHaoBof_ResponseModel MapData(Hrc1TieuHaoBof b) => new Hrc1TieuHaoBof_ResponseModel
        {
            ID = b.ID,
            BieuMau = b.BieuMau,
            Scope = b.Scope,
            MeThoi = b.MeThoi,
            MacThep = b.MacThep,
            IsNM = b.IsNM,
            IsChuyenCa = b.IsChuyenCa,
            IsTrungMeThoi = b.IsTrungMeThoi,
            KLGang = b.KLGang,
            KLGangLongCCT = b.KLGangLongCCT,
            KLThepPhe = b.KLThepPhe,
            KLThepPheGang = b.KLThepPheGang,
            O2 = b.O2,
            N2 = b.N2,
            AR = b.AR,
            QueLayMau = b.QueLayMau,
            QueDoNhiet = b.QueDoNhiet,
            GhiChu = b.GhiChu,
            NgaySanXuat = b.NgaySanXuat,
            Ca = b.Ca,
            ThoiDiemBatDau = b.ThoiDiemBatDau,
            ThoiDiemKetThuc = b.ThoiDiemKetThuc,
        };
    }
}
