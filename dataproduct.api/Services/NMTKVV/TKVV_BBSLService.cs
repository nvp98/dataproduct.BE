using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using System.Text.Json;

namespace dataproduct.api.Services
{
    public class TKVV_BBSLService
    {
        private readonly ITKVV_BBSLRepository _repo;

        public TKVV_BBSLService(ITKVV_BBSLRepository repo)
        {
            _repo = repo;
        }

        // ─── Danh mục NVL ────────────────────────────────────────────────────

        public Task<List<TKVVNguyenVatLieuDto>> GetNvlListAsync(string? maBM, string? scope)
            => _repo.GetNvlListAsync(maBM, scope);

        public async Task<TKVVNguyenVatLieuDto?> GetNvlByIdAsync(int id)
        {
            var e = await _repo.GetNvlByIdAsync(id);
            return e == null ? null : MapNvl(e);
        }

        public async Task<TKVVNguyenVatLieuDto> AddNvlAsync(CreateTKVVNguyenVatLieuDto dto)
        {
            var scopeCode = dto.Scope.HasValue ? TKVV_BBSLRepository.ResolveScopeCode(dto.Scope.Value) : null;
            var entity = new TKVV_NguyenVatLieu
            {
                MaBM = dto.MaBM,
                TenNVL = dto.TenNVL,
                DonViTinh = dto.DonViTinh,
                ThuTu = dto.ThuTu,
                GhiChu = dto.GhiChu,
                TrangThai = true,
                Scope = scopeCode,
                TenScope = dto.TenScope ?? scopeCode,
            };
            var result = await _repo.AddNvlAsync(entity);
            return MapNvl(result);
        }

        public async Task<TKVVNguyenVatLieuDto?> UpdateNvlAsync(int id, UpdateTKVVNguyenVatLieuDto dto)
        {
            var scopeCode = dto.Scope.HasValue ? TKVV_BBSLRepository.ResolveScopeCode(dto.Scope.Value) : null;
            var entity = new TKVV_NguyenVatLieu
            {
                MaBM = dto.MaBM,
                TenNVL = dto.TenNVL,
                DonViTinh = dto.DonViTinh,
                ThuTu = dto.ThuTu,
                TrangThai = dto.TrangThai,
                GhiChu = dto.GhiChu,
                Scope = scopeCode,
                TenScope = dto.TenScope ?? scopeCode,
            };
            var result = await _repo.UpdateNvlAsync(id, entity);
            return result == null ? null : MapNvl(result);
        }

        public Task<bool> DeleteNvlAsync(int id) => _repo.DeleteNvlAsync(id);

        private static TKVVNguyenVatLieuDto MapNvl(TKVV_NguyenVatLieu e) => new()
        {
            Id = e.ID,
            MaBM = e.MaBM,
            TenNVL = e.TenNVL,
            DonViTinh = e.DonViTinh,
            ThuTu = e.ThuTu,
            TrangThai = e.TrangThai,
            GhiChu = e.GhiChu,
            Scope = TKVV_BBSLRepository.ResolveScopeNumber(e.Scope),
            TenScope = e.TenScope ?? e.Scope,
        };

        // ─── Mapping NVL ↔ Tag EMS + Ca ─────────────────────────────────────────
        // Scope kế thừa từ NVL — không lưu lại ở bảng mapping.

        public Task<List<TKVVMappingDto>> GetMappingListAsync()
            => _repo.GetMappingListAsync();

        public async Task<TKVVMappingDto?> GetMappingByIdAsync(int id)
        {
            var e = await _repo.GetMappingByIdAsync(id);
            return e == null ? null : MapMapping(e);
        }

        public async Task<TKVVMappingDto> AddMappingAsync(CreateTKVVMappingDto dto)
        {
            var entity = new TKVV_NVL_TagMapping
            {
                NguyenVatLieuID = dto.NguyenVatLieuID,
                TagIDEMS = dto.TagIDEMS,
                Ca = dto.Ca,
                GhiChu = dto.GhiChu,
                TrangThai = true,
            };
            var result = await _repo.AddMappingAsync(entity);
            return MapMapping(result);
        }

        public async Task<TKVVMappingDto?> UpdateMappingAsync(int id, UpdateTKVVMappingDto dto)
        {
            var entity = new TKVV_NVL_TagMapping
            {
                NguyenVatLieuID = dto.NguyenVatLieuID,
                TagIDEMS = dto.TagIDEMS,
                Ca = dto.Ca,
                TrangThai = dto.TrangThai,
                GhiChu = dto.GhiChu,
            };
            var result = await _repo.UpdateMappingAsync(id, entity);
            return result == null ? null : MapMapping(result);
        }

