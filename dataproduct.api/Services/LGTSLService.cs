using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.DTOs.Export;
using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Text;
using System.Text.Json;

namespace dataproduct.api.Services
{
    public class LGTSLService
    {
        private readonly ILGTSLRepository _repo;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public LGTSLService(
            ILGTSLRepository repo,
            IConverter pdfConverter,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _repo = repo;
            _pdfConverter = pdfConverter;
            _env = env;
            _configuration = configuration;
        }

        // ─── SiLo ────────────────────────────────────────────────────────────────

        public Task<List<LGTSSiLoDto>> GetAllSiLoListAsync(int? idLoCao)
            => _repo.GetSiLoListAsync(idLoCao);

        public Task<List<LGTSSiLoMappingViewDto>> GetSiLoByMappingAsync(int? idLoCao, DateTime? ngay, int? ca)
            => _repo.GetSiLoByMappingAsync(idLoCao, ngay, ca);
        public async Task<LGTSSiLoDto?> GetSiLoByIdAsync(int id)
        {
            var e = await _repo.GetSiLoByIdAsync(id);
            return e == null ? null : MapSiLo(e);
        }

        public async Task<LGTSSiLoDto> AddSiLoAsync(CreateLGTSSiLoDto dto)
        {
            var entity = new LG_TSL_SiLo
            {
                ID_LoCao = dto.IDLoCao,
                TenSiLo = dto.TenSiLo,
                ThuTu = dto.ThuTu,
            };
            var result = await _repo.AddSiLoAsync(entity);
            return MapSiLo(result);
        }

        public async Task<LGTSSiLoDto?> UpdateSiLoAsync(int id, UpdateLGTSSiLoDto dto)
        {
            var entity = new LG_TSL_SiLo
            {
                ID_LoCao = dto.IDLoCao,
                TenSiLo = dto.TenSiLo,
                ThuTu = dto.ThuTu,
            };
            var result = await _repo.UpdateSiLoAsync(id, entity);
            return result == null ? null : MapSiLo(result);
        }

        public Task<bool> DeleteSiLoAsync(int id) => _repo.DeleteSiLoAsync(id);

        // ─── NVL ─────────────────────────────────────────────────────────────────

        public Task<List<LGTSNvlDto>> GetNvlListAsync(int? idLoCao)
            => _repo.GetNvlListAsync(idLoCao);

        public async Task<LGTSNvlDto?> GetNvlByIdAsync(int id)
        {
            var e = await _repo.GetNvlByIdAsync(id);
            return e == null ? null : MapNvl(e);
        }

        public async Task<LGTSNvlDto> AddNvlAsync(CreateLGTSNvlDto dto)
        {

            var entity = new LG_TSL_NVL
            {
                IDLoCao = dto.IDLoCao,
                TenNVL = dto.TenNVL?.Trim(),
                TenNVL_Tk = dto.TenNVLTk,
                GhiChu = dto.GhiChu,
                XacNhan = dto.XacNhan,
            };
            var result = await _repo.AddNvlAsync(entity);
            return MapNvl(result);
        }

        public async Task<LGTSNvlDto?> UpdateNvlAsync(int id, UpdateLGTSNvlDto dto)
        {
       

            var entity = new LG_TSL_NVL
            {
                IDLoCao = dto.IDLoCao,
                TenNVL = dto.TenNVL.Trim(),
                TenNVL_Tk = dto.TenNVLTk,
                GhiChu = dto.GhiChu,
                XacNhan = dto.XacNhan,
            };
            var result = await _repo.UpdateNvlAsync(id, entity);
            return result == null ? null : MapNvl(result);
        }

        public Task<bool> DeleteNvlAsync(int id) => _repo.DeleteNvlAsync(id);

        public Task<bool> UpdateXacNhanAsync(UpdateLGTSXacNhanDto dto)
            => _repo.UpdateXacNhanAsync(dto.ID, dto.XacNhan);

        // ─── Mapping ─────────────────────────────────────────────────────────────

        public Task<List<LGTSMappingDto>> GetMappingListAsync(int? idLoCao, DateTime? ngay, int? ca)
            => _repo.GetMappingListAsync(idLoCao, ngay, ca);

        public async Task<LGTSMappingDto?> GetMappingByIdAsync(int id)
        {
            var e = await _repo.GetMappingByIdAsync(id);
            return e == null ? null : MapMapping(e);
        }

