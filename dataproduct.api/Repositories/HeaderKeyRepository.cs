using System;
using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class HeaderKeyRepository : IHeaderKeyRepository
    {
        private readonly ProductFormContext _context;

        public HeaderKeyRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Header_Key>> GetAllAsync()
        {
            var query = _context.Header_Keys.AsQueryable();

            return await query.ToListAsync();
        }

        public async Task<Header_Key?> GetByIdAsync(int id)
        {
            return await _context.Header_Keys.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(Header_Key entity)
        {
            if (entity.KeyGuid == Guid.Empty)
            {
                entity.KeyGuid = Guid.NewGuid();
            }
            entity.NgayTao ??= DateTime.Now;
            _context.Header_Keys.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Header_Key entity)
        {
            _context.Header_Keys.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.Header_Keys.FindAsync(id);
            if (item != null)
            {
                _context.Header_Keys.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(IEnumerable<HeaderKey_ResponseModel> Data, int TotalCount)> SearchWithPagingAsync(string? searchKey, string? LoaiPhieu, int page, int pageSize)
        {
            var query = _context.Header_Keys.AsQueryable();

            if (!string.IsNullOrEmpty(searchKey))
            {
                query = query.Where(x => x.TenHienThi.Contains(searchKey));
            }

            if (!string.IsNullOrEmpty(LoaiPhieu))
            {
                query = query.Where(x => x.LoaiPhieu == LoaiPhieu);
            }

            var totalCount = await query.CountAsync();

            var headerKeys = await query
                .OrderBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var headerIds = headerKeys.Select(x => x.Id).ToList();

            var mappings = await _context.Header_Mappings
                .Where(m => headerIds.Contains(m.ID_HeaderKey))
                .GroupBy(m => m.ID_HeaderKey)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(m => new HeaderMapping_ResponseModel
                    {
                        Id = m.Id,
                        ID_PhuLieu = m.ID_PhuLieu,
                        TenNguonDuLieu = m.TenNguonDuLieu
                    }).ToList());

            var result = headerKeys.Select(h => new HeaderKey_ResponseModel
            {
                Id = h.Id,
                KeyGuid = h.KeyGuid,
                TenHienThi = h.TenHienThi,
                Mota = h.Mota,
                LoaiPhieu = h.LoaiPhieu,
                IsActive = h.IsActive,
                NgayTao = h.NgayTao,
                IsUsedNXT = h.IsUsedNXT,
                IsUsedThongKe = h.IsUsedThongKe,
                ThuTu = null,
                TyTrong = h.TyTrong.HasValue ? (decimal?)h.TyTrong.Value : null,
                ThuTu_TK_BOF = h.ThuTu_TK_BOF,
                ThuTu_TK_LFRH = h.ThuTu_TK_LFRH,
                IsUsed_Excel = h.IsUsed_Excel,
                LoaiExcel = h.LoaiExcel,
                ThuTu_Excel_BOF = h.ThuTu_Excel_BOF,
                ThuTu_Excel_LFRH = h.ThuTu_Excel_LFRH,
                MaVatTuChiPhi = h.MaVatTuChiPhi,
                HeaderMappings = mappings.TryGetValue(h.Id, out var list)
                    ? list
                    : new List<HeaderMapping_ResponseModel>()
            }).ToList();

            return (result, totalCount);
        }

        public async Task<bool> ExistsByTenHienThiAsync(string tenHienThi, int? excludeId = null)
        {
            var query = _context.Header_Keys.AsQueryable()
                .Where(x => x.TenHienThi == tenHienThi);
            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<bool> IsInUseAsync(int id)
        {
            return await _context.Header_Mappings.AnyAsync(x => x.ID_HeaderKey == id);
        }

        public async Task<Header_Key?> FindDuplicateThuTuAsync(ThuTuColumn column, int value, int? excludeId = null)
        {
            IQueryable<Header_Key> query = column switch
            {
                ThuTuColumn.TK_BOF     => _context.Header_Keys.Where(x => x.ThuTu_TK_BOF == value),
                ThuTuColumn.TK_LFRH    => _context.Header_Keys.Where(x => x.ThuTu_TK_LFRH == value),
                ThuTuColumn.Excel_BOF  => _context.Header_Keys.Where(x => x.ThuTu_Excel_BOF == value),
                ThuTuColumn.Excel_LFRH => _context.Header_Keys.Where(x => x.ThuTu_Excel_LFRH == value),
                _ => throw new ArgumentOutOfRangeException(nameof(column))
            };

            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);

            return await query.FirstOrDefaultAsync();
        }

        // Search theo 3 bảng Header_Key, Header_Mapping, PhuLieu_NM:
        // - Trả về 1 dòng / record
        // - Có móc nối: trả về dòng mapping
        // - Có HeaderKey nhưng chưa móc nối: vẫn trả về
        // - Có PhuLieu_NM nhưng chưa móc nối: vẫn trả về
        public async Task<(IEnumerable<HeaderKeyMapping_ResponseModel> Data, int TotalCount)> SearchMappingsWithPagingAsync(
            string? searchKey,
            string? LoaiPhieu,
            string? TrangThai,
            bool? IsUsedNXT,
            bool? IsUsedThongKe,
            DateTime? FromDate,
            DateTime? ToDate,
            int? IdNhom,
            bool? chuaMappingNM,
            string? SortBy,
            string? SortOrder,
            int page,
            int pageSize
        )
        {
            // (1) Các dòng có móc nối (Header_Mapping)
            var mappedQuery =
                from m in _context.Header_Mappings
                join hk in _context.Header_Keys on m.ID_HeaderKey equals hk.Id into hkGroup
                from hk in hkGroup.DefaultIfEmpty()
                join pl in _context.PhuLieu_NMs on m.ID_PhuLieu equals pl.ID_PhuLieu into plGroup
                from pl in plGroup.DefaultIfEmpty()
                select new HeaderKeyMapping_ResponseModel
                {
                    MappingId = m.Id,
                    ID_HeaderKey = hk != null ? hk.Id : (int?)null,
                    KeyGuid = hk != null ? hk.KeyGuid : (Guid?)null,
                    TenHienThi = hk != null ? hk.TenHienThi : null,
                    Mota = hk != null ? hk.Mota : null,
                    LoaiPhieu = hk != null ? hk.LoaiPhieu : null,
                    IsActive = hk != null ? hk.IsActive : (bool?)null,
                    NgayTao = hk != null ? hk.NgayTao : null,
                    NgayTaoPhuLieu = pl != null ? pl.NgayTao : null,
                    IsUsedNXT = hk != null ? hk.IsUsedNXT : null,
                    IsUsedThongKe = hk != null ? hk.IsUsedThongKe : null,
                    ThuTu = null,
                    TyTrong = hk != null && hk.TyTrong.HasValue ? (decimal?)hk.TyTrong.Value : null,
                    ID_PhuLieu = m.ID_PhuLieu,
                    TenNguonDuLieu = m.TenNguonDuLieu,
                    TenPhuLieu = pl != null ? pl.TenPhuLieu : null,
                    LoaiThongKe = hk != null ? hk.LoaiThongKe : null,
                    ThuTu_TK_BOF = hk != null ? hk.ThuTu_TK_BOF : null,
                    ThuTu_TK_LFRH = hk != null ? hk.ThuTu_TK_LFRH : null,
                    IsUsed_Excel = hk != null ? hk.IsUsed_Excel : null,
                    LoaiExcel = hk != null ? hk.LoaiExcel : null,
                    ThuTu_Excel_BOF = hk != null ? hk.ThuTu_Excel_BOF : null,
                    ThuTu_Excel_LFRH = hk != null ? hk.ThuTu_Excel_LFRH : null,
                    ID_NhomKey = hk != null ? hk.ID_NhomKey : null,
                    MaVatTuChiPhi = hk != null ? hk.MaVatTuChiPhi : null
                };

            // (2) HeaderKey chưa được móc nối
            var unmappedHeaderKeysQuery =
                from hk in _context.Header_Keys
                where !_context.Header_Mappings.Any(m => m.ID_HeaderKey == hk.Id)
                select new HeaderKeyMapping_ResponseModel
                {
                    MappingId = null,
                    ID_HeaderKey = hk.Id,
                    KeyGuid = hk.KeyGuid,
                    TenHienThi = hk.TenHienThi,
                    Mota = hk.Mota,
                    LoaiPhieu = hk.LoaiPhieu,
                    IsActive = hk.IsActive,
                    NgayTao = hk.NgayTao,
                    NgayTaoPhuLieu = null,
                    IsUsedNXT = hk.IsUsedNXT,
                    IsUsedThongKe = hk != null ? hk.IsUsedThongKe : null,
                    ThuTu = null,
                    TyTrong = hk.TyTrong.HasValue ? (decimal?)hk.TyTrong.Value : null,
                    ID_PhuLieu = null,
                    TenNguonDuLieu = null,
                    TenPhuLieu = null,
                    LoaiThongKe = hk != null ? hk.LoaiThongKe : null,
                    ThuTu_TK_BOF = hk.ThuTu_TK_BOF,
                    ThuTu_TK_LFRH = hk.ThuTu_TK_LFRH,
                    IsUsed_Excel = hk.IsUsed_Excel,
                    LoaiExcel = hk.LoaiExcel,
                    ThuTu_Excel_BOF = hk.ThuTu_Excel_BOF,
                    ThuTu_Excel_LFRH = hk.ThuTu_Excel_LFRH,
                    ID_NhomKey = hk.ID_NhomKey,
                    MaVatTuChiPhi = hk.MaVatTuChiPhi
                };

            // (3) PhuLieu_NM chưa được móc nối
            var unmappedPhuLieuQuery =
                from pl in _context.PhuLieu_NMs
                where !_context.Header_Mappings.Any(m => m.ID_PhuLieu == pl.ID_PhuLieu)
                select new HeaderKeyMapping_ResponseModel
                {
                    MappingId = null,
                    ID_HeaderKey = null,
                    KeyGuid = null,
                    TenHienThi = null,
                    Mota = null,
                    LoaiPhieu = null,
                    IsActive = null,
                    NgayTao = null,
                    NgayTaoPhuLieu = pl.NgayTao,
                    IsUsedNXT = null,
                    IsUsedThongKe = null,
                    ThuTu = null,
                    TyTrong = null,
                    ID_PhuLieu = pl.ID_PhuLieu,
                    TenNguonDuLieu = null,
                    TenPhuLieu = pl.TenPhuLieu,
                    LoaiThongKe = null,
                    ThuTu_TK_BOF = null,
                    ThuTu_TK_LFRH = null,
                    IsUsed_Excel = null,
                    LoaiExcel = null,
                    ThuTu_Excel_BOF = null,
                    ThuTu_Excel_LFRH = null,
                    ID_NhomKey = null,
                    MaVatTuChiPhi = null
                };

            // Union 3 tập dữ liệu thành 1 query
            var query = mappedQuery
                .Concat(unmappedHeaderKeysQuery)
                .Concat(unmappedPhuLieuQuery);

            // Filter theo searchKey (tìm trong TenPhuLieu/TenHienThi/TenNguonDuLieu)
            if (!string.IsNullOrEmpty(searchKey))
            {
                query = query.Where(x =>
                    (x.TenPhuLieu != null && x.TenPhuLieu.Contains(searchKey)) ||
                    (x.TenHienThi != null && x.TenHienThi.Contains(searchKey)) ||
                    (x.TenNguonDuLieu != null && x.TenNguonDuLieu.Contains(searchKey))
                );
            }

            // Filter theo LoaiPhieu
            if (!string.IsNullOrEmpty(LoaiPhieu))
            {
                // nếu chưa móc nối (LoaiPhieu null) thì vẫn cho hiện ra
                query = query.Where(x => x.LoaiPhieu == null || x.LoaiPhieu == LoaiPhieu);
            }

            // Filter theo Trạng thái: DangDung | ChuaMocNoi | Ngung
            if (!string.IsNullOrEmpty(TrangThai))
            {
                var st = TrangThai.Trim().ToLowerInvariant();

                // Ngưng: ưu tiên theo isActive == false
                if (st == "ngung" || st == "inactive" || st == "0")
                {
                    query = query.Where(x => x.IsActive == false);
                }
                else if (st == "dangdung" || st == "active" || st == "1")
                {
                    // Đang dùng: isActive == true AND đã móc nối (có cả phụ liệu & header key)
                    query = query.Where(x =>
                        x.IsActive == true &&
                        x.ID_PhuLieu != null &&
                        x.ID_HeaderKey != null
                    );
                }
                else if (st == "chuamocnoi" || st == "unmapped")
                {
                    // Chưa móc nối: isActive != false AND thiếu 1 trong 2 (phụ liệu/header key)
                    query = query.Where(x =>
                        x.IsActive != false &&
                        (
                            (x.ID_PhuLieu != null && x.ID_HeaderKey == null) ||
                            (x.ID_PhuLieu == null && x.ID_HeaderKey != null)
                        )
                    );
                }
            }

            // Filter theo IsUsedNXT: true = Có dùng, false = Không dùng (false hoặc null)
            if (IsUsedNXT.HasValue)
            {
                if (IsUsedNXT.Value)
                {
                    query = query.Where(x => x.IsUsedNXT == true);
                }
                else
                {
                    query = query.Where(x => x.IsUsedNXT != true);
                }
            }
            
            if (IsUsedThongKe.HasValue)
            {
                if (IsUsedThongKe.Value)
                {
                    query = query.Where(x => x.IsUsedThongKe == true);
                }
                else
                {
                    query = query.Where(x => x.IsUsedThongKe != true);
                }
            }
            // Filter theo khoảng ngày (áp dụng trên "Ngày tạo hiệu lực": ưu tiên NgayTaoPhuLieu, fallback NgayTao)
            if (FromDate.HasValue)
            {
                var from = FromDate.Value.Date;
                query = query.Where(x => (x.NgayTaoPhuLieu ?? x.NgayTao) != null && (x.NgayTaoPhuLieu ?? x.NgayTao) >= from);
            }
            if (ToDate.HasValue)
            {
                // inclusive end-of-day
                var to = ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => (x.NgayTaoPhuLieu ?? x.NgayTao) != null && (x.NgayTaoPhuLieu ?? x.NgayTao) <= to);
            }

            // Filter theo nhóm
            if (IdNhom.HasValue)
                query = query.Where(x => x.ID_NhomKey == IdNhom.Value);

            // Filter phụ liệu NM chưa được mapping (có ID_PhuLieu nhưng không có ID_HeaderKey)
            if (chuaMappingNM == true)
                query = query.Where(x => x.ID_PhuLieu != null && x.ID_HeaderKey == null);

            // Đếm tổng số records
            var totalCount = await query.CountAsync();

            // Sort theo 1 trong 4 cột thứ tự (nếu FE yêu cầu) — sort + phân trang trên TOÀN BỘ
            // dữ liệu đã lọc (không phải chỉ trang đang xem). Dòng không có giá trị ở cột đó
            // luôn xếp cuối, bất kể asc/desc.
            var descending = string.Equals(SortOrder, "descend", StringComparison.OrdinalIgnoreCase);
            IOrderedQueryable<HeaderKeyMapping_ResponseModel> ordered = SortBy?.Trim().ToLowerInvariant() switch
            {
                "thutu_tk_bof" => descending
                    ? query.OrderBy(x => x.ThuTu_TK_BOF == null ? 1 : 0).ThenByDescending(x => x.ThuTu_TK_BOF)
                    : query.OrderBy(x => x.ThuTu_TK_BOF == null ? 1 : 0).ThenBy(x => x.ThuTu_TK_BOF),
                "thutu_tk_lfrh" => descending
                    ? query.OrderBy(x => x.ThuTu_TK_LFRH == null ? 1 : 0).ThenByDescending(x => x.ThuTu_TK_LFRH)
                    : query.OrderBy(x => x.ThuTu_TK_LFRH == null ? 1 : 0).ThenBy(x => x.ThuTu_TK_LFRH),
                "thutu_excel_bof" => descending
                    ? query.OrderBy(x => x.ThuTu_Excel_BOF == null ? 1 : 0).ThenByDescending(x => x.ThuTu_Excel_BOF)
                    : query.OrderBy(x => x.ThuTu_Excel_BOF == null ? 1 : 0).ThenBy(x => x.ThuTu_Excel_BOF),
                "thutu_excel_lfrh" => descending
                    ? query.OrderBy(x => x.ThuTu_Excel_LFRH == null ? 1 : 0).ThenByDescending(x => x.ThuTu_Excel_LFRH)
                    : query.OrderBy(x => x.ThuTu_Excel_LFRH == null ? 1 : 0).ThenBy(x => x.ThuTu_Excel_LFRH),
                // Mặc định: phụ liệu/header key mới trước (NgayTao DESC), giữ nguyên hành vi cũ
                _ => query.OrderByDescending(x => x.NgayTao)
            };

            var data = await ordered
                .ThenBy(x => x.MappingId == null ? 1 : 0)
                .ThenBy(x => x.ID_PhuLieu)
                .ThenBy(x => x.ID_HeaderKey)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Enrich TenNhom từ Header_Nhom
            var nhomIds = data.Where(d => d.ID_NhomKey.HasValue)
                              .Select(d => d.ID_NhomKey!.Value).ToHashSet();
            if (nhomIds.Count > 0)
            {
                var nhomNames = await _context.Header_Nhoms
                    .Where(n => nhomIds.Contains(n.Id))
                    .ToDictionaryAsync(n => n.Id, n => n.TenHienThi);
                foreach (var d in data)
                    if (d.ID_NhomKey.HasValue && nhomNames.TryGetValue(d.ID_NhomKey.Value, out var name))
                        d.TenNhom = name;
            }

            return (data, totalCount);
        }
    }
}