        public Task<bool> DeleteMappingAsync(int id) => _repo.DeleteMappingAsync(id);

        private static TKVVMappingDto MapMapping(TKVV_NVL_TagMapping e) => new()
        {
            Id = e.ID,
            NguyenVatLieuID = e.NguyenVatLieuID,
            TagIDEMS = e.TagIDEMS,
            Ca = e.Ca,
            TrangThai = e.TrangThai,
            GhiChu = e.GhiChu,
            NgayCapNhat = e.NgayCapNhat,
        };

        // ─── Danh sách Tag PLC từ EMS để chọn khi tạo Mapping ──────────────────
        // Không lọc theo Loai — dùng chung cho toàn NM.TKVV (không chỉ riêng biên
        // bản sản lượng), nên trả mọi loại Tag của xưởng (SANLUONG, TONSILO, ...).
        // Ca suy ra từ LoaiDuLieu: "1"=ngày, "2"=đêm; "0" (đang tích lũy ca hiện
        // tại) trả Ca=null vì không cố định ngày/đêm.
        public async Task<List<EMSMappingTagDto>> GetEmsTagListAsync(string? xuong, string? tagName)
        {
            var raw = await _repo.GetEmsTagListAsync(xuong, tagName);
            return raw
                .Where(x => x.TagIDEMS != null)
                .Select(x => new EMSMappingTagDto
                {
                    Id = x.ID,
                    Xuong = x.Xuong?.Trim() ?? string.Empty,
                    Loai = x.Loai?.Trim(),
                    MaCan = x.MaCan?.Trim(),
                    TenCan = x.TenCan?.Trim(),
                    TagIDEMS = x.TagIDEMS!.Value.ToString(),
                    TagName = x.TagName?.Trim() ?? string.Empty,
                    Ca = x.LoaiDuLieu?.Trim() switch { "1" => 1, "2" => 2, _ => null },
                    GhiChu = x.GhiChu?.Trim(),
                })
                .OrderBy(x => x.Xuong).ThenBy(x => x.TenCan).ThenBy(x => x.Ca)
                .ToList();
        }

        // ─── Dữ liệu PLC thô ─────────────────────────────────────────────────

        public Task<List<TKVVDuLieuRawDto>> GetDataByFilterAsync(
            string? scope, DateTime? ngayBatDau, DateTime? ngayKetThuc)
            => _repo.GetDataByFilterAsync(scope, ngayBatDau, ngayKetThuc);

        public Task<bool> UpdateGiaTriDieuChinhAsync(long id, decimal? giaTriDieuChinh)
            => _repo.UpdateGiaTriDieuChinhAsync(id, giaTriDieuChinh);

        // ─── Tổng tự động (PLC) theo Ngay/Ca/Scope toàn cục (1-6) ──────────────
        // 1 Tag = 1 BM/xưởng, chỉ có 1 số tổng duy nhất (không tách theo sản phẩm).
        // Chỉ để KTV/KCS đối chiếu — KHÔNG tự điền vào Loại 1/2/3/Phế phẩm vì PLC
        // chỉ báo tổng khối lượng, không tự phân loại được.

        public Task<TKVVTongTuDongDto> GetTongTuDongAsync(DateTime ngay, int ca, int scope)
            => _repo.GetTongTuDongAsync(ngay, ca, scope);

        // ─── Mapping Cân (EMS) → Xưởng theo Ngày/Ca/Kíp ─────────────────────────

        public async Task<List<TKVVSanLuongMappingDto>> GetSanLuongMappingListAsync(string? scope)
        {
            var list = await _repo.GetSanLuongMappingListAsync(scope);
            if (list.Count == 0) return list;

            // Enrich TenCan từ danh mục Cân EMS (không có bảng local — chỉ gọi qua SP linked
            // server) để hiển thị tên cân bên cạnh TagID cho dễ đọc, không bắt buộc khớp được.
            try
            {
                var emsTags = await _repo.GetEmsTagListAsync(null, null);
                var tenCanByTagId = emsTags
                    .Where(t => t.TagIDEMS != null)
                    .GroupBy(t => t.TagIDEMS!.Value.ToString())
                    .ToDictionary(g => g.Key, g => g.First().TenCan);

                foreach (var item in list)
                    item.TenCan = tenCanByTagId.GetValueOrDefault(item.TagID);
            }
            catch
            {
                // Lỗi gọi linked server EMS không được chặn việc xem danh sách mapping đã lưu
            }

            return list;
        }