        public async Task<LGTSMappingDto> AddMappingAsync(CreateLGTSMappingDto dto)
        {
            // IDSiLo trong Mapping lưu ThuTu (không phải ID) → tra cứu ThuTu từ silo ID
            var thuTu = await _repo.GetSiLoThuTuAsync(dto.IDSiLo, dto.IDLoCao)
                        ?? throw new InvalidOperationException($"Silo ID={dto.IDSiLo} không tồn tại hoặc chưa có ThuTu.");

            var entity = new LG_TSL_SiLo_Mapping
            {
                IDLoCao = dto.IDLoCao,
                IDSiLo  = thuTu,
                IDNVL   = dto.IDNVL,
                Ngay    = dto.Ngay,
                Ca      = dto.Ca,
                GhiChu  = dto.GhiChu,
            };
            var result = await _repo.AddMappingAsync(entity);
            return MapMapping(result);
        }

        public async Task<LGTSMappingDto?> UpdateMappingAsync(int id, UpdateLGTSMappingDto dto)
        {
            // IDSiLo trong Mapping lưu ThuTu → tra cứu ThuTu từ silo ID
            var thuTu = await _repo.GetSiLoThuTuAsync(dto.IDSiLo, dto.IDLoCao)
                        ?? throw new InvalidOperationException($"Silo ID={dto.IDSiLo} không tồn tại hoặc chưa có ThuTu.");

            var entity = new LG_TSL_SiLo_Mapping
            {
                IDLoCao = dto.IDLoCao,
                IDSiLo  = thuTu,
                IDNVL   = dto.IDNVL,
                Ngay    = dto.Ngay,
                Ca      = dto.Ca,
                GhiChu  = dto.GhiChu,
            };
            var result = await _repo.UpdateMappingAsync(id, entity);
            return result == null ? null : MapMapping(result);
        }

        public Task<bool> DeleteMappingAsync(int id) => _repo.DeleteMappingAsync(id);

        // ─── Chi tiết tồn silo theo phiếu ────────────────────────────────────────

        public Task UpsertChiTietAsync(UpsertLGTSChiTietDto dto) => _repo.UpsertChiTietAsync(dto);

        public Task<List<LGTSChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu) => _repo.GetChiTietByPhieuAsync(idPhieu);


        // ─── InsertFromPhieu ──────────────────────────────────────────────────────

        public async Task<int> InsertFromPhieuJsonAsync(BmPhieu phieu)
        {
            if (phieu == null || string.IsNullOrWhiteSpace(phieu.DataJson))
                return 0;

            using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
            var root = jsonDoc.RootElement;

            var ngayStr = TryGetString(root, "NgaySX", "ngaySX", "ngay");
            var idLoCao = TryGetInt(root, "scope", "Scope", "idLoCao", "IDLoCao") ?? 0;
            var ca = TryGetInt(root, "ca", "Ca") ?? 0;

            if (idLoCao == 0 || ca == 0 || string.IsNullOrWhiteSpace(ngayStr))
                return 0;

            if (!DateTime.TryParse(ngayStr, out var ngay))
                return 0;

            if (!TryGetArray(root, "table1", out var table1))
                return 0;

            var items = new List<UpsertLGTSChiTietItemDto>();
            foreach (var row in table1.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object)
                    continue;

                var idSiLo = TryGetInt(row, "idSiLo", "IDSiLo");
                if (idSiLo == null || idSiLo == 0)
                    continue;

                items.Add(new UpsertLGTSChiTietItemDto
                {
                    IDSiLo = idSiLo.Value,
                    IDMapping = TryGetInt(row, "idMapping", "IDMapping"),
                    IDNVL = TryGetInt(row, "idNVL", "IDNVL"),
                    TenSiLo = TryGetString(row, "silo", "tenSiLo", "TenSiLo"),
                    TenNVL = TryGetString(row, "loaiNguyenNhienLieu", "tenNVL", "TenNVL"),
                    KLTonCuoiKip = TryGetDecimal(row, "klTonCuoiKip", "KLTonCuoiKip", "ton"),
                    ManualKL = TryGetBool(row, "_manualKL", "manualKL"),
                    KLGoc = TryGetDecimal(row, "_klGoc", "klGoc", "KLGoc"),
                    GhiChu = TryGetString(row, "ghiChu", "GhiChu"),
                    ThuTu = TryGetInt(row, "thuTu", "ThuTu", "stt"),
                });
            }

            // Replace mode: luôn xóa dữ liệu cũ của phiếu trước khi ghi mới.
            await _repo.DeleteByPhieuIdAsync(phieu.Idphieu);

