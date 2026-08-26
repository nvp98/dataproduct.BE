using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories.NMTKVV
{
    public class TKVV_BCSL_ChiPhiRepository : ITKVV_BCSL_ChiPhiRepository
    {
        private readonly ProductFormContext _context;

        public TKVV_BCSL_ChiPhiRepository(ProductFormContext context)
        {
            _context = context;
        }

        private static readonly Dictionary<int, string> ScopeCodeMap = new()
        {
            { 1, "TK1" }, { 2, "TK2" }, { 3, "TK3" }, { 4, "TK4" },
            { 5, "VV1" }, { 6, "VV2" },
        };

        public static string ResolveScopeCode(int scope)
            => ScopeCodeMap.TryGetValue(scope, out var code) ? code : scope.ToString();

        // SP join qua linked server [SQL_OT].EMS_DATA_CAN nên dùng raw ADO.NET
        // thay vì EF FromSqlRaw (không composable qua linked server).
        public async Task<List<TKVVGiaTriNVLAutoDto>> GetGiaTriNVLAutoAsync(
            DateTime ngay, int ca, string scopeCode, string maBM)
        {
            var result = new List<TKVVGiaTriNVLAutoDto>();

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "dbo.SP_TKVV_GetGiaTriNVL_Auto";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;
                cmd.Parameters.Add(new SqlParameter("@Ngay", ngay.Date));
                cmd.Parameters.Add(new SqlParameter("@Ca", ca));
                cmd.Parameters.Add(new SqlParameter("@Scope", scopeCode));
                cmd.Parameters.Add(new SqlParameter("@MaBM", maBM));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new TKVVGiaTriNVLAutoDto
                    {
                        NguyenVatLieuID = reader["NguyenVatLieuID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NguyenVatLieuID"]),
                        MaBM = reader["MaBM"]?.ToString() ?? string.Empty,
                        TenNVL = reader["TenNVL"]?.ToString() ?? string.Empty,
                        DonViTinh = reader["DonViTinh"] == DBNull.Value ? null : reader["DonViTinh"].ToString(),
                        ThuTu = reader["ThuTu"] == DBNull.Value ? null : Convert.ToInt32(reader["ThuTu"]),
                        Scope = reader["Scope"] == DBNull.Value ? null : reader["Scope"].ToString(),
                        TenScope = reader["TenScope"] == DBNull.Value ? null : reader["TenScope"].ToString(),
                        GiaTri = reader["GiaTri"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["GiaTri"]),
                        SoLuongTag = reader["SoLuongTag"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SoLuongTag"]),
                        ThoiGianTu = reader["ThoiGianTu"] == DBNull.Value ? null : Convert.ToDateTime(reader["ThoiGianTu"]),
                        ThoiGianDen = reader["ThoiGianDen"] == DBNull.Value ? null : Convert.ToDateTime(reader["ThoiGianDen"]),
                    });
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            return result;
        }

        public async Task<List<TKVVDuLieuCanDto>> GetDuLieuCanAsync(
            DateTime ngay, int ca, string maBM, string loaiDuLieu, int scope)
        {
            var result = new List<TKVVDuLieuCanDto>();

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "dbo.SP_TKVV_GetDuLieuCan";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;
                cmd.Parameters.Add(new SqlParameter("@Ngay", ngay.Date));
                cmd.Parameters.Add(new SqlParameter("@Ca", ca));
                cmd.Parameters.Add(new SqlParameter("@MaBM", maBM));
                cmd.Parameters.Add(new SqlParameter("@LoaiDuLieu", loaiDuLieu));
                cmd.Parameters.Add(new SqlParameter("@Scope", scope));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new TKVVDuLieuCanDto
                    {
                        Ngay = ngay.Date,
                        Ca = ca,
                        MaBM = maBM,
                        Scope = reader["Scope"] == DBNull.Value ? null : reader["Scope"].ToString(),
                        Xuong = reader["Xuong"] == DBNull.Value ? null : reader["Xuong"].ToString(),
                        SiloID = reader["SiloID"] == DBNull.Value ? null : Convert.ToInt32(reader["SiloID"]),
                        MaSilo = reader["MaSilo"] == DBNull.Value ? null : reader["MaSilo"].ToString(),
                        NguyenVatLieuID = reader["NguyenVatLieuID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NguyenVatLieuID"]),
                        TenNVL = reader["TenNVL"]?.ToString() ?? string.Empty,
                        DonViTinh = reader["DonViTinh"] == DBNull.Value ? null : reader["DonViTinh"].ToString(),
                        GiaTri = reader["GiaTri"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["GiaTri"]),
                        GiaTriXuat = reader["GiaTriXuat"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["GiaTriXuat"]),
                        SoLuongSilo = reader["SoLuongSilo"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SoLuongSilo"]),
                    });
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            return result;
        }

        public async Task<List<TKVVDuLieuSanLuongTongBBGNDto>> GetDuLieuDuLieuSanLuongTongBBGNAsync(
            DateTime ngay, int ca, int scope)
        {
            var result = new List<TKVVDuLieuSanLuongTongBBGNDto>();

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "dbo.sp_TKVV_GetSanLuongTong_BBGN";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;
                cmd.Parameters.Add(new SqlParameter("@Ngay", ngay.Date));
                cmd.Parameters.Add(new SqlParameter("@Ca", ca));
                cmd.Parameters.Add(new SqlParameter("@Scope", scope));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new TKVVDuLieuSanLuongTongBBGNDto
                    {
                        Ngay = ngay.Date,
                        Ca = ca,
                        Scope = reader["Scope"] == DBNull.Value ? null : reader["Scope"].ToString(),
                        Xuong = reader["MaXuong"] == DBNull.Value ? null : reader["MaXuong"].ToString(),
                        GiaTri = reader["KhoiLuong_BG"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["KhoiLuong_BG"]),
                        TenXuong_BN = reader["TenXuong_BN"] == DBNull.Value ? null : reader["TenXuong_BN"].ToString(),
                        MaPB_BN = reader["MaPB_BN"] == DBNull.Value ? null : reader["MaPB_BN"].ToString(),
                        MaLo = reader["MaLo"] == DBNull.Value ? null : reader["MaLo"].ToString(),
                        BBGN_GhiChu = reader["BBGN_GhiChu"] == DBNull.Value ? null : reader["BBGN_GhiChu"].ToString(),
                    });
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            return result;
        }

        // ─── TKVV_BaoCaoSanLuongChiPhi ────────────────────────────────────────────

        public async Task<LoadDuLieuCanResultDto> LoadAndSaveAsync(LoadDuLieuCanRequestDto request)
        {
            var ngay = new DateTime(request.NgaySX.Year, request.NgaySX.Month, request.NgaySX.Day);

            // Chạy tuần tự — cả 2 dùng chung 1 DbConnection, không thể song song
            var spCa1 = await GetDuLieuCanAsync(ngay, 1, request.MaBM, request.LoaiDuLieu, request.Scope);
            var spCa2 = await GetDuLieuCanAsync(ngay, 2, request.MaBM, request.LoaiDuLieu, request.Scope);
            var spTongBBGN1 = await GetDuLieuDuLieuSanLuongTongBBGNAsync(ngay, 1, request.Scope);
            var spTongBBGN2 = await GetDuLieuDuLieuSanLuongTongBBGNAsync(ngay, 2, request.Scope);

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Load existing records for this NgaySX + Scope (int)
                var existing = await _context.TKVV_BaoCaoSanLuongChiPhi
                    .Where(x => x.NgaySX == request.NgaySX && x.Scope == request.Scope && !x.IsDelete)
                    .ToListAsync();

                void UpsertItem(TKVVDuLieuCanDto item, int ca, int thuTu)
                {
                    var klAmAuto = item.GiaTri;
                    // Khóa nghiệp vụ: NgaySX + Ca + Scope + NguyenVatLieuID (không có SiloID)
                    var rec = existing.FirstOrDefault(x =>
                        x.Ca == ca &&
                        x.NguyenVatLieuID == item.NguyenVatLieuID);

                    if (rec == null)
                    {
                        _context.TKVV_BaoCaoSanLuongChiPhi.Add(new TKVV_BaoCaoSanLuongChiPhi
                        {
                            NgaySX = request.NgaySX,
                            Ca = (byte)ca,
                            Scope = request.Scope,
                            NguyenVatLieuID = item.NguyenVatLieuID,
                            Kip = item.MaSilo,
                            ThuTu = thuTu,
                            KLAmAuto = klAmAuto,
                            KLAm = klAmAuto,   // Rule INSERT: KLAm = KLAmAuto, IsAdjusted = 0
                            IsAdjusted = false,
                            CreatedDate = DateTime.Now,
                            CreatedBy = request.CreatedBy
                        });
                    }
                    else
                    {
                        rec.KLAmAuto = klAmAuto;
                        rec.UpdatedDate = DateTime.Now;
                        // Rule 3: chưa điều chỉnh → cập nhật cả KLAm
                        // Rule 4: đã điều chỉnh → chỉ cập nhật KLAmAuto, giữ nguyên KLAm
                        if (!rec.IsAdjusted)
                            rec.KLAm = klAmAuto;
                        rec.Kip = item.MaSilo;
                    }
                }
                void UpsertThanhPham(
                    TKVVDuLieuSanLuongTongBBGNDto item,
                    int ca,
                    int thuTu)
                {
                    // Lấy toàn bộ record của Ca + Scope,
                    // sau đó sắp xếp theo thứ tự đã lưu
                    var records = existing
                        .Where(x =>
                            x.Ca == ca &&
                            x.Scope == request.Scope)
                        //.OrderBy(x => x.ThuTu)
                        .ToList();

                    TKVV_BaoCaoSanLuongChiPhi rec = null;

                    // ---------------------------------------------------------
                    // Nếu đã có record tương ứng theo thứ tự
                    // ---------------------------------------------------------
                    if (thuTu <= records.Count)
                    {
                        rec = records[thuTu - 1];
                    }

                    if (rec == null)
                    {
                        _context.TKVV_BaoCaoSanLuongChiPhi.Add(
                            new TKVV_BaoCaoSanLuongChiPhi
                            {
                                NgaySX = request.NgaySX,
                                Ca = (byte)ca,
                                Scope = request.Scope,

                                // Không phải silo/NVL
                                Kip = null,

                                ThuTu = thuTu,

                                // Sản lượng BBGN
                                ThanhPhamL1 = item.GiaTri,
                                ThanhPham_Note = item.MaPB_BN + " - " + item.TenXuong_BN,

                                CreatedDate = DateTime.Now,
                                CreatedBy = request.CreatedBy,

                                IsAdjusted = false
                            });
                    }
                    else
                    {
                        // Cập nhật sản lượng thành phẩm
                        rec.ThanhPhamL1 = item.GiaTri;

                        rec.ThanhPham_Note = item.MaPB_BN + " - " + item.TenXuong_BN;
                        rec.UpdatedDate = DateTime.Now;
                    }
                }


                for (int i = 0; i < spCa1.Count; i++) UpsertItem(spCa1[i], 1, i + 1);
                for (int i = 0; i < spCa2.Count; i++) UpsertItem(spCa2[i], 2, i + 1);
                for (int i = 0; i < spTongBBGN1.Count; i++)
                {
                    UpsertThanhPham(
                        spTongBBGN1[i],
                        1,
                        i + 1
                    );
                }

                for (int i = 0; i < spTongBBGN2.Count; i++)
                {
                    UpsertThanhPham(
                        spTongBBGN2[i],
                        2,
                        i + 1
                    );
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return await GetBaoCaoDataAsync(request.NgaySX, request.MaBM, request.Scope);
        }

        public async Task<LoadDuLieuCanResultDto> GetBaoCaoDataAsync(DateOnly ngaySX, string maBM, int scope)
        {
            var rows = await (from r in _context.TKVV_BaoCaoSanLuongChiPhi
                              join nvl in _context.TKVV_NguyenVatLieu on r.NguyenVatLieuID equals nvl.ID into nvlG
                              from nvl in nvlG.DefaultIfEmpty()
                              where r.NgaySX == ngaySX && r.Scope == scope && !r.IsDelete
                              orderby r.Ca, r.ThuTu, r.NguyenVatLieuID
                              select new TKVVBaoCaoSanLuongChiPhiDto
                              {
                                  Id = r.ID,
                                  PhieuID = r.PhieuID,
                                  NgaySX = r.NgaySX,
                                  Ca = r.Ca,
                                  Kip = r.Kip,
                                  Scope = r.Scope,
                                  ThuTu = r.ThuTu,
                                  NguyenVatLieuID = r.NguyenVatLieuID,
                                  TenNVL = nvl != null ? nvl.TenNVL : null,
                                  KLAm = r.KLAm,
                                  KLAmAuto = r.KLAmAuto,
                                  DoAm = r.DoAm,
                                  QuyKho = r.QuyKho,
                                  ThanhPhamL1 = r.ThanhPhamL1,
                                  ThanhPhamL2 = r.ThanhPhamL2,
                                  ThanhPhamL3 = r.ThanhPhamL3,
                                  ThanhPham_Note = r.ThanhPham_Note,
                                  GhiChu = r.GhiChu,
                                  IsAdjusted = r.IsAdjusted,
                                  AdjustedBy = r.AdjustedBy,
                                  AdjustedDate = r.AdjustedDate,
                              }).AsNoTracking().ToListAsync();

            return new LoadDuLieuCanResultDto
            {
                Table1 = rows.Where(x => x.Ca == 1).ToList(),
                Table2 = rows.Where(x => x.Ca == 2).ToList(),
            };
        }

        public async Task SavePhieuRowsAsync(SaveBcSlPhieuRequestDto request)
        {
            if (request.Rows.Count == 0) return;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in request.Rows)
                {
                    TKVV_BaoCaoSanLuongChiPhi? rec = null;

                    if (row.Id.HasValue && row.Id > 0)
                        rec = await _context.TKVV_BaoCaoSanLuongChiPhi.FindAsync(row.Id.Value);

                    // Fallback: tìm theo khóa nghiệp vụ NgaySX + Ca + Scope + NguyenVatLieuID
                    rec ??= await _context.TKVV_BaoCaoSanLuongChiPhi.FirstOrDefaultAsync(x =>
                        x.NgaySX == row.NgaySX &&
                        x.Ca == row.Ca &&
                        x.Scope == row.Scope &&
                        x.NguyenVatLieuID == row.NguyenVatLieuID &&
                        !x.IsDelete);

                    if (rec == null) continue;

                    rec.KLAm = row.KLAm;
                    rec.DoAm = row.DoAm;
                    rec.QuyKho = row.QuyKho;
                    rec.ThanhPhamL1 = row.ThanhPhamL1;
                    rec.ThanhPhamL2 = row.ThanhPhamL2;
                    rec.ThanhPhamL3 = row.ThanhPhamL3;
                    rec.GhiChu = row.GhiChu;
                    rec.Kip = row.Kip;
                    rec.PhieuID = request.PhieuID;
                    rec.UpdatedDate = DateTime.Now;

                    // Rule 5-9: backend tự so sánh KLAm vs KLAmAuto để xác định IsAdjusted
                    if (rec.KLAm == rec.KLAmAuto)
                    {
                        rec.IsAdjusted = false;
                        rec.AdjustedBy = null;
                        rec.AdjustedDate = null;
                    }
                    else
                    {
                        rec.IsAdjusted = true;
                        rec.AdjustedBy = request.CurrentUserId;
                        rec.AdjustedDate = DateTime.Now;
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