        public async Task<TKVVSanLuongMappingDto?> GetSanLuongMappingByIdAsync(long id)
        {
            var e = await _repo.GetSanLuongMappingByIdAsync(id);
            return e == null ? null : MapSanLuongMapping(e);
        }

        public async Task<TKVVSanLuongMappingDto> AddSanLuongMappingAsync(CreateTKVVSanLuongMappingDto dto)
        {
            var entity = new TKVV_SanLuongMapping
            {
                TagID = dto.TagID,
                Scope = dto.Scope,
                Ca = dto.Ca,
                Kip = dto.Kip,
                TuNgay = dto.TuNgay,
                DenNgay = dto.DenNgay,
                GhiChu = dto.GhiChu,
                NguoiTaoID = dto.NguoiTaoID,
                TrangThai = true,
            };
            var result = await _repo.AddSanLuongMappingAsync(entity);
            return MapSanLuongMapping(result);
        }

        public async Task<TKVVSanLuongMappingDto?> UpdateSanLuongMappingAsync(long id, UpdateTKVVSanLuongMappingDto dto)
        {
            var entity = new TKVV_SanLuongMapping
            {
                TagID = dto.TagID,
                Scope = dto.Scope,
                Ca = dto.Ca,
                Kip = dto.Kip,
                TuNgay = dto.TuNgay,
                DenNgay = dto.DenNgay,
                GhiChu = dto.GhiChu,
                NguoiTaoID = dto.NguoiTaoID,
                TrangThai = dto.TrangThai,
            };
            var result = await _repo.UpdateSanLuongMappingAsync(id, entity);
            return result == null ? null : MapSanLuongMapping(result);
        }

        public Task<bool> DeleteSanLuongMappingAsync(long id) => _repo.DeleteSanLuongMappingAsync(id);

        private static TKVVSanLuongMappingDto MapSanLuongMapping(TKVV_SanLuongMapping e) => new()
        {
            Id = e.ID,
            TagID = e.TagID,
            Scope = e.Scope,
            Ca = e.Ca,
            Kip = e.Kip,
            TuNgay = e.TuNgay,
            DenNgay = e.DenNgay,
            TrangThai = e.TrangThai,
            GhiChu = e.GhiChu,
            NgayTao = e.NgayTao,
            NguoiTaoID = e.NguoiTaoID,
        };

        // ─── Đồng bộ dữ liệu cân/PLC thô (SP_TKVV_GetDuLieuCan_TuMapping) ───────────

        public Task<int> SyncDuLieuTuEmsAsync(DateTime ngay, byte ca, int scope)
        {
            var scopeCode = TKVV_BBSLRepository.ResolveScopeCode(scope);
            return _repo.SyncDuLieuTuEmsAsync(ngay, ca, scopeCode);
        }

        // ─── Chi tiết sản lượng theo phiếu ─────────────────────────────────────

        public Task<List<TKVVChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu)
            => _repo.GetChiTietByPhieuAsync(idPhieu);

