using System;
using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.SqlClient;

namespace dataproduct.api.Repositories
{
    public class DLNMHRC2Repository   : IDLNMHRC2Repository   
    {
        private readonly ProductFormContext _context;
        private readonly ProductDataMasterDbContext _masterDataContext;
        public DLNMHRC2Repository (ProductFormContext context, ProductDataMasterDbContext masterDataContext)
        {
            _context = context;
            _masterDataContext = masterDataContext;
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

        public async Task<DLNM_HRC2?> GetByIdAsync(long id)
        {
            return await _context.DLNM_HRC2s.FirstOrDefaultAsync(x => x.ID == id);
        }

        public async Task<HRC2DetailByReportNoModel?> GetByReportNoAsync(int reportNo)
        {
            try
            {
                // Lấy 1 record DLNM_HRC2 per REPORT_NO
                var baseRecord = await _context.DLNM_HRC2s
                    .Where(x => x.REPORT_NO == reportNo)
                    .FirstOrDefaultAsync();

                if (baseRecord == null)
                {
                    return null;
                }

                // Lấy tất cả phụ liệu từ bảng PhuLieu_HRC2
                var phuLieuItems = await _context.PhuLieu_HRC2s
                    .Where(x => x.REPORT_NO == reportNo && x.ID_PhuLieu.HasValue)
                    .Select(x => new
                    {
                        ID_PhuLieu = x.ID_PhuLieu!.Value,
                        x.TenPhuLieu,
                        x.KLPhuGia
                    })
                    .GroupBy(x => x.ID_PhuLieu)
                    .Select(g => g.First())
                    .ToListAsync();

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
                            TenHienThi = header?.TenHienThi,
                            LoaiPhieu = header?.LoaiPhieu,
                            ThuTu = header != null && header.ThuTu.HasValue ? (int?)header.ThuTu.Value : null
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
                                TenNguonDuLieu = map.TenNguonDuLieu,
                                ThuTu = map.ThuTu
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
                        IsNM = baseRecord.IsNM,
                        KLGangLongCCT = FormatNumber(baseRecord.KLGangLongCCT),
                        KLGangLongCR = FormatNumber( baseRecord.KLGangLongCR),
                        KLThepLong = FormatNumber(baseRecord.KLThepLong)
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

        public async Task<HRC2GroupedByReportNoModel?> GetByMeThoiGroupedAsync(string meThoi)
        {
            try
            {
                // Lấy 1 record DLNM_HRC2 per REPORT_NO
                var baseRecord = await _context.DLNM_HRC2s
                    .Where(x => x.MeThoi == meThoi)
                    .FirstOrDefaultAsync();

                if (baseRecord == null)
                {
                    return null;
                }

                // Lấy tất cả phụ liệu từ bảng PhuLieu_HRC2
                var allPhuLieuData = await _context.PhuLieu_HRC2s
                    .Where(x => x.MeThoi == meThoi && x.ID_PhuLieu.HasValue)
                    .Select(x => new
                    {
                        ID_PhuLieu = x.ID_PhuLieu!.Value,
                        x.TenPhuLieu,
                        x.KLPhuGia,
                    })
                    .ToListAsync();

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
                                IsActive = header?.IsActive ?? false,
                                ThuTu = header != null && header.ThuTu.HasValue ? (int?)header.ThuTu.Value : null
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
                                        MappingId = map.Id,
                                        ThuTu = map.ThuTu
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
                        IsNM = baseRecord.IsNM,
                        IsChuyenCa = baseRecord.IsChuyenCa,
                        O2 = FormatNumber(baseRecord.O2),
                        AR_RH = FormatNumber(baseRecord.AR_RH),
                        N2 = FormatNumber(baseRecord.N2),
                        AR_BOF = FormatNumber(baseRecord.AR_BOF),
                        AR_LF = FormatNumber(baseRecord.AR_LF),
                        KLGangLong = FormatNumber(baseRecord.KLGangLong),
                        KLThepPhe = FormatNumber(baseRecord.KLThepPhe),
                        KLGangLongCCT = FormatNumber(baseRecord.KLGangLongCCT),
                        KLGangLongCR = FormatNumber(baseRecord.KLGangLongCR),
                        KLThepLong = FormatNumber(baseRecord.KLThepLong)
                    },
                    mappedPhulieus = groupedMappedPhuLieus.Values.ToList(),
                    unmappedPhulieus = groupedUnmappedPhuLieus.Values.ToList()
                };
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error while getting grouped report {meThoi}", ex);
            }
        }
        public async Task<HRC2GroupedByReportNoModel?> GetByReportNoGroupedAsync(int reportNo)
        {
            try
            {
                // Lấy 1 record DLNM_HRC2 per REPORT_NO
                var baseRecord = await _context.DLNM_HRC2s
                    .Where(x => x.REPORT_NO == reportNo)
                    .FirstOrDefaultAsync();

                if (baseRecord == null)
                {
                    return null;
                }

                // Lấy tất cả phụ liệu từ bảng PhuLieu_HRC2
                var allPhuLieuData = await _context.PhuLieu_HRC2s
                    .Where(x => x.REPORT_NO == reportNo && x.ID_PhuLieu.HasValue)
                    .Select(x => new
                    {
                        ID_PhuLieu = x.ID_PhuLieu!.Value,
                        x.TenPhuLieu,
                        x.KLPhuGia,
                    })
                    .ToListAsync();

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
                                IsActive = header?.IsActive ?? false,
                                ThuTu = header != null && header.ThuTu.HasValue ? (int?)header.ThuTu.Value : null
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
                                        MappingId = map.Id,
                                        ThuTu = map.ThuTu
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
                        KLThepPhe = FormatNumber(baseRecord.KLThepPhe),
                        KLGangLongCCT = FormatNumber(baseRecord.KLGangLongCCT),
                        KLGangLongCR = FormatNumber(baseRecord.KLGangLongCR),
                        KLThepLong = FormatNumber(baseRecord.KLThepLong)
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

        public async Task<HRC2GroupedByReportNoModel?>  GetByIdGroupedAsync(int id)
        {
            try
            {
                // Lấy 1 record DLNM_HRC2 per REPORT_NO
                var baseRecord = await _context.DLNM_HRC2s
                    .Where(x => x.ID == id)
                    .FirstOrDefaultAsync();

                if (baseRecord == null)
                {
                    return null;
                }

                // Lấy tất cả phụ liệu từ bảng PhuLieu_HRC2
                var allPhuLieuData = await _context.PhuLieu_HRC2s
                    .Where(x => x.ID_MeThoi == id && x.ID_PhuLieu.HasValue )
                    .Select(x => new
                    {
                        ID_PhuLieu = x.ID_PhuLieu!.Value,
                        x.TenPhuLieu,
                        x.KLPhuGia,
                    })
                    .ToListAsync();

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
                                IsActive = header?.IsActive ?? false,
                                ThuTu = header != null && header.ThuTu.HasValue ? (int?)header.ThuTu.Value : null
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
                                        MappingId = map.Id,
                                        ThuTu = map.ThuTu
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
                        IsNM = baseRecord.IsNM,
                        IsChuyenCa = baseRecord.IsChuyenCa,
                        O2 = FormatNumber(baseRecord.O2),
                        AR_RH = FormatNumber(baseRecord.AR_RH),
                        N2 = FormatNumber(baseRecord.N2),
                        AR_BOF = FormatNumber(baseRecord.AR_BOF),
                        AR_LF = FormatNumber(baseRecord.AR_LF),
                        KLGangLong = FormatNumber(baseRecord.KLGangLong),
                        KLThepPhe = FormatNumber(baseRecord.KLThepPhe),
                        KLGangLongCCT = FormatNumber(baseRecord.KLGangLongCCT),
                        KLGangLongCR = FormatNumber(baseRecord.KLGangLongCR),
                        KLThepLong = FormatNumber(baseRecord.KLThepLong),
                        IsTrungMeThoi = baseRecord.IsTrungMeThoi
                    },
                    mappedPhulieus = groupedMappedPhuLieus.Values.ToList(),
                    unmappedPhulieus = groupedUnmappedPhuLieus.Values.ToList()
                };
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error while getting grouped report {id}", ex);
            }
        }
        public async Task AddAsync(DLNM_HRC2 entity)
        {
            var existing = await _context.DLNM_HRC2s.Where(x => x.MeThoi == entity.MeThoi && x.BieuMau == entity.BieuMau).ToListAsync();
            if (existing != null)
            {
                foreach(var item in existing){
                    item.IsTrungMeThoi = true;
                    _context.DLNM_HRC2s.Update(item);
                }
            }
            _context.DLNM_HRC2s.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DLNM_HRC2 entity)
        {
            _context.DLNM_HRC2s.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var item = await _context.DLNM_HRC2s.FindAsync(id);
            if (item != null)
            {
                _context.DLNM_HRC2s.Remove(item);
                
                var relatedPhuLieuItems = await _context.PhuLieu_HRC2s
                    .Where(x => x.ID_MeThoi == id)
                    .ToListAsync();
                _context.PhuLieu_HRC2s.RemoveRange(relatedPhuLieuItems);
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
            int? caKQ = null;
            DateOnly? ngayKQ = null;
            if(request.ChuyenToiCa == 1){ // chuyển về ca trước
                if(request.Ca == 1){
                    caKQ = 2;
                    ngayKQ = request.NgaySX.AddDays(-1);
                }
                else{
                    caKQ = 1;
                    ngayKQ = request.NgaySX;
                }
            } else { // chuyển đến ca sau
                if(request.Ca == 2){
                    caKQ = 1;
                    ngayKQ = request.NgaySX.AddDays(1);
                }
                else{
                    caKQ = 2;
                    ngayKQ = request.NgaySX;
                }
            }

            var phieu = await _context.BmPhieus.FirstOrDefaultAsync(x => x.MaBm == request.MaBM && x.NgaySX == ngayKQ && x.Ca == caKQ && x.Scope == request.Scope && x.IsDelete == 0 && x.IsLock == 0);
            

            if(phieu == null)
                throw new ApplicationException("Phiếu chưa được tạo");
            if(phieu.TinhTrang != 0)
                throw new ApplicationException("Phiếu đã được gửi đi nên không nhận mẻ chuyển");
            
            // Cập nhật DLNM_HRC2 records
            var dlnmItems = await _context.DLNM_HRC2s
                .Where(x => x.MeThoi == request.MeThoi && x.BieuMau == request.BieuMau && x.Scope == request.Scope)
                .ToListAsync();

            if (dlnmItems.Count == 0)
                throw new ApplicationException("Phiếu không có dữ liệu");

            foreach (var item in dlnmItems)
            {
                item.Ngay = ngayKQ?.ToDateTime(TimeOnly.MinValue);
                item.Ca = caKQ;
                item.IsChuyenCa = true;
            }

            // Cập nhật PhuLieu_HRC2 records (nếu có)
            var phuLieuItems = await _context.PhuLieu_HRC2s
                .Where(x => x.MeThoi == request.MeThoi && x.BieuMau == request.BieuMau)
                .ToListAsync();

            await _context.SaveChangesAsync();
            return true;
        }

     

        /// <summary>
        /// Gọi stored procedure sp_GetHRC2GroupedByMaterial_Test để lấy dữ liệu phụ liệu theo ngày/ca.
        /// Sau đó map với Header_Mapping & Header_Key để quy đổi về HeaderKey và gộp những phụ liệu cùng HeaderKey.
        /// </summary>
        public async Task<IEnumerable<FilterSTD_NXTResponse>> GetHRC2GroupedByMaterialAsync(DateTime ngaySX, int ca)
        {
            var ngayParam = new SqlParameter("@NgaySX", ngaySX);
            var caParam = new SqlParameter("@Ca", ca);

            // 1) Lấy dữ liệu raw từ stored
            var raw = await _context.STD_NXT_Filters
                .FromSqlRaw("EXEC sp_GetHRC2GroupedByMaterial_Test @NgaySX, @Ca", ngayParam, caParam)
                .ToListAsync();

            if (raw == null || raw.Count == 0) return Enumerable.Empty<FilterSTD_NXTResponse>();

            // 2) Lấy danh sách ID_PhuLieu
            var phuLieuIds = raw.Select(x => (int)x.ID_PhuLieu).Distinct().ToList();

            // 3) Lấy mapping Header_Mapping và Header_Key
            var mappings = await _context.Header_Mappings
                .Where(m => phuLieuIds.Contains(m.ID_PhuLieu))
                .ToListAsync();

            var headerKeyIds = mappings.Select(m => m.ID_HeaderKey).Distinct().ToList();
            var headerKeys = await _context.Header_Keys
                .Where(h => headerKeyIds.Contains(h.Id))
                .ToListAsync();

            // 4) Map và group
            var mapped = raw.Select(r =>
            {
                var map = mappings.FirstOrDefault(m => m.ID_PhuLieu == (int)r.ID_PhuLieu);
                var headerKeyId = map?.ID_HeaderKey;
                var headerKeyName = headerKeys.FirstOrDefault(h => h.Id == headerKeyId)?.TenHienThi;
                return new
                {
                    Raw = r,
                    HeaderKeyId = headerKeyId,
                    HeaderKeyName = headerKeyName
                };
            });

            // Group: nếu có HeaderKeyId thì group theo HeaderKeyId, nếu không thì group theo ID_PhuLieu riêng
            var grouped = mapped.GroupBy(x =>
                x.HeaderKeyId.HasValue
                    ? $"HK_{x.HeaderKeyId}"
                    : $"PL_{(int)x.Raw.ID_PhuLieu}");

            var result = grouped.Select(g =>
            {
                var first = g.First().Raw;
                var headerKeyId = g.First().HeaderKeyId;
                var headerKeyName = g.First().HeaderKeyName;
                var total = g.Sum(x => x.Raw.TotalKLPhuGia);

                var phuLieus = g
                    .GroupBy(x => (int)x.Raw.ID_PhuLieu)
                    .Select(plGroup => new PhuLieuNM
                    {
                        ID_PhuLieu = plGroup.Key,
                        TenPhuLieu = plGroup.First().Raw.TenPhuLieu,
                    })
                    .ToList();

                return new FilterSTD_NXTResponse
                {
                    BieuMau = first.BieuMau,
                    Scope = (int)first.Scope,
                    PhuLieus = phuLieus,
                    TotalKLPhuGia = (double?)total,
                    HeaderKeyId = headerKeyId,
                    HeaderKeyName = headerKeyName
                };
            }).ToList();

            return result;
        }

    }
}