            await _repo.UpsertChiTietAsync(new UpsertLGTSChiTietDto
            {
                IDPhieu = phieu.Idphieu,
                IDLoCao = idLoCao,
                Ngay = ngay,
                Ca = ca,
                Items = items,
            });

            return items.Count;
        }

        // ─── Mappers ─────────────────────────────────────────────────────────────

        private static LGTSSiLoDto MapSiLo(LG_TSL_SiLo e) => new()
        {
            ID = e.ID,
            IDLoCao = e.ID_LoCao,
            TenSiLo = e.TenSiLo,
            ThuTu = e.ThuTu,
        };

        private static LGTSNvlDto MapNvl(LG_TSL_NVL e) => new()
        {
            ID = e.ID,
            IDLoCao = e.IDLoCao,
            TenNVL = e.TenNVL,
            TenNVLTk = e.TenNVL_Tk,
            GhiChu = e.GhiChu,
            NgayTao = e.NgayTao,
            XacNhan = e.XacNhan,
            NgayXacNhan = e.NgayXacNhan,
            IDNguoiXacNhan = e.IDNguoiXacNhan,
        };

        private static LGTSMappingDto MapMapping(LG_TSL_SiLo_Mapping e) => new()
        {
            ID = e.ID,
            IDLoCao = e.IDLoCao,
            IDSiLo = e.IDSiLo,
            IDNVL = e.IDNVL,
            Ngay = e.Ngay,
            Ca = e.Ca,
            GhiChu = e.GhiChu,
            NgayTao = e.NgayTao,
            NguoiTao = e.NguoiTao,
        };

        // ─── Export PDF ──────────────────────────────────────────────────────────

        public async Task<ExportFileResult> ExportTonSiloPdfAsync(Guid idPhieu, List<PheDuyetDto> pheDuyets)
        {
            var phieu = await _repo.GetPhieuByIdAsync(idPhieu)
                ?? throw new Exception("Không tìm thấy phiếu.");

            if (string.IsNullOrWhiteSpace(phieu.DataJson))
                throw new Exception("Phiếu không có dữ liệu JSON.");

            using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
            var root = jsonDoc.RootElement;

            var ngayStr = TryGetString(root, "NgaySX", "ngaySX", "ngay");
            var ca = TryGetInt(root, "ca", "Ca") ?? 0;
            var scope = TryGetInt(root, "scope", "Scope", "idLoCao") ?? 0;

            DateTime.TryParse(ngayStr, out var ngay);
            var ngayDisplay = ngay != DateTime.MinValue ? ngay.ToString("dd/MM/yyyy") : "";
            var caLabel = ca == 1 ? "Ca Ngày" : ca == 2 ? "Ca Đêm" : $"Ca {ca}";
            var loCao = scope > 0 ? scope.ToString() : "";

            var chiTiet = await _repo.GetChiTietByPhieuAsync(idPhieu);

            var rows = new StringBuilder();
            decimal tongKL = 0;
            int stt = 0;
            foreach (var c in chiTiet)
            {
                stt++;
                tongKL += c.KLTonCuoiKip ?? 0;

                rows.Append($@"
                    <tr>
                        <td class=""text-center"">{stt}</td>
                        <td class=""text-center"">{c.TenSiLo ?? ""}</td>
                        <td class=""text-center"">{c.TenNVL ?? ""}</td>
                        <td class=""text-center"">
                            {(c.KLTonCuoiKip.HasValue ? c.KLTonCuoiKip.Value.ToString("N3") : "")}
                        </td>
                        <td class=""text-center"">{c.GhiChu ?? ""}</td>
                    </tr>");
            }

            var nguoiGiao = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);
            var nguoiNhan = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1);

            var logoUrl = _configuration.GetValue<string>("AppSettings:LogoUrl")
                          ?? "https://report.hoaphatdungquat.vn/img/logoHP.png";
            var logoBase64 = await ConvertImageUrlToBase64Async(logoUrl);

            var signGiao = await FormatChuKyBase64Async(nguoiGiao?.ChuKy, nguoiGiao?.TinhTrang == 1);
            var signNhan = await FormatChuKyBase64Async(nguoiNhan?.ChuKy, nguoiNhan?.TinhTrang == 1);

            var templatePath = Path.Combine(
                _env.WebRootPath,
                "template_html",
                "BM.07-QT.05.09_So_giao_nhan_ton_silo_lo_cao.html");

            var html = await File.ReadAllTextAsync(templatePath);

            html = html
                .Replace("{{LogoUrl}}", logoBase64)
                .Replace("{{LoCao}}", loCao)
                .Replace("{{CaLabel}}", caLabel)
                .Replace("{{NgaySX}}", ngayDisplay)
                .Replace("{{Rows}}", rows.ToString())
                .Replace("{{TongKhoiLuong}}", tongKL.ToString("N3"))
                .Replace("{{Sign_NguoiGiao}}", signGiao)
                .Replace("{{Ten_NguoiGiao}}", nguoiGiao?.HoVaTen ?? "")
                .Replace("{{Sign_NguoiNhan}}", signNhan)
                .Replace("{{Ten_NguoiNhan}}", nguoiNhan?.HoVaTen ?? "");

            var doc = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Portrait,
                },
                Objects =
                {
                    new ObjectSettings
                    {
                        HtmlContent = html,
                        WebSettings =
                        {
                            DefaultEncoding = "utf-8",
                            LoadImages = true,
                            EnableJavascript = false,
                            PrintMediaType = true,
                        },
                        LoadSettings =
                        {
                            BlockLocalFileAccess = false,
                            LoadErrorHandling = ContentErrorHandling.Ignore,
                        }
                    }
                }
            };

            var pdfBytes = _pdfConverter.Convert(doc);
            var fileName = $"TonSiLoLoCao_{phieu.SoPhieu ?? idPhieu.ToString("N")}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

            return new ExportFileResult
            {
                Content = pdfBytes,
                FileName = fileName,
                ContentType = "application/pdf",
            };
        }

        private async Task<string> ConvertImageUrlToBase64Async(string imageUrl)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var bytes = await client.GetByteArrayAsync(imageUrl);
                var ext = Path.GetExtension(imageUrl).TrimStart('.').ToLower();
                var mime = ext == "png" ? "image/png" : "image/jpeg";
                return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
            }
            catch
            {
                return imageUrl;
            }
        }

        private async Task<string> FormatChuKyBase64Async(string? chuKy, bool daKy = false)
        {
            // Nếu chưa có chữ ký
            if (string.IsNullOrWhiteSpace(chuKy))
            {
                // Nếu đã ký nhưng chưa có ảnh chữ ký
                if (daKy)
                {
                    return @"
                        <div style='text-align:center'>
                            <div style='font-style:italic;color:red'>Đã ký</div>
                            <div style='font-size:11px;color:red'>(Chưa cập nhật chữ ký)</div>
                        </div>";
                }

                return "";
            }

            // Nếu đã là base64 image
            if (chuKy.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                return $"<img src=\"{chuKy}\" style=\"max-width:150px;max-height:80px;\" />";
            }

            // Nếu là URL
            if (chuKy.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var base64 = await ConvertImageUrlToBase64Async(chuKy);
                if (!string.IsNullOrEmpty(base64))
                {
                    return $"<img src=\"{base64}\" style=\"max-width:150px;max-height:80px;\" />";
                }
            }
            else if (chuKy.StartsWith("/"))
            {
                var domain = _configuration.GetValue<string>("AppSettings:Domain") ?? "https://report.hoaphatdungquat.vn";
                var fullUrl = domain.TrimEnd('/') + chuKy;

                var base64 = await ConvertImageUrlToBase64Async(fullUrl);
                if (!string.IsNullOrEmpty(base64))
                {
                    return $"<img src=\"{base64}\" style=\"max-width:150px;max-height:80px;\" />";
                }
            }

            return @"
                    <div style='text-align:center'>
                        <div style='font-style:italic;color:red'>Đã ký</div>
                        <div style='font-size:11px;color:red'>(Chưa cập nhật chữ ký)</div>
                    </div>";
        }

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

        private static bool TryGetBool(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!obj.TryGetProperty(key, out var val) || val.ValueKind == JsonValueKind.Null)
                    continue;

                if (val.ValueKind == JsonValueKind.True) return true;
                if (val.ValueKind == JsonValueKind.False) return false;
                if (val.ValueKind == JsonValueKind.String && bool.TryParse(val.GetString(), out var b)) return b;
            }
            return false;
        }

        private static decimal? TryGetDecimal(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!obj.TryGetProperty(key, out var val) || val.ValueKind == JsonValueKind.Null)
                    continue;

                if (val.ValueKind == JsonValueKind.Number && val.TryGetDecimal(out var d))
                    return d;

                if (val.ValueKind == JsonValueKind.String && decimal.TryParse(val.GetString(), out var s))
                    return s;
            }
            return null;
        }
    }
}