        // ─── Ghi chi tiết từ JSON của phiếu (hook post-save từ PhieuService) ──
        // table1[i] có dạng: { thuTu, thoiGian, nguyenVatLieuID, ghiChu,
        //   "1": giaTriLoai1, "2": giaTriLoai2, "3": giaTriLoai3, "4": giaTriPhePham }
        // Mỗi dòng trên bảng UI = đúng 1 bản ghi TKVV_SanLuongChiTiet (4 cột Loai1/
        // Loai2/Loai3/PhePham nằm ngang trên cùng 1 dòng, không nổ thành nhiều dòng
        // theo PhanLoai nữa). Mọi giá trị đều do KTV/KCS tự nhập tay nên IsEdited
        // luôn true. SkippedRows = số dòng có nhập ít nhất 1 giá trị Loại 1-4 nhưng
        // bị bỏ qua vì chưa xác định được Sản lượng (nguyenVatLieuID) — dùng để cảnh
        // báo người dùng thay vì âm thầm lưu phiếu "thành công" mà bảng chi tiết trống trơn.
        public async Task<(int ItemCount, int SkippedRows)> InsertFromPhieuJsonAsync(BmPhieu phieu)
        {
            if (phieu == null || string.IsNullOrWhiteSpace(phieu.DataJson))
                return (0, 0);

            try
            {
                using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
                var root = jsonDoc.RootElement;

                var ngayStr = TryGetString(root, "NgaySX", "ngaySX", "ngay");
                var scope = TryGetInt(root, "scope", "Scope");
                var ca = TryGetInt(root, "ca", "Ca");

                if (scope == null || ca == null || string.IsNullOrWhiteSpace(ngayStr))
                    return (0, 0);
                if (!DateTime.TryParse(ngayStr, out var ngay))
                    return (0, 0);
                if (!TryGetArray(root, "table1", out var table1))
                    return (0, 0);

                var items = new List<TKVV_SanLuongChiTiet>();
                int thuTu = 0;
                int skippedRows = 0;

                foreach (var row in table1.EnumerateArray())
                {
                    if (row.ValueKind != JsonValueKind.Object) continue;
                    thuTu++;

                    var nguyenVatLieuID = TryGetInt(row, "nguyenVatLieuID") ?? 0;
                    if (nguyenVatLieuID == 0)
                    {
                        if (RowHasAnyPhanLoaiValue(row)) skippedRows++;
                        continue;
                    }

                    var thoiGianStr = TryGetString(row, "thoiGian");
                    TimeOnly? thoiGian = TimeOnly.TryParse(thoiGianStr, out var tg) ? tg : null;
                    var ghiChu = TryGetString(row, "ghiChu");

                    var loai1 = TryGetDecimalProperty(row, "1");
                    var loai2 = TryGetDecimalProperty(row, "2");
                    var loai3 = TryGetDecimalProperty(row, "3");
                    var phePham = TryGetDecimalProperty(row, "4");

                    if (loai1 == null && loai2 == null && loai3 == null && phePham == null)
                        continue;

                    items.Add(new TKVV_SanLuongChiTiet
                    {
                        IDPhieu = phieu.Idphieu,
                        Scope = TKVV_BBSLRepository.ResolveScopeCode(scope.Value),
                        Ngay = DateOnly.FromDateTime(ngay),
                        Ca = (byte)ca.Value,
                        NguyenVatLieuID = nguyenVatLieuID,
                        ThuTuDong = thuTu,
                        ThoiGian = thoiGian,
                        Loai1 = loai1,
                        Loai2 = loai2,
                        Loai3 = loai3,
                        PhePham = phePham,
                        IsEdited = true,
                        NguoiSuaID = phieu.NguoiTaoId,
                        NgaySua = DateTime.Now,
                        GhiChu = ghiChu,
                        NgayTao = DateTime.Now,
                    });
                }

                await _repo.ReplaceChiTietAsync(phieu.Idphieu, items);
                return (items.Count, skippedRows);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return (0, 0);
            }
        }

        private static bool RowHasAnyPhanLoaiValue(JsonElement row)
        {
            for (byte phanLoai = 1; phanLoai <= 4; phanLoai++)
                if (row.TryGetProperty(phanLoai.ToString(), out var v) && v.ValueKind != JsonValueKind.Null)
                    return true;
            return false;
        }

        private static decimal? TryGetDecimalProperty(JsonElement row, string propName)
        {
            if (!row.TryGetProperty(propName, out var propVal)) return null;
            if (propVal.ValueKind == JsonValueKind.Null) return null;
            return TryParseDecimalElement(propVal, out var giaTri) ? giaTri : null;
        }

        // ─── JSON helpers ─────────────────────────────────────────────────────

        private static bool TryGetArray(JsonElement obj, string key, out JsonElement array)
        {
            array = default;
            if (obj.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.Array)
            {
                array = el;
                return true;
            }
            return false;
        }

        private static string? TryGetString(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (obj.TryGetProperty(key, out var val) && val.ValueKind != JsonValueKind.Null)
                    return val.ValueKind == JsonValueKind.String ? val.GetString() : val.ToString();
            }
            return null;
        }

        private static int? TryGetInt(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!obj.TryGetProperty(key, out var val) || val.ValueKind == JsonValueKind.Null)
                    continue;
                if (val.ValueKind == JsonValueKind.Number && val.TryGetInt32(out var n))
                    return n;
                if (val.ValueKind == JsonValueKind.String && int.TryParse(val.GetString(), out var s))
                    return s;
            }
            return null;
        }

        private static bool TryParseDecimalElement(JsonElement el, out decimal result)
        {
            if (el.ValueKind == JsonValueKind.Number)
                return el.TryGetDecimal(out result);

            if (el.ValueKind == JsonValueKind.String)
                return decimal.TryParse(
                    el.GetString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out result);

            result = 0;
            return false;
        }
    }
}
