using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.DTOs.Export;
using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace dataproduct.api.Services
{
    public class LGNLService
    {
        private readonly ILGNLRepository _repo;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<Guid, PhieuLockEntry> _phieuLocks = new();

        private sealed class PhieuLockEntry
        {
            public SemaphoreSlim Semaphore { get; } = new(1, 1);
            public int RefCount;
        }

        public LGNLService(
            ILGNLRepository repo,
            IConverter pdfConverter,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _repo = repo;
            _pdfConverter = pdfConverter;
            _env = env;
            _configuration = configuration;
        }
        // ─── TS Mapping lookup ───────────────────────────────────────────────

        public Task<List<LGNLTsMappingDto>> GetTsMappingListAsync()
            => _repo.GetTsMappingListAsync();

        // ─── SiLo Master ─────────────────────────────────────────────────────

        public async Task<List<LGNLSiLoMasterDto>> GetSiLoMasterListAsync(int? idLoCao)
        {
            var list = await _repo.GetSiLoMasterListAsync(idLoCao);
            return list.Select(MapSiLoMaster).ToList();
        }

        public async Task<LGNLSiLoMasterDto?> GetSiLoMasterByIdAsync(int id)
        {
            var e = await _repo.GetSiLoMasterByIdAsync(id);
            return e == null ? null : MapSiLoMaster(e);
        }

        public async Task<LGNLSiLoMasterDto> AddSiLoMasterAsync(CreateLGNLSiLoMasterDto dto)
        {
            var entity = new LG_NL_SiLo
            {
                IDLoCao = dto.IDLoCao,
                TenSiLo = dto.TenSiLo,
                ThuTu   = dto.ThuTu,
                TagKey  = dto.TagKey
            };
            var result = await _repo.AddSiLoMasterAsync(entity);
            return MapSiLoMaster(result);
        }

        public async Task<LGNLSiLoMasterDto?> UpdateSiLoMasterAsync(int id, UpdateLGNLSiLoMasterDto dto)
        {
            var entity = new LG_NL_SiLo
            {
                IDLoCao = dto.IDLoCao,
                TenSiLo = dto.TenSiLo,
                ThuTu   = dto.ThuTu,
                TagKey  = dto.TagKey
            };
            var result = await _repo.UpdateSiLoMasterAsync(id, entity);
            return result == null ? null : MapSiLoMaster(result);
        }

        public Task<bool> DeleteSiLoMasterAsync(int id) => _repo.DeleteSiLoMasterAsync(id);

        // ─── Mapping ─────────────────────────────────────────────────────────

        public Task<List<LGNLMappingDto>> GetMappingListAsync(DateTime? ngay, int? idCa, int? idLoCao)
            => _repo.GetMappingListAsync(ngay, idCa, idLoCao);

        public async Task<LGNLMappingDto?> GetMappingByIdAsync(int id)
        {
            var e = await _repo.GetMappingByIdAsync(id);
            return e == null ? null : MapMapping(e);
        }

        public async Task<LGNLMappingDto> AddMappingAsync(CreateLGNLMappingDto dto)
        {
            var entity = new LG_NL_Mapping
            {
                Ngay    = dto.Ngay,
                IDCa    = dto.IDCa,
                IDLoCao = dto.IDLoCao,
                IDSiLo  = dto.IDSiLo,
                IDNVL   = dto.IDNVL,
                GhiChu  = dto.GhiChu
            };
            var result = await _repo.AddMappingAsync(entity);
            return MapMapping(result);
        }

        public async Task<LGNLMappingDto?> UpdateMappingAsync(int id, UpdateLGNLMappingDto dto)
        {
            var entity = new LG_NL_Mapping
            {
                Ngay    = dto.Ngay,
                IDCa    = dto.IDCa,
                IDLoCao = dto.IDLoCao,
                IDSiLo  = dto.IDSiLo,
                IDNVL   = dto.IDNVL,
                GhiChu  = dto.GhiChu
            };
            var result = await _repo.UpdateMappingAsync(id, entity);
            return result == null ? null : MapMapping(result);
        }

        public Task<bool> DeleteMappingAsync(int id) => _repo.DeleteMappingAsync(id);

        // ─── Nhóm NVL ─────────────────────────────────────────────────────────

        public async Task<List<LGNLNhomNvlDto>> GetNhomNvlListAsync(int? idLoCao)
        {
            var list = await _repo.GetNhomNvlListAsync(idLoCao);
            return list.Select(MapNhomNvl).ToList();
        }

        public async Task<LGNLNhomNvlDto?> GetNhomNvlByIdAsync(int id)
        {
            var e = await _repo.GetNhomNvlByIdAsync(id);
            return e == null ? null : MapNhomNvl(e);
        }

        public async Task<LGNLNhomNvlDto> AddNhomNvlAsync(CreateLGNLNhomNvlDto dto)
        {
            var entity = new LG_NL_NhomNVL
            {
                IDLoCao = dto.IDLoCao,
                TenNhom = dto.TenNhom,
                ThuTu   = dto.ThuTu,
                GhiChu  = dto.GhiChu
            };
            var result = await _repo.AddNhomNvlAsync(entity);
            return MapNhomNvl(result);
        }

        public async Task<LGNLNhomNvlDto?> UpdateNhomNvlAsync(int id, UpdateLGNLNhomNvlDto dto)
        {
            var entity = new LG_NL_NhomNVL
            {
                IDLoCao = dto.IDLoCao,
                TenNhom = dto.TenNhom,
                ThuTu   = dto.ThuTu,
                GhiChu  = dto.GhiChu
            };
            var result = await _repo.UpdateNhomNvlAsync(id, entity);
            return result == null ? null : MapNhomNvl(result);
        }

        public Task<bool> DeleteNhomNvlAsync(int id) => _repo.DeleteNhomNvlAsync(id);

        // ─── NVL ─────────────────────────────────────────────────────────────

        public Task<List<LGNLNvlDto>> GetNvlListAsync(int? idLoCao)
            => _repo.GetNvlListAsync(idLoCao);

        public async Task<LGNLNvlDto?> GetNvlByIdAsync(int id)
        {
            var e = await _repo.GetNvlByIdAsync(id);
            return e == null ? null : MapNvlEntity(e);
        }

        public async Task<LGNLNvlDto> AddNvlAsync(CreateLGNLNvlDto dto)
        {
            var entity = new LG_NL_NVL
            {
                IDLoCao     = dto.IDLoCao,
                IDNhomNVL   = dto.IDNhomNVL,
                TenNVL_NM   = dto.TenNVL_NM,
                ThuTu       = dto.ThuTu,
                GhiChu      = dto.GhiChu,
            };
            var result = await _repo.AddNvlAsync(entity);
            return MapNvlEntity(result);
        }

        public async Task<LGNLNvlDto?> UpdateNvlAsync(int id, UpdateLGNLNvlDto dto)
        {
            var entity = new LG_NL_NVL
            {
                IDLoCao     = dto.IDLoCao,
                IDNhomNVL   = dto.IDNhomNVL,
                TenNVL_NM   = dto.TenNVL_NM,
                TenNVL_TK = dto.TenNVL_TK,
                XacNhan = dto.XacNhan,
                ThuTu       = dto.ThuTu,
                GhiChu      = dto.GhiChu,
            };
            var result = await _repo.UpdateNvlAsync(id, entity);
            return result == null ? null : MapNvlEntity(result);
        }

        public Task<bool> DeleteNvlAsync(int id) => _repo.DeleteNvlAsync(id);

        public async Task<bool> UpdateXacNhanAsync(UpdateXacNhanDto dto)
        {
            var entity = await _repo.GetNvlByIdAsync(dto.ID);
            if (entity == null) return false;

            entity.XacNhan = dto.XacNhan;
            entity.NgayXacNhan = DateTime.Now;

            await _repo.UpdateNvlAsync(dto.ID, entity);
            return true;
        }
        private static LGNLSiLoMasterDto MapSiLoMaster(LG_NL_SiLo e) => new()
        {
            ID      = e.ID,
            IDLoCao = e.IDLoCao,
            TenSiLo = e.TenSiLo,
            ThuTu   = e.ThuTu,
            NgayTao = e.NgayTao,
            TagKey  = e.TagKey
        };

        private static LGNLMappingDto MapMapping(LG_NL_Mapping e) => new()
        {
            ID          = e.ID,
            Ngay        = e.Ngay,
            IDCa        = e.IDCa,
            IDLoCao     = e.IDLoCao,
            IDSiLo      = e.IDSiLo,
            IDNVL       = e.IDNVL,
            ThoiDiemBD  = e.ThoiDiemBD,
            NgayHetHL   = e.NgayHetHL,
            IDCaHetHL   = e.IDCaHetHL,
            GhiChu      = e.GhiChu,
            NgayTao     = e.NgayTao
        };

        private static LGNLNhomNvlDto MapNhomNvl(LG_NL_NhomNVL e) => new()
        {
            ID      = e.ID,
            IDLoCao = e.IDLoCao,
            TenNhom = e.TenNhom,
            ThuTu   = e.ThuTu,
            GhiChu  = e.GhiChu,
            NgayTao = e.NgayTao
        };

        private static LGNLNvlDto MapNvlEntity(LG_NL_NVL e) => new()
        {
            ID          = e.ID,
            IDLoCao     = e.IDLoCao,
            IDNhomNVL   = e.IDNhomNVL,
            TenNVL_NM   = e.TenNVL_NM,
            ThuTu       = e.ThuTu,
            GhiChu      = e.GhiChu,
            NgayTao     = e.NgayTao,
        };

        // ─── Dữ liệu theo LoCao, Ngày ───────────────────────────────

        public async Task<List<LGNLDuLieuScadaDto>> GetDataByFilterAsync(
            int? idLoCao, DateTime? ngayBatDau, DateTime? ngayKetThuc)
        {
            return await _repo.GetDataByFilterAsync(idLoCao, ngayBatDau, ngayKetThuc);
        }

        // ─── Pivot dữ liệu nạp liệu theo Silo mapping ───────────────

        public async Task<LGNLDuLieuSiLoResult> GetDuLieuSiloPivotAsync(
            DateTime ngay, int idCa, int idLoCao)
        {
            return await _repo.GetDuLieuSiloPivotAsync(ngay, idCa, idLoCao);
        }

        // ─── Snapshot trạng thái Silo ──────────────────────────────

        public Task<List<LGNLSiloSnapshotDto>> GetSiloSnapshotAsync(
            int idLoCao, DateTime ngay, int idCa)
            => _repo.GetSiloSnapshotAsync(idLoCao, ngay, idCa);

        // ─── Đổi NVL cho silo tại thời điểm cụ thể trong ca ─────────

        public async Task<LG_NL_Mapping> ChangeSiLoNVLAsync(
            int idLoCao, DateTime ngay, int idCa, int idSiLo, int idNVLMoi,
            DateTime thoiDiem, string? ghiChu)
        {
            return await _repo.ChangeSiLoNVLAsync(
                idLoCao, ngay, idCa, idSiLo, idNVLMoi, thoiDiem, ghiChu);
        }

        // ─── Chi tiết nạp liệu theo phiếu ────────────────────────────────────────

        public Task<List<LGNLChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu)
            => _repo.GetChiTietByPhieuAsync(idPhieu);

        // ─── InsertFromPhieu ──────────────────────────────────────────────────────

        public async Task<int> InsertFromPhieuJsonAsync(BmPhieu phieu)
        {
            if (phieu == null || string.IsNullOrWhiteSpace(phieu.DataJson))
                return 0;
            try
            {
                using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
                var root = jsonDoc.RootElement;

                var ngayStr = TryGetString(root, "NgaySX", "ngaySX", "ngay");
                var idLoCao = TryGetInt(root, "scope", "Scope", "idLoCao", "IDLoCao");
                var idCa = TryGetInt(root, "ca", "Ca");

                if (idLoCao == null || idCa == null || string.IsNullOrWhiteSpace(ngayStr))
                    return 0;

                if (!DateTime.TryParse(ngayStr, out var ngay))
                    return 0;

                if (!TryGetArray(root, "table1", out var table1))
                    return 0;

                // Parse độ ẩm % per IDNVL từ root: { "doAm": { "123": 5.0, "456": 3.5 } }
                var doAmByNvl = new Dictionary<int, decimal>();
                if (root.TryGetProperty("doAm", out var doAmEl) && doAmEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var kv in doAmEl.EnumerateObject())
                    {
                        if (int.TryParse(kv.Name, out var nvlId) && kv.Value.TryGetDecimal(out var da))
                            doAmByNvl[nvlId] = da;
                    }
                }

                // Các cột cố định — không phải NVL động
                var fixedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "soMe", "meGio", "thoiGianNapLieu", "cheDoNapLieu",
                    "thuocThamLieu1", "thuocThamLieu2", "ghiChu", "key", "id", "time"
                };

                var items = new List<LG_NL_ChiTiet>();
                int thuTu = 0;

                foreach (var row in table1.EnumerateArray())
                {
                    if (row.ValueKind != JsonValueKind.Object)
                        continue;

                    thuTu++;

                    var soMe = TryGetDecimal(row, "soMe");
                    var meGio = TryGetString(row, "meGio");
                    var thoiGianNapLieu = TryGetString(row, "thoiGianNapLieu");
                    var cheDo = TryGetString(row, "cheDoNapLieu");
                    var thuocThamLieu1 = TryGetDecimal(row, "thuocThamLieu1");
                    var thuocThamLieu2 = TryGetDecimal(row, "thuocThamLieu2");
                    var ghiChu = TryGetString(row, "ghiChu");

                    // Unpivot: mỗi NVL (key là chuỗi số nguyên = IDNVL) → 1 record
                    foreach (var prop in row.EnumerateObject())
                    {
                        if (fixedKeys.Contains(prop.Name)) continue;
                        if (!int.TryParse(prop.Name, out var idNvl)) continue;
                        if (prop.Value.ValueKind == JsonValueKind.Null) continue;
                        if (!prop.Value.TryGetDecimal(out var giaTri)) continue;

                        // Manual tracking: _manual_{idNvl} và _goc_{idNvl} trong cùng row
                        var isManual = row.TryGetProperty($"_manual_{idNvl}", out var manualEl)
                                       && manualEl.ValueKind == JsonValueKind.True;
                        decimal? giaTri_Goc = null;
                        if (row.TryGetProperty($"_goc_{idNvl}", out var gocEl)
                            && gocEl.ValueKind != JsonValueKind.Null
                            && gocEl.TryGetDecimal(out var gocVal))
                            giaTri_Goc = gocVal;

                        var doAm = doAmByNvl.TryGetValue(idNvl, out var da) ? da : (decimal?)null;

                        items.Add(new LG_NL_ChiTiet
                        {
                            IDPhieu = phieu.Idphieu,
                            IDLoCao = idLoCao.Value,
                            Ngay = ngay,
                            IDCa = idCa.Value,
                            ThoiGianNapLieu = thoiGianNapLieu,
                            SoMe = soMe,
                            MeGio = meGio,
                            CheDo = cheDo,
                            ThuocThamLieu1 = thuocThamLieu1,
                            ThuocThamLieu2 = thuocThamLieu2,
                            GhiChu = ghiChu,
                            IDNVL = idNvl,
                            GiaTri = giaTri,
                            ThuTu = thuTu,
                            NgayTao = DateTime.Now,
                            ManualGiaTri = isManual,
                            GiaTri_Goc = giaTri_Goc,
                            DoAm = doAm,
                            // QuyKho sẽ được tính sau khi có đủ tất cả rows
                        });
                    }
                }

                items = items
                    .GroupBy(x => new { x.IDPhieu, x.ThuTu, x.IDNVL })
                    .Select(g => g.First())
                    .ToList();

                // Tính QuyKho server-side: sum(GiaTri) per IDNVL * (1 - DoAm/100)
                var totalByNvl = items
                    .GroupBy(x => x.IDNVL)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.GiaTri ?? 0));

                foreach (var item in items)
                {
                    if (doAmByNvl.TryGetValue(item.IDNVL, out var pct) && totalByNvl.TryGetValue(item.IDNVL, out var total))
                        item.QuyKho = total * (100 - pct) / 100;
                }

                // Replace mode: xóa dữ liệu cũ rồi ghi mới
                await _repo.DeleteChiTietByPhieuIdAsync(phieu.Idphieu);

                if (items.Count > 0)
                    await _repo.AddChiTietRangeAsync(items);

                return items.Count;
            }

            catch (Exception ex)
            {
                // TODO: log error
                Console.WriteLine(ex);
                return 0;
            }
        }

        // ─── Export PDF ──────────────────────────────────────────────────────────

        public async Task<ExportFileResult> ExportNapLieuPdfAsync(Guid idPhieu, List<PheDuyetDto> pheDuyets)
        {
            var phieu = await _repo.GetPhieuByIdAsync(idPhieu)
                ?? throw new Exception("Không tìm thấy phiếu.");

            if (string.IsNullOrWhiteSpace(phieu.DataJson))
                throw new Exception("Phiếu không có dữ liệu JSON.");

            using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
            var root = jsonDoc.RootElement;

            var ngayStr  = TryGetString(root, "NgaySX", "ngaySX", "ngay");
            var ca       = TryGetInt(root, "ca", "Ca") ?? 0;
            var scope    = TryGetInt(root, "scope", "Scope", "idLoCao") ?? 0;

            DateTime.TryParse(ngayStr, out var ngay);
            var ngayDisplay = ngay != DateTime.MinValue ? ngay.ToString("dd/MM/yyyy") : "";
            var caLabel     = ca == 1 ? "1" : ca == 2 ? "2" : $"{ca}";
            var loCao       = scope > 0 ? scope.ToString() : "";

            // Lấy chi tiết và NVL
            var chiTiet = await _repo.GetChiTietByPhieuAsync(idPhieu);
            var nvlList = await _repo.GetNvlListAsync(scope > 0 ? scope : null);
            var nvlById = nvlList.ToDictionary(n => n.ID);

            // Tập hợp IDNVLs từ chi tiết
            var usedNvlIds = chiTiet.Select(x => x.IDNVL).Distinct().Order().ToList();
            var usedNvls   = usedNvlIds
                .Select(id => nvlById.TryGetValue(id, out var n) ? n : null)
                .OfType<LGNLNvlDto>()
                .OrderBy(n => n.ThuTuNhom ?? 999)
                .ThenBy(n => n.ThuTu ?? 999)
                .ToList();

            // Nhóm NVL theo NhomHienThi để build header 2 tầng
            var nhomGroups = usedNvls
                .GroupBy(n => n.NhomHienThi ?? (n.XacNhan == true && !string.IsNullOrWhiteSpace(n.TenNVL_TK) ? n.TenNVL_TK : n.TenNVL_NM) ?? "")
                .OrderBy(g => usedNvls.First(n => (n.NhomHienThi ?? (n.XacNhan == true && !string.IsNullOrWhiteSpace(n.TenNVL_TK) ? n.TenNVL_TK : n.TenNVL_NM) ?? "") == g.Key).ThuTuNhom ?? 999)
                .ToList();

            // Build thead
            var th1 = new StringBuilder();
            var th2 = new StringBuilder();
            // Fixed prefix cols header row 1 (rowspan=2)
            th1.Append(@"<th rowspan=""2"" style=""width:45px"">Số mê</th>");
            th1.Append(@"<th rowspan=""2"" style=""width:55px"">Mê/giờ</th>");
            th1.Append(@"<th rowspan=""2"" style=""width:80px"">Thời gian nạp liệu</th>");
            th1.Append(@"<th rowspan=""2"" style=""width:75px"">Chế độ nạp liệu</th>");
            th1.Append(@"<th rowspan=""2"" style=""width:55px"">Thuốc thăm liệu 1 (m)</th>");
            th1.Append(@"<th rowspan=""2"" style=""width:55px"">Thuốc thăm liệu 2 (m)</th>");

            foreach (var grp in nhomGroups)
            {
                var nvlsInGrp = grp.ToList();
                if (nvlsInGrp.Count == 1)
                {
                    var n0 = nvlsInGrp[0];
                    var label = (n0.XacNhan == true && !string.IsNullOrWhiteSpace(n0.TenNVL_TK) ? n0.TenNVL_TK : n0.TenNVL_NM) ?? grp.Key;
                    th1.Append($@"<th rowspan=""2"">{System.Net.WebUtility.HtmlEncode(label)}</th>");
                }
                else
                {
                    th1.Append($@"<th colspan=""{nvlsInGrp.Count}"">{System.Net.WebUtility.HtmlEncode(grp.Key)}</th>");
                    foreach (var n in nvlsInGrp)
                    {
                        var label = n.XacNhan == true && !string.IsNullOrWhiteSpace(n.TenNVL_TK) ? n.TenNVL_TK : n.TenNVL_NM;
                        th2.Append($@"<th>{System.Net.WebUtility.HtmlEncode(label ?? "")}</th>");
                    }
                }
            }

            th1.Append(@"<th rowspan=""2"" style=""width:80px"">Ghi chú</th>");

            // Build data rows grouped by ThuTu
            var rowsByThuTu = chiTiet
                .GroupBy(x => x.ThuTu ?? 0)
                .OrderBy(g => g.Key)
                .ToList();

            var dataRows = new StringBuilder();
            foreach (var grp in rowsByThuTu)
            {
                var sample = grp.First();
                var nvlValues = grp.ToDictionary(x => x.IDNVL, x => x.GiaTri);

                dataRows.Append("<tr>");
                dataRows.Append($"<td class=\"text-center\">{sample.SoMe?.ToString("N0") ?? ""}</td>");
                dataRows.Append($"<td class=\"text-center\">{System.Net.WebUtility.HtmlEncode(sample.MeGio ?? "")}</td>");
                dataRows.Append($"<td class=\"text-center\">{System.Net.WebUtility.HtmlEncode(sample.ThoiGianNapLieu ?? "")}</td>");
                dataRows.Append($"<td class=\"text-center\">{System.Net.WebUtility.HtmlEncode(sample.CheDo ?? "")}</td>");
                dataRows.Append($"<td class=\"text-right\">{(sample.ThuocThamLieu1.HasValue ? sample.ThuocThamLieu1.Value.ToString("N2") : "")}</td>");
                dataRows.Append($"<td class=\"text-right\">{(sample.ThuocThamLieu2.HasValue ? sample.ThuocThamLieu2.Value.ToString("N2") : "")}</td>");

                foreach (var n in usedNvls)
                {
                    var val = nvlValues.TryGetValue(n.ID, out var v) ? v : null;
                    dataRows.Append($"<td class=\"text-right\">{(val.HasValue ? val.Value.ToString("N0") : "")}</td>");
                }

                dataRows.Append($"<td>{System.Net.WebUtility.HtmlEncode(sample.GhiChu ?? "")}</td>");
                dataRows.Append("</tr>");
            }

            // Tổng cộng row
            var tongRow = new StringBuilder();
            tongRow.Append(@"<tr class=""tfoot-row""><td colspan=""6"" class=""text-center""><b>TỔNG CỘNG</b></td>");
            foreach (var n in usedNvls)
            {
                var total = chiTiet.Where(x => x.IDNVL == n.ID).Sum(x => x.GiaTri ?? 0);
                tongRow.Append($"<td class=\"text-right\"><b>{total.ToString("N0")}</b></td>");
            }
            tongRow.Append("<td></td></tr>");

            // Độ ẩm row
            var doAmRow = new StringBuilder();
            doAmRow.Append(@"<tr class=""doam-row""><td colspan=""6"" class=""text-center"">Độ ẩm (%)</td>");
            foreach (var n in usedNvls)
            {
                var da = chiTiet.FirstOrDefault(x => x.IDNVL == n.ID && x.DoAm.HasValue)?.DoAm;
                doAmRow.Append($"<td class=\"text-center\">{(da.HasValue ? da.Value.ToString("N2") : "")}</td>");
            }
            doAmRow.Append("<td></td></tr>");

            // Quy khô row
            var quyKhoRow = new StringBuilder();
            quyKhoRow.Append(@"<tr class=""quykho-row""><td colspan=""6"" class=""text-center"">Quy khô</td>");
            foreach (var n in usedNvls)
            {
                var qk = chiTiet.FirstOrDefault(x => x.IDNVL == n.ID && x.QuyKho.HasValue)?.QuyKho;
                quyKhoRow.Append($"<td class=\"text-right\">{(qk.HasValue ? qk.Value.ToString("N0") : "")}</td>");
            }
            quyKhoRow.Append("<td></td></tr>");

            // Signatures
            var nguoiTheoDoi = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);

            var logoUrl    = _configuration.GetValue<string>("AppSettings:LogoUrl")
                             ?? "https://report.hoaphatdungquat.vn/img/logoHP.png";
            var logoBase64 = await ConvertImageUrlToBase64Async(logoUrl);
            var signTheoDoi = await FormatChuKyBase64Async(nguoiTheoDoi?.ChuKy, nguoiTheoDoi?.TinhTrang == 1);

            var templatePath = Path.Combine(
                _env.WebRootPath,
                "template_html",
                "BM.05-QT.05.09_So_theo_doi_nap_lieu_lo_cao.html");

            var html = await File.ReadAllTextAsync(templatePath);
            html = html
                .Replace("{{LogoUrl}}", logoBase64)
                .Replace("{{LoCao}}", loCao)
                .Replace("{{CaLabel}}", caLabel)
                .Replace("{{NgaySX}}", ngayDisplay)
                .Replace("{{TheadRow1}}", th1.ToString())
                .Replace("{{TheadRow2}}", th2.ToString())
                .Replace("{{DataRows}}", dataRows.ToString())
                .Replace("{{TongRow}}", tongRow.ToString())
                .Replace("{{DoAmRow}}", doAmRow.ToString())
                .Replace("{{QuyKhoRow}}", quyKhoRow.ToString())
                .Replace("{{Sign_NguoiTheoDoi}}", signTheoDoi)
                .Replace("{{Ten_NguoiTheoDoi}}", nguoiTheoDoi?.HoVaTen ?? "");

            var doc = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    PaperSize   = PaperKind.A4,
                    Orientation = Orientation.Landscape,
                },
                Objects =
                {
                    new ObjectSettings
                    {
                        HtmlContent = html,
                        WebSettings =
                        {
                            DefaultEncoding  = "utf-8",
                            LoadImages       = true,
                            EnableJavascript = false,
                            PrintMediaType   = true,
                        },
                        LoadSettings =
                        {
                            BlockLocalFileAccess = false,
                            LoadErrorHandling    = ContentErrorHandling.Ignore,
                        }
                    }
                }
            };

            var pdfBytes = _pdfConverter.Convert(doc);
            var fileName = $"NapLieuLoCao_{phieu.SoPhieu ?? idPhieu.ToString("N")}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

            return new ExportFileResult
            {
                Content     = pdfBytes,
                FileName    = fileName,
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
                var ext   = Path.GetExtension(imageUrl).TrimStart('.').ToLower();
                var mime  = ext == "png" ? "image/png" : "image/jpeg";
                return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
            }
            catch
            {
                return imageUrl;
            }
        }

        private async Task<string> FormatChuKyBase64Async(string? chuKy, bool daKy = false)
        {
            if (string.IsNullOrWhiteSpace(chuKy))
            {
                if (daKy)
                    return @"<div style='text-align:center'>
                        <div style='font-style:italic;color:red'>Đã ký</div>
                        <div style='font-size:11px;color:red'>(Chưa cập nhật chữ ký)</div>
                    </div>";
                return "";
            }

            if (chuKy.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                return $"<img src=\"{chuKy}\" style=\"max-width:150px;max-height:80px;\" />";

            if (chuKy.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var base64 = await ConvertImageUrlToBase64Async(chuKy);
                if (!string.IsNullOrEmpty(base64))
                    return $"<img src=\"{base64}\" style=\"max-width:150px;max-height:80px;\" />";
            }
            else if (chuKy.StartsWith("/"))
            {
                var domain  = _configuration.GetValue<string>("AppSettings:Domain") ?? "https://report.hoaphatdungquat.vn";
                var fullUrl = domain.TrimEnd('/') + chuKy;
                var base64  = await ConvertImageUrlToBase64Async(fullUrl);
                if (!string.IsNullOrEmpty(base64))
                    return $"<img src=\"{base64}\" style=\"max-width:150px;max-height:80px;\" />";
            }

            return @"<div style='text-align:center'>
                <div style='font-style:italic;color:red'>Đã ký</div>
                <div style='font-size:11px;color:red'>(Chưa cập nhật chữ ký)</div>
            </div>";
        }

        // ─── JSON helpers ─────────────────────────────────────────────────────────

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

        private static decimal? TryGetDecimal(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!obj.TryGetProperty(key, out var val) || val.ValueKind == JsonValueKind.Null)
                    continue;

                if (val.ValueKind == JsonValueKind.Number && val.TryGetDecimal(out var d))
                    return d;

                if (val.ValueKind == JsonValueKind.String
                    && decimal.TryParse(val.GetString(), out var s))
                    return s;
            }
            return null;
        }
    }
}
