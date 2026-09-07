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
                        ID_CT_BBGN = reader["ID_CT_BBGN"] == DBNull.Value ? null : Convert.ToInt32(reader["ID_CT_BBGN"]),
                    });
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            return result;
        }

        // SP tự tổng hợp theo Tag PLC đang dùng cho Scope — 1 dòng kết quả duy nhất
        // cho mỗi (Ngay, Ca, MaBM, LoaiDuLieu). MaBM truyền vào có dạng "TONGSANLUONG_{ScopeCode}".
        public async Task<TKVVTongSanLuongAutoDto?> GetTongSanLuongAutoAsync(
            DateTime ngay, int ca, string maBM, string loaiDuLieu)
        {
            TKVVTongSanLuongAutoDto? result = null;

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "dbo.SP_TKVV_GetTongSanLuong";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;
                cmd.Parameters.Add(new SqlParameter("@Ngay", ngay.Date));
                cmd.Parameters.Add(new SqlParameter("@Ca", ca));
                cmd.Parameters.Add(new SqlParameter("@MaBM", maBM));
                cmd.Parameters.Add(new SqlParameter("@LoaiDuLieu", loaiDuLieu));

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    result = new TKVVTongSanLuongAutoDto
                    {
                        Ngay = ngay.Date,
                        Ca = ca,
                        MaBM = reader["MaBM"]?.ToString() ?? maBM,
                        LoaiDuLieu = reader["LoaiDuLieu"]?.ToString() ?? loaiDuLieu,
                        TagIDEMS_SuDung = reader["TagIDEMS_SuDung"] == DBNull.Value ? null : reader["TagIDEMS_SuDung"].ToString(),
                        TongSanLuong = reader["TongSanLuong"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TongSanLuong"]),
                        SoTagCoDuLieu = reader["SoTagCoDuLieu"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SoTagCoDuLieu"]),
                    };
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            return result;
        }

        // Đọc TKVV_SanLuongDuLieu đã lưu theo khóa nghiệp vụ Ngay+Ca+Scope (không upsert).
        // scope: chuỗi số "1".."6" (int scope ép chuỗi) — KHÔNG phải mã "TK1" (khác quy ước
        // với @MaBM của SP_TKVV_GetTongSanLuong), khớp đúng cột Scope của TKVV_SanLuongDuLieu.
        private async Task<TKVVSanLuongDuLieuDto?> GetTongSanLuongDuLieuDtoAsync(DateOnly ngay, int ca, string scope)
        {
            var rec = await _context.TKVV_SanLuongDuLieu
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Ngay == ngay && x.Ca == ca && x.Scope == scope);
            if (rec == null) return null;

            return new TKVVSanLuongDuLieuDto
            {
                Id = rec.ID,
                TagID = rec.TagID,
                GiaTriTuDong = rec.GiaTriTuDong,
                GiaTriDieuChinh = rec.GiaTriDieuChinh,
                Ngay = rec.Ngay,
                Ca = rec.Ca,
                Scope = rec.Scope,
                ThoiGian = rec.ThoiGian,
                NgayTao = rec.NgayTao,
            };
        }

        // ─── TKVV_BaoCaoSanLuongChiPhi ────────────────────────────────────────────

        public async Task<LoadDuLieuCanResultDto> LoadAndSaveAsync(LoadDuLieuCanRequestDto request)
        {
            var ngay = new DateTime(request.NgaySX.Year, request.NgaySX.Month, request.NgaySX.Day);
            var caLoad = request.CaSX is > 0 and <= 2 ? request.CaSX.Value : 1;

            // ============================================================
            // MỖI PHIẾU CHỈ DỮ LIỆU CỦA 1 CA.
            // caSX là ca đang được chọn trên form, không load cả 2 ca cùng lúc.
            // ============================================================

            // Lấy từ TKVV_TonSilo, group theo NguyenVatLieuID, tổng Xuat
            var spCa = (await _context.TKVV_TonSilo
                .Where(x =>
                    x.NgaySX == request.NgaySX &&
                    x.Ca == caLoad &&
                    x.Scope == request.Scope &&
                    x.NguyenVatLieuID != null &&
                    !x.IsDelete)
                .GroupBy(x => x.NguyenVatLieuID)
                .Select(g => new
                {
                    NguyenVatLieuID = g.Key!.Value,
                    GiaTri = g.Sum(x => x.Xuat ?? 0),
                })
                .OrderBy(x => x.NguyenVatLieuID)
                .AsNoTracking()
                .ToListAsync())
                .Select(g => new TKVVDuLieuCanDto
                {
                    NguyenVatLieuID = g.NguyenVatLieuID,
                    GiaTri = g.GiaTri,
                    MaSilo = null,
                })
                .ToList();

            var spTongBBGN = await GetDuLieuDuLieuSanLuongTongBBGNAsync(
                ngay,
                caLoad,
                request.Scope);

            var scopeCode = ResolveScopeCode(request.Scope);
            var spTongSanLuong = await GetTongSanLuongAutoAsync(
                ngay,
                caLoad,
                $"TONGSANLUONG_{scopeCode}",
                "TONGSANLUONG");


            // ============================================================
            // TRANSACTION 1
            // XỬ LÝ DỮ LIỆU CÂN NVL / SILO
            // ============================================================

            await using (var tx1 = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // --------------------------------------------------------
                    // Load existing records
                    // Ngày SX + Ca + Scope
                    // --------------------------------------------------------

                    var existing = await _context.TKVV_BaoCaoSanLuongChiPhi
                        .Where(x =>
                            x.NgaySX == request.NgaySX &&
                            x.Ca == caLoad &&
                            x.Scope == request.Scope &&
                            !x.IsDelete)
                        .ToListAsync();


                    // ========================================================
                    // UPSERT DỮ LIỆU CÂN NVL / SILO
                    // ========================================================

                    void UpsertItem(
                        TKVVDuLieuCanDto item,
                        int ca,
                        int thuTu)
                    {
                        var klAmAuto = item.GiaTri;

                        // ----------------------------------------------------
                        // Khóa nghiệp vụ:
                        //
                        // NgaySX
                        // + Ca
                        // + Scope
                        // + NguyenVatLieuID
                        //
                        // Không có SiloID
                        // ----------------------------------------------------

                        var rec = existing.FirstOrDefault(x =>
                            x.Ca == ca &&
                            x.NguyenVatLieuID == item.NguyenVatLieuID);


                        // ====================================================
                        // INSERT
                        // ====================================================

                        if (rec == null)
                        {
                            rec = new TKVV_BaoCaoSanLuongChiPhi
                            {
                                NgaySX = request.NgaySX,

                                Ca = (byte)ca,

                                Scope = request.Scope,

                                NguyenVatLieuID = item.NguyenVatLieuID,

                                Kip = item.MaSilo,

                                ThuTu = thuTu,

                                KLAmAuto = klAmAuto,

                                // Rule INSERT:
                                // KLAm = KLAmAuto
                                KLAm = klAmAuto,

                                // Rule INSERT:
                                // IsAdjusted = 0
                                IsAdjusted = false,

                                CreatedDate = DateTime.Now,

                                CreatedBy = request.CreatedBy
                            };

                            _context.TKVV_BaoCaoSanLuongChiPhi.Add(rec);

                            // ------------------------------------------------
                            // Quan trọng:
                            // Thêm record mới vào existing
                            // để các vòng xử lý tiếp theo nhìn thấy
                            // ------------------------------------------------

                            existing.Add(rec);
                        }
                        else
                        {
                            // =================================================
                            // UPDATE
                            // =================================================

                            // Luôn cập nhật KLAmAuto
                            rec.KLAmAuto = klAmAuto;

                            rec.UpdatedDate = DateTime.Now;

                            // -------------------------------------------------
                            // Rule 3:
                            // Chưa điều chỉnh → cập nhật cả KLAm
                            //
                            // Rule 4:
                            // Đã điều chỉnh → chỉ cập nhật KLAmAuto
                            // -------------------------------------------------

                            if (!rec.IsAdjusted)
                            {
                                rec.KLAm = klAmAuto;
                            }

                            // Giữ nguyên logic hiện tại
                            rec.Kip = item.MaSilo;

                            rec.ThuTu = thuTu;
                        }
                    }


                    // ========================================================
                    // CA ĐANG CHỌN
                    // ========================================================

                    for (int i = 0; i < spCa.Count; i++)
                    {
                        UpsertItem(
                            spCa[i],
                            caLoad,
                            i + 1);
                    }


                    // ========================================================
                    // SAVE TRANSACTION 1
                    // ========================================================

                    await _context.SaveChangesAsync();

                    await tx1.CommitAsync();
                }
                catch
                {
                    await tx1.RollbackAsync();
                    throw;
                }
            }


            // ============================================================
            // TRANSACTION 2
            // XỬ LÝ SẢN LƯỢNG BBGN / THÀNH PHẨM
            // ============================================================

            await using (var tx2 = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // --------------------------------------------------------
                    // Load lại dữ liệu từ DB
                    //
                    // Không dùng lại existing của Transaction 1
                    // --------------------------------------------------------

                    var existing = await _context.TKVV_BaoCaoSanLuongChiPhi
                        .Where(x =>
                            x.NgaySX == request.NgaySX &&
                            x.Ca == caLoad &&
                            x.Scope == request.Scope &&
                            !x.IsDelete)
                        .OrderBy(x => x.ThuTu)
                        .ToListAsync();


                    // ========================================================
                    // UPDATE SẢN LƯỢNG BBGN
                    // ========================================================

                    void UpsertThanhPham(
                        TKVVDuLieuSanLuongTongBBGNDto item,
                        int ca,
                        int thuTu)
                    {
                        // ----------------------------------------------------
                        // Lấy danh sách record của Ca + Scope
                        //
                        // Không map NguyenVatLieuID
                        // Không map ID NVL
                        // ----------------------------------------------------

                        var records = existing
                            .Where(x =>
                                x.Ca == ca &&
                                x.Scope == request.Scope &&
                                !x.IsDelete)
                            .OrderBy(x => x.ThuTu)
                            .ToList();


                        // ----------------------------------------------------
                        // Không có dòng tương ứng
                        // ----------------------------------------------------

                        // ----------------------------------------------------
                        // Lấy record theo thứ tự
                        //
                        // thuTu = 1 → records[0]
                        // thuTu = 2 → records[1]
                        // thuTu = 3 → records[2]
                        // ...
                        // ----------------------------------------------------

                        // Unique index (NgaySX, Ca, Scope, NguyenVatLieuID) chỉ cho 1 row/NVL
                        // → BBGN items vượt quá số NVL rows thì bỏ qua, không thể lưu thêm
                        if (thuTu > records.Count) return;

                        var rec = records[thuTu - 1];


                        // ====================================================
                        // UPDATE THÀNH PHẨM
                        // ====================================================

                        rec.ThanhPhamL1 = item.GiaTri;

                        rec.ThanhPham_Note =
                            item.MaPB_BN
                            + " - "
                            + item.TenXuong_BN;

                        rec.ID_CT_BBGN = item.ID_CT_BBGN;
                        rec.UpdatedDate = DateTime.Now;
                    }


                    // ========================================================
                    // BBGN theo ca đang chọn
                    // ========================================================

                    for (int i = 0; i < spTongBBGN.Count; i++)
                    {
                        UpsertThanhPham(
                            spTongBBGN[i],
                            caLoad,
                            i + 1);
                    }


                    // ========================================================
                    // SAVE TRANSACTION 2
                    // ========================================================

                    await _context.SaveChangesAsync();

                    await tx2.CommitAsync();
                }
                catch
                {
                    await tx2.RollbackAsync();
                    throw;
                }
            }


            // ============================================================
            // TRANSACTION 3
            // UPSERT TỔNG SẢN LƯỢNG TỰ ĐỘNG (SP_TKVV_GetTongSanLuong)
            // → TKVV_SanLuongDuLieu.GiaTriTuDong theo khóa Ngay + Ca + Scope
            // ============================================================

            if (spTongSanLuong != null)
            {
                await using (var tx3 = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var rec = await _context.TKVV_SanLuongDuLieu.FirstOrDefaultAsync(x =>
                            x.Ngay == request.NgaySX &&
                            x.Ca == caLoad &&
                            x.Scope == request.Scope.ToString());

                        if (rec == null)
                        {
                            rec = new TKVV_SanLuongDuLieu
                            {
                                TagID = spTongSanLuong.TagIDEMS_SuDung,
                                GiaTriTuDong = spTongSanLuong.TongSanLuong,
                                Ngay = request.NgaySX,
                                Ca = (byte)caLoad,
                                Scope = request.Scope.ToString(),
                                // Rule INSERT: GiaTriDieuChinh = GiaTriTuDong — mới tạo thì chưa ai
                                // điều chỉnh tay, seed bằng giá trị tự động (khớp rule KLAm=KLAmAuto
                                // của TKVV_BaoCaoSanLuongChiPhi).
                                // GiaTriDieuChinh = spTongSanLuong.TongSanLuong,
                                ThoiGian = DateTime.Now,
                                NgayTao = DateTime.Now,
                            };
                            _context.TKVV_SanLuongDuLieu.Add(rec);
                        }
                        else
                        {
                            // Luôn cập nhật GiaTriTuDong — không đụng GiaTriDieuChinh
                            // (giữ nguyên giá trị người dùng đã điều chỉnh tay, nếu có)
                            rec.TagID = spTongSanLuong.TagIDEMS_SuDung;
                            rec.GiaTriTuDong = spTongSanLuong.TongSanLuong;
                            rec.ThoiGian = DateTime.Now;
                        }

                        await _context.SaveChangesAsync();
                        await tx3.CommitAsync();
                    }
                    catch
                    {
                        await tx3.RollbackAsync();
                        throw;
                    }
                }
            }


            var result = await GetBaoCaoDataAsync(request.NgaySX, request.MaBM, request.Scope, caSX: request.CaSX is > 0 and <= 2 ? request.CaSX.Value : 1);
            result.TongSanLuong = await GetTongSanLuongDuLieuDtoAsync(request.NgaySX, caLoad, request.Scope.ToString());
            return result;
        }

        public async Task<LoadDuLieuCanResultDto> GetBaoCaoDataAsync(DateOnly ngaySX, string maBM, int scope, int? caSX = null)
        {
            var query = (from r in _context.TKVV_BaoCaoSanLuongChiPhi
                         join nvl in _context.TKVV_NguyenVatLieu on r.NguyenVatLieuID equals nvl.ID into nvlG
                         from nvl in nvlG.DefaultIfEmpty()
                         where r.NgaySX == ngaySX && r.Scope == scope && !r.IsDelete
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
                             ID_CT_BBGN = r.ID_CT_BBGN,
                         });

            if (caSX is > 0 and <= 2)
                query = query.Where(x => x.Ca == caSX.Value);

            var rows = await query.OrderBy(x => x.Ca).ThenBy(x => x.ThuTu).ThenBy(x => x.NguyenVatLieuID).AsNoTracking().ToListAsync();

            var result = new LoadDuLieuCanResultDto { Table = rows };
            if (caSX is > 0 and <= 2)
                result.TongSanLuong = await GetTongSanLuongDuLieuDtoAsync(ngaySX, caSX.Value, scope.ToString());
            return result;
        }

        public async Task<LoadDuLieuCanResultDto> GetByPhieuIdAsync(Guid phieuId)
        {
            var rows = await (from r in _context.TKVV_BaoCaoSanLuongChiPhi
                              join nvl in _context.TKVV_NguyenVatLieu on r.NguyenVatLieuID equals nvl.ID into nvlG
                              from nvl in nvlG.DefaultIfEmpty()
                              where r.PhieuID == phieuId && !r.IsDelete
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
                                  ID_CT_BBGN = r.ID_CT_BBGN,
                              }).AsNoTracking().ToListAsync();

            var result = new LoadDuLieuCanResultDto { Table = rows };
            var first = rows.FirstOrDefault();
            if (first?.Scope is > 0 and <= 6)
                result.TongSanLuong = await GetTongSanLuongDuLieuDtoAsync(first.NgaySX, first.Ca, first.Scope.Value.ToString());
            return result;
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

                    rec.PhieuID = request.PhieuID;
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
