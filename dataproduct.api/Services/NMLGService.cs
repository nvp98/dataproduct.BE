using System.Text.Json;
using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;


namespace dataproduct.api.Services
{
    public class NMLGService
    {
        private readonly INMLGRepository _repo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IColumnMappingRepository _colRepo;
        private readonly IColumnMappingNhomRepository _nhomRepo;
        private const string NapLieuBaseUrl = "http://10.192.72.50:5160/api/NapLieu_BM05_QT05_09";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public NMLGService(INMLGRepository repo,  
            IHttpClientFactory httpClientFactory,
            IColumnMappingRepository colRepo, IColumnMappingNhomRepository nhomRepo)
        {
            _repo = repo;
            _httpClientFactory = httpClientFactory;
            _colRepo = colRepo;
            _nhomRepo = nhomRepo;
        }

        public async Task<List<SiLoTonDto>> GetSiLoTonAsync(int? idLoCao, int? idCa, DateTime? ngay)
        {
            return await _repo.GetSiLoTon(idLoCao, idCa, ngay);
        }
        public async Task<List<SiLo_LG>> GetSiLoWithLoCaAsync(int? idLoCao)
        {
            return await _repo.GetSiLoWithLoCaoAsync(idLoCao);
        }

        public async Task<SiLo_LG> AddSiLoAsync(SiLo_LG entity)
        {
            return await _repo.AddSiLoAsync(entity);
        }

        public async Task<SiLo_LG?> UpdateSiLoAsync(int id, SiLo_LG entity)
        {
            return await _repo.UpdateSiLoAsync(id, entity);
        }

        /// <summary>
        /// Ca 1: 07:30 → 19:30 cùng ngày
        /// Ca 2: 19:30 → 07:30 ngày hôm sau
        /// </summary>
        public static (DateTime begind, DateTime endd) GetCaRange(DateTime ngay, int ca)
        {
            var date = ngay.Date;
            return ca == 1
                ? (date.AddHours(7).AddMinutes(30), date.AddHours(19).AddMinutes(30))
                : (date.AddHours(19).AddMinutes(30), date.AddDays(1).AddHours(7).AddMinutes(30));
        }

        public async Task<List<NapLieuItemDto>> GetNapLieuAsync(int loCao, DateTime begind, DateTime endd)
        {
            var url = $"{NapLieuBaseUrl}?type={loCao}" +
                      $"&begind={Uri.EscapeDataString(begind.ToString("yyyy-MM-dd HH:mm:ss"))}" +
                      $"&endd={Uri.EscapeDataString(endd.ToString("yyyy-MM-dd HH:mm:ss"))}";

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<NapLieuItemDto>>(json, _jsonOptions) ?? [];
        }

        public async Task<List<Dictionary<string, object?>>> GetNapLieuPivotAsync(int loCao, DateTime begind, DateTime endd)
        {
            var raw = await GetNapLieuAsync(loCao, begind, endd);

            return raw
                .GroupBy(x => new DateTime(x.Time.Year, x.Time.Month, x.Time.Day, x.Time.Hour, x.Time.Minute, 0))
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var row = new Dictionary<string, object?> { { "time", g.Key } };
                    foreach (var item in g)
                        row[item.TagNameShort ?? item.TagName ?? ""] = item.Value;
                    return row;
                })
                .ToList();
        }


        public async Task<NapLieuMappedResult> GetNapLieuMappedAsync(int loCao, DateTime begind, DateTime endd)
        {
            // Lấy song song: raw data + toàn bộ nhóm (kèm cột con visible)
            var rawTask = GetNapLieuAsync(loCao, begind, endd);
            var nhomsTask = _nhomRepo.GetAllWithColumnsAsync(loCao);
            await Task.WhenAll(rawTask, nhomsTask);

            var raw = rawTask.Result;
            var nhoms = nhomsTask.Result; // đã lọc IsVisible, order ThuTu, include Columns

            // Build mapping: sourceField → dataIndex (cả leaf nhóm lẫn cột con)
            var mapping = new Dictionary<string, string>();
            foreach (var nhom in nhoms)
            {
                if (nhom.Columns.Count == 0)
                {
                    // Nhóm leaf
                    if (!string.IsNullOrEmpty(nhom.SourceField) && !string.IsNullOrEmpty(nhom.DataIndex))
                        mapping[nhom.SourceField] = nhom.DataIndex;
                }
                else
                {
                    // Cột con
                    foreach (var c in nhom.Columns.Where(c => !string.IsNullOrEmpty(c.SourceField)))
                        mapping[c.SourceField!] = c.DataIndex!;
                }
            }

            // Build columns cho Ant Design Table
            var columns = nhoms.Select(nhom =>
            {
                if (nhom.Columns.Count == 0 && !string.IsNullOrEmpty(nhom.DataIndex))
                    return new ColumnDto
                    {
                        title = nhom.TenNhom,
                        dataIndex = nhom.DataIndex,
                        format = nhom.Format
                    };

                if (nhom.Columns.Count > 0)
                    return new ColumnDto
                    {
                        title = nhom.TenNhom,
                        children = [.. nhom.Columns.Select(c => new ColumnChildDto
                        {
                            title = c.TenCot ?? "",
                            dataIndex = c.DataIndex ?? "",
                            format = c.Format
                        })]
                    };

                return null;
            })
            .Where(x => x != null)
            .ToList()!;

            // Build rows: pivot theo time, dùng DataIndex làm key
            var rows = raw
                .GroupBy(x => new DateTime(x.Time.Year, x.Time.Month, x.Time.Day, x.Time.Hour, x.Time.Minute, 0))
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var row = new Dictionary<string, object?> { { "time", g.Key } };
                    foreach (var item in g)
                    {
                        var src = item.TagNameShort ?? item.TagName ?? "";
                        if (mapping.TryGetValue(src, out var dataIndex))
                            row[dataIndex] = item.Value;
                    }
                    return row;
                })
                .ToList();

            return new NapLieuMappedResult { Columns = columns, Rows = rows };
        }

        public async Task<List<NMLG_QuyKhoNapLieu>> GetQuyKhoAsync(DateOnly ngay, int idCa, int idLoCao)
        {
            return await _repo.GetAsync(ngay, idCa, idLoCao);
        }

        public async Task UpsertQuyKhoAsync(UpsertQuyKhoRequest request)
        {
            var items = request.Items.Select(item => new NMLG_QuyKhoNapLieu
            {
                Ngay = request.Ngay,
                IDCa = request.IDCa,
                IDLoCao = request.IDLoCao,
                DataIndex = item.DataIndex,
                TenNVL = item.TenNVL,
                TongCong = item.TongCong,
                DoAm = item.DoAm,
                QuyKho = (item.TongCong.HasValue && item.DoAm.HasValue)
                    ? item.TongCong.Value * (100 - item.DoAm.Value) / 100
                    : null,
                NgayTao = DateTime.Now
            }).ToList();

            await _repo.UpsertAsync(items);
        }
    }
}
