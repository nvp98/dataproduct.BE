using ClosedXML.Excel;
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
    /// <summary>Ném ra khi silo đã có NVL khác và người dùng chưa xác nhận ghi đè.</summary>
    public class NvlConflictException(int currentNvlId, string? currentNvlName)
        : Exception($"Silo đã được mapping với NVL '{currentNvlName}'.")
    {
        public int CurrentNvlId { get; } = currentNvlId;
        public string? CurrentNvlName { get; } = currentNvlName;
    }

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
                ID_LoCao = dto.IdLoCao,
                TenSiLo = dto.TenSiLo,
                ThuTu = dto.ThuTu,
                ThuTuCoDinh = dto.ThuTuCoDinh
            };
            var result = await _repo.AddSiLoAsync(entity);
            return MapSiLo(result);
        }

        public async Task<LGTSSiLoDto?> UpdateSiLoAsync(int id, UpdateLGTSSiLoDto dto)
        {
            var entity = new LG_TSL_SiLo
            {
                ID_LoCao = dto.IdLoCao,
                TenSiLo = dto.TenSiLo,
                ThuTu = dto.ThuTu,
                ThuTuCoDinh = dto.ThuTuCoDinh,
                IsDelete = dto.IsDelete,
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
                IDLoCao = dto.IdLoCao,
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
                IDLoCao = dto.IdLoCao,
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
            => _repo.UpdateXacNhanAsync(dto.Id, dto.XacNhan);

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
            // dto.IdSiLo đã là ThuTuCoDinh — FE truyền trực tiếp, không cần lookup từ actual ID.
            var duplicate = await _repo.GetExistingMappingAsync(dto.IdSiLo, dto.IdLoCao, dto.Ngay, dto.Ca);
            if (duplicate != null)
            {
                var currentNvl = await _repo.GetNvlByIdAsync(duplicate.IDNVL);
                throw new NvlConflictException(duplicate.IDNVL, currentNvl?.TenNVL);
            }

            var entity = new LG_TSL_SiLo_Mapping
            {
                IDLoCao = dto.IdLoCao,
                IDSiLo  = dto.IdSiLo,
                IDNVL   = dto.IdNVL,
                Ngay    = dto.Ngay,
                Ca      = dto.Ca,
                GhiChu  = dto.GhiChu,
            };
            var result = await _repo.AddMappingAsync(entity);
            return MapMapping(result);
        }

        public async Task<LGTSMappingDto?> UpdateMappingAsync(int id, UpdateLGTSMappingDto dto)
        {
            // dto.IdSiLo đã là ThuTuCoDinh — FE truyền trực tiếp, không cần lookup từ actual ID.
            var entity = new LG_TSL_SiLo_Mapping
            {
                IDLoCao = dto.IdLoCao,
                IDSiLo  = dto.IdSiLo,
                IDNVL   = dto.IdNVL,
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
                    IdSiLo = idSiLo.Value,
                    IdMapping = TryGetInt(row, "idMapping", "IDMapping"),
                    IdNVL = TryGetInt(row, "idNVL", "IDNVL"),
                    TenSiLo = TryGetString(row, "silo", "tenSiLo", "TenSiLo"),
                    TenNVL = TryGetString(row, "loaiNguyenNhienLieu", "tenNVL", "TenNVL"),
                    KLTonCuoiKip = TryGetDecimal(row, "klTonCuoiKip", "KLTonCuoiKip", "ton"),
                    ManualKL = TryGetBool(row, "_manualKL", "manualKL"),
                    KLGoc = TryGetDecimal(row, "_klGoc", "klGoc", "KLGoc"),
                    GhiChu = TryGetString(row, "ghiChu", "GhiChu"),
                    ThuTu = TryGetInt(row, "thuTu", "ThuTu", "stt"),
                });
            }

            // Backfill IdNVL từ mapping khi row không có idNVL
            var needLookup = items.Where(x => x.IdNVL == null).ToList();
            if (needLookup.Any())
            {
                // Tải mapping của ngày/ca/lò cao này một lần
                var mappings = await _repo.GetMappingListAsync(idLoCao, ngay, ca);
                // m.Id = IdMapping (PK), m.IdSiLo = ThuTu của silo
                var mappingById   = mappings.ToDictionary(m => m.Id);
                var mappingBySilo = mappings.ToDictionary(m => m.IdSiLo);

                foreach (var item in needLookup)
                {
                    if (item.IdMapping.HasValue && mappingById.TryGetValue(item.IdMapping.Value, out var m1))
                        item.IdNVL = m1.IdNVL;
                    else if (mappingBySilo.TryGetValue(item.IdSiLo, out var m2))
                        item.IdNVL = m2.IdNVL;
                }
            }

            // Bảo vệ KLGoc: giá trị gốc phải là SCADA (tự động), không được là giá trị nhập tay.
            // Nếu _klGoc từ JSON null (lỗi FE cũ hoặc dữ liệu thiếu) → dùng giá trị đã lưu trong DB.
            if (items.Any(x => x.ManualKL && x.KLGoc == null))
            {
                var savedChiTiet = await _repo.GetChiTietByPhieuAsync(phieu.Idphieu);
                var savedByMapping = savedChiTiet
                    .Where(r => r.ManualKL && r.IdMapping.HasValue && r.KLGoc.HasValue)
                    .GroupBy(r => r.IdMapping!.Value)
                    .ToDictionary(g => g.Key, g => g.First());
                var savedBySilo = savedChiTiet
                    .Where(r => r.ManualKL && !r.IdMapping.HasValue && r.KLGoc.HasValue)
                    .GroupBy(r => r.IdSiLo)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var item in items.Where(x => x.ManualKL && x.KLGoc == null))
                {
                    LGTSChiTietDto? saved = null;
                    if (item.IdMapping.HasValue && savedByMapping.TryGetValue(item.IdMapping.Value, out var s1))
                        saved = s1;
                    else if (savedBySilo.TryGetValue(item.IdSiLo, out var s2))
                        saved = s2;
                    item.KLGoc = saved?.KLGoc;
                }
            }

            // Replace mode: luôn xóa dữ liệu cũ của phiếu trước khi ghi mới.
            await _repo.DeleteByPhieuIdAsync(phieu.Idphieu);

            await _repo.UpsertChiTietAsync(new UpsertLGTSChiTietDto
            {
                IdPhieu = phieu.Idphieu,
                IdLoCao = idLoCao,
                Ngay = ngay,
                Ca = ca,
                Items = items,
            });

            return items.Count;
        }

        // ─── Sync chi tiết từ SCADA khi người dùng bấm "Tải dữ liệu" ─────────────
        // Đọc NgaySX/Ca/Scope từ BmPhieu → lấy dữ liệu SCADA → merge ManualKL → lưu DB → trả mapping view.

        public async Task<List<LGTSSiLoMappingViewDto>> SyncChiTietFromScadaAsync(Guid idPhieu)
        {
            // 1. Đọc ngày/ca/lò cao từ BmPhieu — không phụ thuộc frontend truyền xuống
            var phieu = await _repo.GetPhieuByIdAsync(idPhieu)
                ?? throw new InvalidOperationException($"Không tìm thấy phiếu {idPhieu}.");

            if (phieu.NgaySX is null || phieu.Ca is null || phieu.Scope is null)
                throw new InvalidOperationException("Phiếu thiếu thông tin NgaySX, Ca hoặc Scope.");

            var ngay    = phieu.NgaySX.Value.ToDateTime(TimeOnly.MinValue);
            var ca      = phieu.Ca.Value;
            var idLoCao = phieu.Scope.Value;

            // 2. Lấy dữ liệu SCADA mới nhất (mapping view: silo + NVL + KL tồn)
            var viewData = await _repo.GetSiLoByMappingAsync(idLoCao, ngay, ca);

            // 3. Load bản ghi đã lưu để giữ lại ManualKL.
            // Dùng GroupBy thay cho ToDictionary để tránh exception khi có bản ghi trùng khóa.
            // Ưu tiên khớp theo IDMapping (unique per silo-NVL pair, ổn định trong cùng ngày/ca);
            // fallback về IDSiLo nếu IDMapping chưa có (trường hợp dữ liệu cũ).
            var savedRecords = await _repo.GetChiTietByPhieuAsync(idPhieu);

            var manualByMapping = savedRecords
                .Where(r => r.ManualKL && r.IdMapping.HasValue)
                .GroupBy(r => r.IdMapping!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var manualBySilo = savedRecords
                .Where(r => r.ManualKL && !r.IdMapping.HasValue)
                .GroupBy(r => r.IdSiLo)
                .ToDictionary(g => g.Key, g => g.First());

            // 4. Build danh sách items từ SCADA, merge ManualKL
            var items = viewData.Select((v, idx) =>
            {
                // Tìm bản ghi nhập tay: ưu tiên khớp IDMapping, fallback IDSiLo
                LGTSChiTietDto? saved = null;
                if (manualByMapping.TryGetValue(v.IdMapping, out var s1))
                    saved = s1;
                else if (manualBySilo.TryGetValue(v.IdSiLo, out var s2))
                    saved = s2;

                if (saved is not null)
                {
                    return new UpsertLGTSChiTietItemDto
                    {
                        IdSiLo       = v.IdSiLo,
                        IdMapping    = v.IdMapping,
                        IdNVL        = v.IdNVL,
                        TenSiLo      = v.TenSiLo,
                        TenNVL       = v.TenNVL,
                        KLTonCuoiKip = saved.KLTonCuoiKip,   // giữ giá trị nhập tay
                        ManualKL     = true,
                        KLGoc        = v.Ton,                  // cập nhật KL gốc SCADA mới nhất
                        GhiChu       = v.GhiChu,
                        ThuTu        = v.ThuTu ?? idx + 1,
                    };
                }
                return new UpsertLGTSChiTietItemDto
                {
                    IdSiLo       = v.IdSiLo,
                    IdMapping    = v.IdMapping,
                    IdNVL        = v.IdNVL,
                    TenSiLo      = v.TenSiLo,
                    TenNVL       = v.TenNVL,
                    KLTonCuoiKip = v.Ton,
                    ManualKL     = false,
                    KLGoc        = null,
                    GhiChu       = v.GhiChu,
                    ThuTu        = v.ThuTu ?? idx + 1,
                };
            }).ToList();

            // 5. Delete cũ → Insert mới (đã merge)
            await _repo.DeleteByPhieuIdAsync(idPhieu);
            await _repo.UpsertChiTietAsync(new UpsertLGTSChiTietDto
            {
                IdPhieu = idPhieu,
                IdLoCao = idLoCao,
                Ngay    = ngay,
                Ca      = ca,
                Items   = items,
            });

            return viewData;
        }

        // ─── Mappers ─────────────────────────────────────────────────────────────

        private static LGTSSiLoDto MapSiLo(LG_TSL_SiLo e) => new()
        {
            Id = e.ID,
            IdLoCao = e.ID_LoCao,
            TenSiLo = e.TenSiLo,
            ThuTu = e.ThuTu,
            ThuTuCoDinh = e.ThuTuCoDinh,
            IsDelete = e.IsDelete,
        };

        private static LGTSNvlDto MapNvl(LG_TSL_NVL e) => new()
        {
            Id = e.ID,
            IdLoCao = e.IDLoCao,
            TenNVL = e.TenNVL,
            TenNVLTk = e.TenNVL_Tk,
            GhiChu = e.GhiChu,
            NgayTao = e.NgayTao,
            XacNhan = e.XacNhan,
            NgayXacNhan = e.NgayXacNhan,
            IdNguoiXacNhan = e.IDNguoiXacNhan,
        };

        private static LGTSMappingDto MapMapping(LG_TSL_SiLo_Mapping e) => new()
        {
            Id = e.ID,
            IdLoCao = e.IDLoCao,
            IdSiLo = e.IDSiLo,
            IdNVL = e.IDNVL,
            Ngay = e.Ngay,
            Ca = e.Ca,
            GhiChu = e.GhiChu,
            NgayTao = e.NgayTao,
            NguoiTao = e.NguoiTao,
        };

        // ─── Export PDF ──────────────────────────────────────────────────────────

        public async Task<ExportFileResult> ExportTonSiloPdfAsync(Guid idPhieu, List<PheDuyetDto> pheDuyets, bool useKeHoachName = false)
        {
            var phieu = await _repo.GetPhieuByIdAsync(idPhieu)
                ?? throw new Exception("Không tìm thấy phiếu.");

            var kip = phieu.Kip ?? throw new Exception("Phiếu chưa có thông tin kíp.");

            var chiTiet = await _repo.GetChiTietByPhieuAsync(idPhieu);
            var firstRow = chiTiet.FirstOrDefault();

            var ngay  = firstRow?.Ngay ?? DateTime.MinValue;
            var ca    = firstRow?.Ca ?? 0;
            var scope = firstRow?.IdLoCao ?? 0;

            var ngayDisplay = ngay != DateTime.MinValue ? ngay.ToString("dd/MM/yyyy") : "";
            var caLabel = ca == 1 ? "1" : ca == 2 ? "2" : $"Ca {ca}";
            var loCao = scope > 0 ? scope.ToString() : "";

            // Bảng tra cứu TenNVL_Tk theo tên NM, dùng làm fallback khi IdNVL null
            var nvlList = await _repo.GetNvlListAsync(scope > 0 ? scope : null);
            var nvlTkByName = nvlList
                .Where(n => !string.IsNullOrWhiteSpace(n.TenNVL))
                .GroupBy(n => n.TenNVL!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().TenNVLTk, StringComparer.OrdinalIgnoreCase);
            var nvlTkById = nvlList
                .Where(n => n.Id > 0)
                .ToDictionary(n => n.Id, n => n.TenNVLTk);

            var rows = new StringBuilder();
            decimal tongKL = 0;
            int stt = 0;
            foreach (var c in chiTiet)
            {
                stt++;
                tongKL += c.KLTonCuoiKip ?? 0;

                // Ưu tiên: TenNVLTk từ JOIN theo IdNVL → fallback tra theo TenNVL
                var tenNvlTk = c.TenNVLTk;
                if (string.IsNullOrWhiteSpace(tenNvlTk) && c.IdNVL.HasValue && nvlTkById.TryGetValue(c.IdNVL.Value, out var tkById))
                    tenNvlTk = tkById;
                if (string.IsNullOrWhiteSpace(tenNvlTk) && !string.IsNullOrWhiteSpace(c.TenNVL) && nvlTkByName.TryGetValue(c.TenNVL!, out var tkByName))
                    tenNvlTk = tkByName;

                var tenNvlLabel = useKeHoachName && !string.IsNullOrWhiteSpace(tenNvlTk)
                    ? tenNvlTk!
                    : c.TenNVL ?? "";

                rows.Append($@"
                    <tr>
                        <td class=""text-center"">{stt}</td>
                        <td class=""text-center"">{c.TenSiLo ?? ""}</td>
                        <td class=""text-center"">{tenNvlLabel}</td>
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
                .Replace("{{CaLabel}}", $"{caLabel} {kip}")
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

        // ─── Export Excel ─────────────────────────────────────────────────────────

        public async Task<ExportFileResult> ExportTonSiloExcelAsync(Guid idPhieu)
        {
            var phieu = await _repo.GetPhieuByIdAsync(idPhieu)
                ?? throw new Exception("Không tìm thấy phiếu.");

            var chiTiet = await _repo.GetChiTietByPhieuAsync(idPhieu);
            var firstRow = chiTiet.FirstOrDefault();

            var ngay = firstRow?.Ngay ?? DateTime.MinValue;
            var ca = firstRow?.Ca ?? 0;
            var loCao = firstRow?.IdLoCao ?? 0;
            var ngayDisplay = ngay != DateTime.MinValue ? ngay.ToString("dd/MM/yyyy") : "";
            var caLabel = ca == 1 ? "Ca 1" : ca == 2 ? "Ca 2" : $"Ca {ca}";

            var nvlList = await _repo.GetNvlListAsync(loCao > 0 ? loCao : null);
            var nvlTkByName = nvlList
                .Where(n => !string.IsNullOrWhiteSpace(n.TenNVL))
                .GroupBy(n => n.TenNVL!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().TenNVLTk, StringComparer.OrdinalIgnoreCase);
            var nvlTkById = nvlList.Where(n => n.Id > 0).ToDictionary(n => n.Id, n => n.TenNVLTk);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("TonSilo");

            // ── Header ──
            ws.Cell(1, 1).Value = "CÔNG TY CỔ PHẦN THÉP HÒA PHÁT DUNG QUẤT";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Range(1, 1, 1, 5).Merge();

            ws.Cell(2, 1).Value = $"Ngày: {ngayDisplay}    {caLabel}    Lò cao: {(loCao > 0 ? loCao.ToString() : "")}";
            ws.Range(2, 1, 2, 5).Merge();

            // ── Title ──
            ws.Cell(3, 1).Value = $"SỔ GIAO NHẬN TỒN SILO LÒ CAO {(loCao > 0 ? loCao.ToString() : "")}";
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(3, 1).Style.Font.FontSize = 14;
            ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(3, 1, 3, 5).Merge();

            // ── Column headers ──
            string[] hdr = { "STT", "Tên Silo", "Tên NVL", "KL Tồn Cuối Kíp (t)", "Ghi chú" };
            for (int c = 0; c < hdr.Length; c++)
            {
                var cell = ws.Cell(4, c + 1);
                cell.Value = hdr[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#dce6f1");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // ── Data rows ──
            int row = 5;
            int stt = 0;
            decimal tongKL = 0;
            foreach (var c in chiTiet)
            {
                stt++;
                tongKL += c.KLTonCuoiKip ?? 0;

                var tenNvlTk = c.TenNVLTk;
                if (string.IsNullOrWhiteSpace(tenNvlTk) && c.IdNVL.HasValue && nvlTkById.TryGetValue(c.IdNVL.Value, out var tkById))
                    tenNvlTk = tkById;
                if (string.IsNullOrWhiteSpace(tenNvlTk) && !string.IsNullOrWhiteSpace(c.TenNVL) && nvlTkByName.TryGetValue(c.TenNVL!, out var tkByName))
                    tenNvlTk = tkByName;

                ws.Cell(row, 1).Value = stt;
                ws.Cell(row, 2).Value = c.TenSiLo ?? "";
                ws.Cell(row, 3).Value = c.TenNVL ?? "";
                ws.Cell(row, 4).Value = c.KLTonCuoiKip.HasValue ? (double)c.KLTonCuoiKip.Value : (double?)null;
                if (c.KLTonCuoiKip.HasValue) ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.000";
                ws.Cell(row, 5).Value = c.GhiChu ?? "";

                for (int col = 1; col <= 5; col++)
                {
                    ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                row++;
            }

            // ── Tổng ──
            ws.Cell(row, 3).Value = "Tổng KL:";
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 4).Value = (double)tongKL;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.000";
            ws.Cell(row, 4).Style.Font.Bold = true;
            for (int col = 1; col <= 5; col++)
                ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // ── Column widths ──
            ws.Column(1).Width = 6;
            ws.Column(2).Width = 18;
            ws.Column(3).Width = 22;
            ws.Column(4).Width = 20;
            ws.Column(5).Width = 28;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var fileName = $"TonSiLoLoCao_{phieu.SoPhieu ?? idPhieu.ToString("N")}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return new ExportFileResult
            {
                Content = ms.ToArray(),
                FileName = fileName,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
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
