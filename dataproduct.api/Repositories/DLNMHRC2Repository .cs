using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.ResponseModels;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;

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

            return await query.OrderBy(x => x.MeThoi).ToListAsync();
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

                // Lấy tất cả phụ liệu từ bảng PhuLieu_HRC2 (CHỈ lấy phụ liệu thực tế, không lấy phân bổ)
                var phuLieuItems = await _context.PhuLieu_HRC2s
                    .Where(x => 
                        x.REPORT_NO == reportNo && 
                        x.ID_PhuLieu.HasValue &&
                        (x.IsPhanBo != true)) // ⭐ CHỈ lấy phụ liệu thực tế, không lấy phân bổ
                    .Select(x => new
                    {
                        ID_PhuLieu = x.ID_PhuLieu!.Value,
                        x.TenPhuLieu,
                        x.KLPhuGia,
                        KLPhuGia_Manual = x.KLPhuGia_Manual,
                        IsManual = x.IsManual
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

                // Lấy tất cả phụ liệu từ bảng PhuLieu_HRC2 (CHỈ lấy phụ liệu thực tế, không lấy phân bổ)
                var allPhuLieuData = await _context.PhuLieu_HRC2s
                    .Where(x => 
                        x.MeThoi == meThoi && 
                        x.ID_PhuLieu.HasValue &&
                        (x.IsPhanBo != true)) // ⭐ CHỈ lấy phụ liệu thực tế, không lấy phân bổ
                    .Select(x => new
                    {
                        ID_PhuLieu = x.ID_PhuLieu!.Value,
                        x.TenPhuLieu,
                        x.KLPhuGia,
                        x.IsManual,
                        x.KLPhuGia_Manual
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
                                var formattedManual = FormatNumber(phuLieuItem.KLPhuGia_Manual);
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
                                        KLPhuGia_Manual = formattedManual,
                                        IsManual = phuLieuItem.IsManual,
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

                // ========== Lấy dữ liệu phân bổ (IsPhanBo = true) và nhóm theo HeaderKey ==========
                var phanBoData = await _context.PhuLieu_HRC2s
                    .Where(x => 
                        x.MeThoi == meThoi && 
                        x.IsPhanBo == true &&
                        x.ID_HeaderKey.HasValue)
                    .Select(x => new
                    {
                        x.ID_HeaderKey,
                        x.KLPhuGia,
                        x.TenHienThi,
                    })
                    .ToListAsync();

                // Nhóm phân bổ theo ID_HeaderKey
                var groupedPhanBoPhuLieus = new Dictionary<int, HeaderKeyGroupedByReportNoModel>();
                foreach (var phanBoItem in phanBoData)
                {
                    if (!phanBoItem.ID_HeaderKey.HasValue) continue;
                    var headerKeyId = phanBoItem.ID_HeaderKey.Value;
                    var formattedValue = FormatNumber(phanBoItem.KLPhuGia);
                    
                    if (!groupedPhanBoPhuLieus.ContainsKey(headerKeyId))
                    {
                        groupedPhanBoPhuLieus[headerKeyId] = new HeaderKeyGroupedByReportNoModel
                        {
                            ID_HeaderKey = headerKeyId,
                            TenHienThi = phanBoItem.TenHienThi,
                            KLPhuGia = formattedValue,
                            KLPhuGiaTotal = 0,
                        };
                    }
                    // Sum KLPhuGia
                    groupedPhanBoPhuLieus[headerKeyId].KLPhuGiaTotal = (groupedPhanBoPhuLieus[headerKeyId].KLPhuGiaTotal ?? 0) + (formattedValue ?? 0);
                }

                foreach (var item in groupedPhanBoPhuLieus.Values)
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
                    unmappedPhulieus = groupedUnmappedPhuLieus.Values.ToList(),
                    phanBoPhulieus = groupedPhanBoPhuLieus.Values.ToList()
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

                // Lấy tất cả phụ liệu từ bảng PhuLieu_HRC2 (CHỈ lấy phụ liệu thực tế, không lấy phân bổ)
                var allPhuLieuData = await _context.PhuLieu_HRC2s
                    .Where(x => 
                        x.REPORT_NO == reportNo && 
                        x.ID_PhuLieu.HasValue &&
                        (x.IsPhanBo != true)) // ⭐ CHỈ lấy phụ liệu thực tế, không lấy phân bổ
                    .Select(x => new
                    {
                        ID_PhuLieu = x.ID_PhuLieu!.Value,
                        x.TenPhuLieu,
                        x.KLPhuGia,
                        x.IsManual,
                        x.KLPhuGia_Manual
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
                                var formattedManualValue = FormatNumber(phuLieuItem.KLPhuGia_Manual);
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
                                        KLPhuGia_Manual = formattedManualValue,
                                        IsManual = phuLieuItem.IsManual,
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
                            KLPhuGia_Manual = FormatNumber(phuLieuItem.KLPhuGia_Manual),
                            IsManual = phuLieuItem.IsManual,
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

                // ========== Lấy dữ liệu phân bổ (IsPhanBo = true) và nhóm theo HeaderKey ==========
                var phanBoData = await _context.PhuLieu_HRC2s
                    .Where(x => 
                        x.REPORT_NO == reportNo && 
                        x.IsPhanBo == true &&
                        x.ID_HeaderKey.HasValue)
                    .Select(x => new
                    {
                        x.ID_HeaderKey,
                        x.KLPhuGia,
                        x.KLPhuGia_Manual,
                        x.IsManual,
                        x.TenHienThi,
                    })
                    .ToListAsync();

                // Nhóm phân bổ theo ID_HeaderKey
                var groupedPhanBoPhuLieus = new Dictionary<int, HeaderKeyGroupedByReportNoModel>();
                foreach (var phanBoItem in phanBoData)
                {
                    if (!phanBoItem.ID_HeaderKey.HasValue) continue;
                    var headerKeyId = phanBoItem.ID_HeaderKey.Value;
                    var formattedValue = FormatNumber(phanBoItem.KLPhuGia);
                    var formattedManual = FormatNumber(phanBoItem.KLPhuGia_Manual);
                    
                    if (!groupedPhanBoPhuLieus.ContainsKey(headerKeyId))
                    {
                        groupedPhanBoPhuLieus[headerKeyId] = new HeaderKeyGroupedByReportNoModel
                        {
                            ID_HeaderKey = headerKeyId,
                            TenHienThi = phanBoItem.TenHienThi,
                            KLPhuGia = formattedValue,
                            KLPhuGia_Manual = formattedManual,
                            IsManual = phanBoItem.IsManual,
                            KLPhuGiaTotal = 0,
                        };
                    }
                    // Sum KLPhuGia
                    groupedPhanBoPhuLieus[headerKeyId].KLPhuGiaTotal = (groupedPhanBoPhuLieus[headerKeyId].KLPhuGiaTotal ?? 0) + (formattedValue ?? 0);
                }

                foreach (var item in groupedPhanBoPhuLieus.Values)
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
                    unmappedPhulieus = groupedUnmappedPhuLieus.Values.ToList(),
                    phanBoPhulieus = groupedPhanBoPhuLieus.Values.ToList()
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

                // Lấy tất cả phụ liệu từ bảng PhuLieu_HRC2 (CHỈ lấy phụ liệu thực tế, không lấy phân bổ)
                var allPhuLieuData = await _context.PhuLieu_HRC2s
                    .Where(x => 
                        x.ID_MeThoi == id && 
                        x.ID_PhuLieu.HasValue &&
                        (x.IsPhanBo != true)) // ⭐ CHỈ lấy phụ liệu thực tế, không lấy phân bổ
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

                // ========== Lấy dữ liệu phân bổ (IsPhanBo = true) và nhóm theo HeaderKey ==========
                var phanBoData = await _context.PhuLieu_HRC2s
                    .Where(x => 
                        x.ID_MeThoi == id && 
                        x.IsPhanBo == true &&
                        x.ID_HeaderKey.HasValue)
                    .Select(x => new
                    {
                        x.ID_HeaderKey,
                        x.KLPhuGia,
                        x.KLPhuGia_Manual,
                        x.IsManual,
                        x.TenHienThi,
                    })
                    .ToListAsync();

                // Nhóm phân bổ theo ID_HeaderKey
                var groupedPhanBoPhuLieus = new Dictionary<int, HeaderKeyGroupedByReportNoModel>();
                foreach (var phanBoItem in phanBoData)
                {
                    if (!phanBoItem.ID_HeaderKey.HasValue) continue;
                    var headerKeyId = phanBoItem.ID_HeaderKey.Value;
                    var formattedValue = FormatNumber(phanBoItem.KLPhuGia);
                    var formattedManual = FormatNumber(phanBoItem.KLPhuGia_Manual);
                    
                    if (!groupedPhanBoPhuLieus.ContainsKey(headerKeyId))
                    {
                        groupedPhanBoPhuLieus[headerKeyId] = new HeaderKeyGroupedByReportNoModel
                        {
                            ID_HeaderKey = headerKeyId,
                            TenHienThi = phanBoItem.TenHienThi,
                            KLPhuGia = formattedValue,
                            KLPhuGia_Manual = formattedManual,
                            IsManual = phanBoItem.IsManual,
                            KLPhuGiaTotal = 0,
                        };
                    }
                    // Sum KLPhuGia
                    groupedPhanBoPhuLieus[headerKeyId].KLPhuGiaTotal = (groupedPhanBoPhuLieus[headerKeyId].KLPhuGiaTotal ?? 0) + (formattedValue ?? 0);
                }

                foreach (var item in groupedPhanBoPhuLieus.Values)
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
                    unmappedPhulieus = groupedUnmappedPhuLieus.Values.ToList(),
                    phanBoPhulieus = groupedPhanBoPhuLieus.Values.ToList()
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
            var raw = await _context.STD_NXT_Filters
                .FromSqlRaw(
                    "EXEC sp_GetHRC2GroupedByMaterial @WorkDate, @Shift",
                    new SqlParameter("@WorkDate", ngaySX.Date),
                    new SqlParameter("@Shift", ca)
                )
                .ToListAsync();

            if (!raw.Any())
                return Enumerable.Empty<FilterSTD_NXTResponse>();

            // Mapping
            var phuLieuIds = raw.Select(x => (int)x.ID_PhuLieu).Distinct().ToList();

            var mappings = await _context.Header_Mappings
                .Where(m => phuLieuIds.Contains(m.ID_PhuLieu))
                .ToListAsync();

            var headerKeys = await _context.Header_Keys
                .Where(h => mappings.Select(m => m.ID_HeaderKey).Contains(h.Id))
                .ToListAsync();

            var mapped = raw.Select(r =>
            {
                var map = mappings.FirstOrDefault(m => m.ID_PhuLieu == (int)r.ID_PhuLieu);
                var hk = headerKeys.FirstOrDefault(h => h.Id == map?.ID_HeaderKey);

                return new
                {
                    Raw = r,
                    HeaderKeyId = map?.ID_HeaderKey,
                    HeaderKeyName = hk?.TenHienThi
                };
            });

            // 🔥 GROUP ĐÚNG THEO TỔ HỢP
            var grouped = mapped.GroupBy(x => new
            {
                x.Raw.BieuMau,
                x.Raw.Scope,
                Key = x.HeaderKeyId.HasValue
                    ? $"HK_{x.HeaderKeyId}"
                    : $"PL_{x.Raw.ID_PhuLieu}"
            });

            var result = grouped.Select(g =>
            {
                var first = g.First();

                return new FilterSTD_NXTResponse
                {
                    BieuMau = first.Raw.BieuMau,
                    Scope = (int)first.Raw.Scope,
                    HeaderKeyId = first.HeaderKeyId,
                    HeaderKeyName = first.HeaderKeyName,
                    TotalKLPhuGia = g.Sum(x => x.Raw.TotalKLPhuGia),
                    PhuLieus = g
                        .GroupBy(x => (int)x.Raw.ID_PhuLieu)
                        .Select(pl => new PhuLieuNM
                        {
                            ID_PhuLieu = pl.Key,
                            TenPhuLieu = pl.First().Raw.TenPhuLieu
                        })
                        .ToList()
                };
            }).ToList();

            return result;
        }

        public async Task<(IEnumerable<HRC2FilterThongKe> Data, int TotalCount)> SearchThongKeAsync1(SearchThongKe dto)
        {
            var query = _context.DLNM_HRC2s.AsQueryable();

            if (dto.TuNgay.HasValue && dto.DenNgay.HasValue)
            {
                query = query.Where(x =>
                    x.Ngay.HasValue &&
                    x.Ngay.Value.Date >= dto.TuNgay.Value.Date &&
                    x.Ngay.Value.Date <= dto.DenNgay.Value.Date);
            }

            if (dto.Ca.HasValue)
                query = query.Where(x => x.Ca == dto.Ca.Value);

            if (!string.IsNullOrEmpty(dto.LoaiBM))
                query = query.Where(x => x.BieuMau == dto.LoaiBM);

            if (dto.Scope.HasValue)
                query = query.Where(x => x.Scope == dto.Scope.Value);

            if (!string.IsNullOrWhiteSpace(dto.SearchText))
            {
                var search = dto.SearchText.Trim();

                if (int.TryParse(search, out var searchReportNo))
                {
                    query = query.Where(x => x.REPORT_NO == searchReportNo);
                }
                else
                {
                    query = query.Where(x =>
                        (x.MacThep ?? "").Contains(search) ||
                        (x.MeThoi ?? "").Contains(search));
                }
            }

            // đếm số report_no unique
            var totalCount = await query
                .Select(x => x.REPORT_NO)
                .Distinct()
                .CountAsync();

            // Lọc Header_Key theo LoaiBM -> LoaiThongKe
            // - BOF: LoaiThongKe = 1 hoặc 3
            // - LF/RH: LoaiThongKe = 2 hoặc 3
            // - Khác: chỉ lọc IsUsedThongKe = true
            var loaiBmKey = (dto.LoaiBM ?? string.Empty).Trim().ToUpperInvariant();
            HashSet<byte>? allowedLoaiThongKe = null;
            if (loaiBmKey.Contains("BOF"))
            {
                allowedLoaiThongKe = new HashSet<byte> { 1, 3 };
            }
            else if (loaiBmKey.Contains("LF") || loaiBmKey.Contains("RH"))
            {
                allowedLoaiThongKe = new HashSet<byte> { 2, 3 };
            }

            // Danh sách Header_Key dùng cho thống kê (render column + lọc dữ liệu)
            var usedThongKeHeaders = await _context.Header_Keys
                .Where(h =>
                    h.IsUsedThongKe == true &&
                    (allowedLoaiThongKe == null ||
                     (h.LoaiThongKe.HasValue && allowedLoaiThongKe.Contains(h.LoaiThongKe.Value))))
                .OrderBy(h => h.ThuTu ?? decimal.MaxValue)
                .ThenBy(h => h.Id)
                .Select(h => new PhuLieuHeaderTable
                {
                    IDHeaderKey = h.Id,
                    TenPhuLieu = h.TenHienThi,
                    LoaiThongKe = (byte)(h.LoaiThongKe ?? 0)
                })
                .ToListAsync();
            var usedHeaderKeyIds = usedThongKeHeaders.Select(x => x.IDHeaderKey).ToHashSet();

            var allData = await query
                .OrderBy(x => x.ID)
                .ToListAsync();

           
            var groupedQuery = allData
                .GroupBy(x => x.REPORT_NO)
                .Select(g => g.First())
                .OrderByDescending(x => x.Ngay)
                .ThenByDescending(x => x.REPORT_NO);

            List<DLNM_HRC2> groupedData;

            if (dto.Page.HasValue && dto.PageSize.HasValue && dto.Page > 0 && dto.PageSize > 0)
            {
                groupedData = groupedQuery
                    .Skip((dto.Page.Value - 1) * dto.PageSize.Value)
                    .Take(dto.PageSize.Value)
                    .ToList();
            }
            else
            {
                // Không phân trang
                groupedData = groupedQuery.ToList();
            }

            var result = new List<HRC2FilterThongKe>();
            foreach (var x in groupedData)
            {
                if (!x.REPORT_NO.HasValue || x.REPORT_NO.Value == 0)
                {
                    continue;
                }

                // Lấy chi tiết grouped (mapped/unmapped/phanBo) theo REPORT_NO
                var detail = await GetByReportNoGroupedAsync(x.REPORT_NO.Value);
                if (detail == null)
                {
                    continue;
                }

                // Chỉ giữ các HeaderKey đang bật IsUsedThongKe
                var mappedById = (detail.mappedPhulieus ?? new List<HeaderKeyGroupedByReportNoModel>())
                    .Where(p => p.ID_HeaderKey.HasValue && usedHeaderKeyIds.Contains(p.ID_HeaderKey.Value))
                    .GroupBy(p => p.ID_HeaderKey!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                // Bảo đảm đủ cột: header nào không có dữ liệu thì trả về 0
                var mappedOrdered = new List<HeaderKeyGroupedByReportNoModel>();
                foreach (var h in usedThongKeHeaders)
                {
                    if (mappedById.TryGetValue(h.IDHeaderKey, out var item))
                    {
                        mappedOrdered.Add(item);
                    }
                    else
                    {
                        mappedOrdered.Add(new HeaderKeyGroupedByReportNoModel
                        {
                            ID_HeaderKey = h.IDHeaderKey,
                            TenHienThi = h.TenPhuLieu,
                            KLPhuGia = null,
                            KLPhuGiaTotal = null
                        });
                    }
                }
                detail.mappedPhulieus = mappedOrdered;

                var phanBoById = (detail.phanBoPhulieus ?? new List<HeaderKeyGroupedByReportNoModel>())
                    .Where(p => p.ID_HeaderKey.HasValue && usedHeaderKeyIds.Contains(p.ID_HeaderKey.Value))
                    .GroupBy(p => p.ID_HeaderKey!.Value)
                    .ToDictionary(g => g.Key, g => g.First());
                var phanBoOrdered = new List<HeaderKeyGroupedByReportNoModel>();
                foreach (var h in usedThongKeHeaders)
                {
                    if (phanBoById.TryGetValue(h.IDHeaderKey, out var item))
                    {
                        phanBoOrdered.Add(item);
                    }
                    else
                    {
                        phanBoOrdered.Add(new HeaderKeyGroupedByReportNoModel
                        {
                            ID_HeaderKey = h.IDHeaderKey,
                            TenHienThi = h.TenPhuLieu,
                            KLPhuGia = null,
                            KLPhuGiaTotal = null
                        });
                    }
                }
                detail.phanBoPhulieus = phanBoOrdered;

                // Thống kê chỉ quan tâm mapped + phanBo theo header đã chọn
                detail.unmappedPhulieus = new List<HeaderKeyGroupedByReportNoModel>();

                result.Add(new HRC2FilterThongKe
                {
                    dulieu = detail,
                    phuLieuHeaderTables = usedThongKeHeaders
                });
            }

            return (result, totalCount);
        }

        public async Task<(IEnumerable<HRC2FilterThongKe> Data, int TotalCount)> SearchThongKeAsync(SearchThongKe dto)
        {
            var query = _context.DLNM_HRC2s.AsQueryable();

if (dto.TuNgay.HasValue && dto.DenNgay.HasValue)
            {
                query = query.Where(x =>
                    x.Ngay.HasValue &&
                    x.Ngay.Value.Date >= dto.TuNgay.Value.Date &&
                    x.Ngay.Value.Date <= dto.DenNgay.Value.Date);
            }

            if (dto.Ca.HasValue)
                query = query.Where(x => x.Ca == dto.Ca.Value);

            if (!string.IsNullOrEmpty(dto.LoaiBM))
                query = query.Where(x => x.BieuMau == dto.LoaiBM);

            if (dto.Scope.HasValue)
                query = query.Where(x => x.Scope == dto.Scope.Value);

            if (!string.IsNullOrWhiteSpace(dto.SearchText))
            {
                var search = dto.SearchText.Trim();

                if (int.TryParse(search, out var searchReportNo))
                {
                    query = query.Where(x => x.REPORT_NO == searchReportNo);
                }
                else
                {
                    query = query.Where(x =>
                        (x.MacThep ?? "").Contains(search) ||
                        (x.MeThoi ?? "").Contains(search));
                }
            }

            // đếm report_no unique
            var totalCount = await query
                .Where(x => x.REPORT_NO.HasValue)
                .Select(x => x.REPORT_NO)
                .Distinct()
                .CountAsync();

            // lọc loại thống kê theo LoaiBM
            var loaiBmKey = (dto.LoaiBM ?? "").Trim().ToUpperInvariant();
            HashSet<byte>? allowedLoaiThongKe = null;

            if (loaiBmKey.Contains("BOF"))
                allowedLoaiThongKe = new HashSet<byte> { 1, 3 };
            else if (loaiBmKey.Contains("LF") || loaiBmKey.Contains("RH"))
                allowedLoaiThongKe = new HashSet<byte> { 2, 3 };

            // danh sách header phụ liệu
            var usedThongKeHeaders = await _context.Header_Keys
                .Where(h =>
                    h.IsUsedThongKe == true &&
                    (allowedLoaiThongKe == null ||
                     (h.LoaiThongKe.HasValue && allowedLoaiThongKe.Contains(h.LoaiThongKe.Value))))
                .OrderBy(h => h.ThuTu ?? decimal.MaxValue)
                .ThenBy(h => h.Id)
                .Select(h => new PhuLieuHeaderTable
                {
                    IDHeaderKey = h.Id,
                    TenPhuLieu = h.TenHienThi,
                    LoaiThongKe = (byte)(h.LoaiThongKe ?? 0)
                })
                .ToListAsync();

            var usedHeaderKeyIds = usedThongKeHeaders.Select(x => x.IDHeaderKey).ToHashSet();

            // lấy dữ liệu group theo REPORT_NO trực tiếp SQL
            var groupedIds = query
             .Where(x => x.REPORT_NO.HasValue)
             .GroupBy(x => x.REPORT_NO)
             .Select(g => g.Max(x => x.ID));

            IQueryable<DLNM_HRC2> groupedQuery = _context.DLNM_HRC2s
            .Where(x => groupedIds.Contains(x.ID))
            .OrderByDescending(x => x.Ngay)
            .ThenByDescending(x => x.REPORT_NO);

            if (dto.Page.HasValue && dto.PageSize.HasValue)
            {
                groupedQuery = groupedQuery
                    .Skip((dto.Page.Value - 1) * dto.PageSize.Value)
                    .Take(dto.PageSize.Value);
            }

            var groupedData = await groupedQuery
                .AsNoTracking()
                .ToListAsync();

            var result = new List<HRC2FilterThongKe>();

            foreach (var x in groupedData)
            {
                if (!x.REPORT_NO.HasValue || x.REPORT_NO.Value == 0)
                    continue;

                var detail = await GetByReportNoGroupedAsync(x.REPORT_NO.Value);

                if (detail == null)
                    continue;

                // mapped
                var mappedById = (detail.mappedPhulieus ?? new List<HeaderKeyGroupedByReportNoModel>())
                    .Where(p => p.ID_HeaderKey.HasValue && usedHeaderKeyIds.Contains(p.ID_HeaderKey.Value))
                    .GroupBy(p => p.ID_HeaderKey!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                var mappedOrdered = new List<HeaderKeyGroupedByReportNoModel>();

                foreach (var h in usedThongKeHeaders)
                {
                    if (mappedById.TryGetValue(h.IDHeaderKey, out var item))
                    {
                        mappedOrdered.Add(item);
                    }
                    else
                    {
                        mappedOrdered.Add(new HeaderKeyGroupedByReportNoModel
                        {
                            ID_HeaderKey = h.IDHeaderKey,
                            TenHienThi = h.TenPhuLieu,
                            KLPhuGia = null,
                            KLPhuGiaTotal = null
                        });
                    }
                }

                detail.mappedPhulieus = mappedOrdered;

                // phân bổ
                var phanBoById = (detail.phanBoPhulieus ?? new List<HeaderKeyGroupedByReportNoModel>())
                    .Where(p => p.ID_HeaderKey.HasValue && usedHeaderKeyIds.Contains(p.ID_HeaderKey.Value))
                    .GroupBy(p => p.ID_HeaderKey!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                var phanBoOrdered = new List<HeaderKeyGroupedByReportNoModel>();

                foreach (var h in usedThongKeHeaders)
                {
                    if (phanBoById.TryGetValue(h.IDHeaderKey, out var item))
                    {
                        phanBoOrdered.Add(item);
                    }
                    else
                    {
                        phanBoOrdered.Add(new HeaderKeyGroupedByReportNoModel
                        {
                            ID_HeaderKey = h.IDHeaderKey,
                            TenHienThi = h.TenPhuLieu,
                            KLPhuGia = null,
                            KLPhuGiaTotal = null
                        });
                    }
                }

                detail.phanBoPhulieus = phanBoOrdered;
                detail.unmappedPhulieus = new List<HeaderKeyGroupedByReportNoModel>();

                result.Add(new HRC2FilterThongKe
                {
                    dulieu = detail,
                    phuLieuHeaderTables = usedThongKeHeaders
                });
            }

            return (result, totalCount);


        }

    }
}
