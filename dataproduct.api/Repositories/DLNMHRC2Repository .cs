using System;
using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class DLNMHRC2Repository   : IDLNMHRC2Repository   
    {
        private readonly ProductFormContext _context;
        public DLNMHRC2Repository (ProductFormContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<DLNM_HRC2>> GetAllAsync(DateTime? Ngay,int? Ca, string? BieuMau, int? Scope)
        {
            var query = _context.DLNM_HRC2s.AsQueryable();

            if (Ngay.HasValue)
                query = query.Where(x => x.Ngay == Ngay.Value.Date);

            if (Ca.HasValue)
                query = query.Where(x => x.Ca == Ca.Value);

            if (!string.IsNullOrEmpty(BieuMau))
                query = query.Where(x => x.BieuMau == BieuMau);

            if (Scope.HasValue)
                query = query.Where(x => x.Scope == Scope.Value);

            return await query.ToListAsync();
        }

        public async Task<DLNM_HRC2?> GetByIdAsync(int id)
        {
            return await _context.DLNM_HRC2s.FirstOrDefaultAsync(x => x.REPORT_NO == id);
        }

        public async Task<HRC2DetailByReportNoModel?> GetByReportNoAsync(int reportNo)
        {
            try
        {
            var data = await _context.DLNM_HRC2s
                .Where(x => x.REPORT_NO == reportNo)
                .OrderBy(x => x.ID)
                    .ToListAsync();

                if (!data.Any())
                {
                    return null;
                }

                var baseRecord = data.First();

                var phuLieuItems = data
                    .Where(x => x.ID_PhuLieu.HasValue)
                    .Select(x => new
                    {
                        ID_PhuLieu = x.ID_PhuLieu!.Value,
                        x.TenPhuLieu,
                        x.KLPhuGia
                    })
                    .GroupBy(x => x.ID_PhuLieu)
                    .Select(g => g.First())
                    .ToList();

                var phuLieuIds = phuLieuItems
                    .Select(x => x.ID_PhuLieu)
                    .ToList();

                var mappings = await _context.Header_Mappings
                .Where(m => phuLieuIds.Contains(m.ID_PhuLieu))
                .ToListAsync();

                var headerKeyIds = mappings.Select(m => m.ID_HeaderKey).Distinct().ToList();

                var headerKeys = await _context.Header_Keys
                    .Where(k => headerKeyIds.Contains(k.Id))
                    .ToDictionaryAsync(k => k.Id);

                var mappingLookup = mappings
                    .GroupBy(m => m.ID_PhuLieu)
                    .ToDictionary(
                        g => g.Key,
                    g => g.Select(m =>
                    {
                        headerKeys.TryGetValue(m.ID_HeaderKey, out var header);
                        return new
                        {
                            m.Id,
                            m.ID_HeaderKey,
                            m.TenNguonDuLieu,
                            KeyGuid = header?.KeyGuid,
                            TenHienThi = header?.TenHienThi
                        };
                    }).ToList());

                var phuLieus = new List<HeaderKey_ResponeModels>();

                foreach (var item in phuLieuItems)
                {
                    if (mappingLookup.TryGetValue(item.ID_PhuLieu, out var mapEntries) && mapEntries.Any())
                    {
                        foreach (var map in mapEntries)
                        {
                            phuLieus.Add(new HeaderKey_ResponeModels
                            {
                                MappingId = map.Id,
                                ID_PhuLieu = item.ID_PhuLieu,
                                TenPhuLieu = item.TenPhuLieu,
                                KLPhuGia = item.KLPhuGia,
                                ID_HeaderKey = map.ID_HeaderKey,
                                KeyGuid = map.KeyGuid,
                                TenHienThi = map.TenHienThi,
                                TenNguonDuLieu = map.TenNguonDuLieu
                            });
                        }
                    }
                    else
                    {
                        phuLieus.Add(new HeaderKey_ResponeModels
                        {
                        MappingId = null,
                            ID_PhuLieu = item.ID_PhuLieu,
                            TenPhuLieu = item.TenPhuLieu,
                            KLPhuGia = item.KLPhuGia
                        });
                    }
                }

                var response = new HRC2DetailByReportNoModel
            {
                    data = new DLNM_HRC2_ResponseModels
                    {
                        ID = baseRecord.ID,
                        REPORT_NO = baseRecord.REPORT_NO,
                        NgaySx = baseRecord.NgaySx,
                        Ngay = baseRecord.Ngay,
                        Ca = baseRecord.Ca,
                        BieuMau = baseRecord.BieuMau,
                        Scope = baseRecord.Scope,
                        MeThoi = baseRecord.MeThoi,
                        MacThep = baseRecord.MacThep,
                        O2 = FormatNumber(baseRecord.O2),
                        AR_RH = FormatNumber(baseRecord.AR_RH),
                        N2 = FormatNumber(baseRecord.N2),
                        AR_BOF = FormatNumber(baseRecord.AR_BOF),
                        AR_LF = FormatNumber(baseRecord.AR_LF),
                        KLGangLong = FormatNumber(baseRecord.KLGangLong),
                        KLThepPhe = FormatNumber(baseRecord.KLThepPhe),
                        IsNM = baseRecord.IsNM
                    },
                    phulieus = phuLieus
                };

                foreach (var item in response.phulieus)
                {
                    item.KLPhuGia = FormatNumber(item.KLPhuGia);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error while getting report {reportNo}", ex);
            }
        }

        public async Task<HRC2GroupedByReportNoModel?> GetByReportNoGroupedAsync(int reportNo)
        {
            try
            {
                var data = await _context.DLNM_HRC2s
                    .Where(x => x.REPORT_NO == reportNo)
                    .OrderBy(x => x.ID)
                    .ToListAsync();

                if (!data.Any())
                {
                    return null;
                }

                var baseRecord = data.First();

                // Lấy tất cả phụ liệu với KLPhuGia
                var allPhuLieuData = data
                    .Where(x => x.ID_PhuLieu.HasValue)
                    .Select(x => new
                    {
                        ID_PhuLieu = x.ID_PhuLieu!.Value,
                        x.TenPhuLieu,
                        x.KLPhuGia,
                    })
                    .ToList();

                var phuLieuIds = allPhuLieuData
                    .Select(x => x.ID_PhuLieu)
                    .Distinct()
                    .ToList();

                // Lấy mappings
                var mappings = await _context.Header_Mappings
                    .Where(m => phuLieuIds.Contains(m.ID_PhuLieu))
                    .ToListAsync();

                var headerKeyIds = mappings.Select(m => m.ID_HeaderKey).Distinct().ToList();

                var headerKeys = await _context.Header_Keys
                    .Where(k => headerKeyIds.Contains(k.Id))
                    .ToDictionaryAsync(k => k.Id);

                // Tạo lookup mapping theo ID_PhuLieu
                var mappingLookup = mappings
                    .GroupBy(m => m.ID_PhuLieu)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(m =>
                        {
                            headerKeys.TryGetValue(m.ID_HeaderKey, out var header);
                            return new
                            {
                                m.Id,
                                m.ID_HeaderKey,
                                m.TenNguonDuLieu,
                                KeyGuid = header?.KeyGuid,
                                TenHienThi = header?.TenHienThi,
                                LoaiPhieu = header?.LoaiPhieu,
                                IsActive = header?.IsActive ?? false
                            };
                        }).ToList());

                // Group phụ liệu đã mapped theo idHeaderKey
                var groupedMappedPhuLieus = new Dictionary<int, HeaderKeyGroupedByReportNoModel>();
                // Group phụ liệu chưa mapped theo tên
                var groupedUnmappedPhuLieus = new Dictionary<string, HeaderKeyGroupedByReportNoModel>();

                foreach (var phuLieuItem in allPhuLieuData)
                {
                    if (mappingLookup.TryGetValue(phuLieuItem.ID_PhuLieu, out var mapEntries) && mapEntries.Any())
                    {
                        var activeMaps = mapEntries
                            .Where(m => m.IsActive && m.KeyGuid.HasValue)
                            .ToList();

                        if (activeMaps.Any())
                        {
                            // Phụ liệu đã mapped: group theo idHeaderKey
                            foreach (var map in activeMaps)
                            {
                                var headerKeyId = map.ID_HeaderKey;
                                var formattedValue = FormatNumber(phuLieuItem.KLPhuGia);
                                if (!groupedMappedPhuLieus.ContainsKey(headerKeyId))
                                {
                                    groupedMappedPhuLieus[headerKeyId] = new HeaderKeyGroupedByReportNoModel
                                    {
                                        ID_HeaderKey = map.ID_HeaderKey,
                                        KeyGuid = map.KeyGuid,
                                        TenHienThi = map.TenHienThi,
                                        TenNguonDuLieu = map.TenNguonDuLieu,
                                        ID_PhuLieu = phuLieuItem.ID_PhuLieu,
                                        TenPhuLieu = phuLieuItem.TenPhuLieu,
                                        KLPhuGia = formattedValue,
                                        KLPhuGiaTotal = 0,
                                        LoaiPhuLieu = map.LoaiPhieu,
                                        MappingId = map.Id
                                    };
                                }
                                // Sum KLPhuGia
                                groupedMappedPhuLieus[headerKeyId].KLPhuGiaTotal = (groupedMappedPhuLieus[headerKeyId].KLPhuGiaTotal ?? 0) + (formattedValue ?? 0);
                            }
                            continue;
                        }

                        // Có mapping nhưng toàn bộ Header Key đã bị vô hiệu → bỏ qua hoàn toàn
                        if (mapEntries.Any())
                        {
                            continue;
                        }
                    }

                    // Phụ liệu chưa mapped (không có bản ghi mapping nào)
                    var groupKey = (phuLieuItem.TenPhuLieu ?? $"PL_{phuLieuItem.ID_PhuLieu}")?.Trim() ?? $"PL_{phuLieuItem.ID_PhuLieu}";
                    if (!groupedUnmappedPhuLieus.ContainsKey(groupKey))
                    {
                        groupedUnmappedPhuLieus[groupKey] = new HeaderKeyGroupedByReportNoModel
                        {
                            ID_HeaderKey = null,
                            KeyGuid = null,
                            TenHienThi = null,
                            TenNguonDuLieu = phuLieuItem.TenPhuLieu,
                            ID_PhuLieu = phuLieuItem.ID_PhuLieu,
                            TenPhuLieu = phuLieuItem.TenPhuLieu,
                            KLPhuGia = FormatNumber(phuLieuItem.KLPhuGia),
                            KLPhuGiaTotal = 0,
                            LoaiPhuLieu = null // Unmapped phụ liệu không có LoaiPhieu từ Header_Key
                        };
                    }
                    // Sum KLPhuGia
                    groupedUnmappedPhuLieus[groupKey].KLPhuGiaTotal = (groupedUnmappedPhuLieus[groupKey].KLPhuGiaTotal ?? 0) + (FormatNumber(phuLieuItem.KLPhuGia) ?? 0);
                }

                foreach (var item in groupedMappedPhuLieus.Values)
                {
                    item.KLPhuGia = FormatNumber(item.KLPhuGia);
                    item.KLPhuGiaTotal = FormatNumber(item.KLPhuGiaTotal);
                }

                foreach (var item in groupedUnmappedPhuLieus.Values)
                {
                    item.KLPhuGia = FormatNumber(item.KLPhuGia);
                    item.KLPhuGiaTotal = FormatNumber(item.KLPhuGiaTotal);
                }

                return new HRC2GroupedByReportNoModel
                {
                    data = new DLNM_HRC2_ResponseModels
                    {
                        ID = baseRecord.ID,
                        REPORT_NO = baseRecord.REPORT_NO,
                        NgaySx = baseRecord.NgaySx,
                        Ngay = baseRecord.Ngay,
                        Ca = baseRecord.Ca,
                        BieuMau = baseRecord.BieuMau,
                        Scope = baseRecord.Scope,
                        MeThoi = baseRecord.MeThoi,
                        MacThep = baseRecord.MacThep,
                        O2 = FormatNumber(baseRecord.O2),
                        AR_RH = FormatNumber(baseRecord.AR_RH),
                        N2 = FormatNumber(baseRecord.N2),
                        AR_BOF = FormatNumber(baseRecord.AR_BOF),
                        AR_LF = FormatNumber(baseRecord.AR_LF),
                        KLGangLong = FormatNumber(baseRecord.KLGangLong),
                        KLThepPhe = FormatNumber(baseRecord.KLThepPhe)
                    },
                    mappedPhulieus = groupedMappedPhuLieus.Values.ToList(),
                    unmappedPhulieus = groupedUnmappedPhuLieus.Values.ToList()
                };
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error while getting grouped report {reportNo}", ex);
        }
        }
        public async Task AddAsync(DLNM_HRC2 entity)
        {
            _context.DLNM_HRC2s.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DLNM_HRC2 entity)
        {
            _context.DLNM_HRC2s.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.DLNM_HRC2s.FindAsync(id);
            if (item != null)
            {
                _context.DLNM_HRC2s.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.DLNM_HRC2s.AnyAsync(e => e.REPORT_NO == id);
        }

        public async Task<(IEnumerable<DLNM_HRC2> Data, int TotalCount)> SearchWithPagingAsync(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, string? searchText, int page, int pageSize)
        {
            var query = _context.DLNM_HRC2s.AsQueryable();

            if (NgaySX.HasValue)
                query = query.Where(x => x.Ngay.HasValue && x.Ngay.Value.Date == NgaySX.Value.Date);

            if (Ca.HasValue)
                query = query.Where(x => x.Ca == Ca.Value);

            if (!string.IsNullOrEmpty(LoaiBM))
                query = query.Where(x => x.BieuMau == LoaiBM);

            if (Scope.HasValue)
                query = query.Where(x => x.Scope == Scope.Value);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var search = searchText!.Trim();
                if (int.TryParse(search, out var searchReportNo))
                {
                    query = query.Where(x => x.REPORT_NO == searchReportNo);
                }
                else
                {
                    query = query.Where(x =>
                        (x.MacThep ?? string.Empty).Contains(search) ||
                        (x.MeThoi ?? string.Empty).Contains(search));
                }
            }

            // Đếm số lượng REPORT_NO duy nhất
            var totalCount = await query.Select(x => x.REPORT_NO).Distinct().CountAsync();

            // Load tất cả dữ liệu vào memory và group ở đó để tránh lỗi EF Core
            var allData = await query
                .OrderBy(x => x.ID)
                .ToListAsync();

            // Group by REPORT_NO và lấy record đầu tiên của mỗi group
            var groupedData = allData
                .GroupBy(x => x.REPORT_NO)
                .Select(g => g.First())
                .OrderByDescending(x => x.Ngay)
                .ThenByDescending(x => x.REPORT_NO)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (groupedData, totalCount);
        }

        private static double? FormatNumber(double? value)
        {
            if (!value.HasValue) return null;

            var rounded = Math.Round(value.Value, 2, MidpointRounding.AwayFromZero);
            if (Math.Abs(rounded % 1) < 0.0000001)
            {
                return Math.Truncate(rounded);
            }
            return rounded;
        }

        public async Task<bool> ChuyenMeThoiAsync(ChuyenMeThoiRequest request)
        {
            var items = await _context.DLNM_HRC2s.Where(x => x.MeThoi == request.MeThoi).ToListAsync();
            if (items.Count == 0) return false;
            foreach (var item in items)
            {
                var ca = item.Ca;
                if(ca == 2) {
                    var NgaySau = item.Ngay.Value.AddDays(1);
                    item.Ngay = NgaySau;
                    item.Ca = 1;
                } else{
                    item.Ca = 2;
                }
                item.IsChuyenCa = true;
            }
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
