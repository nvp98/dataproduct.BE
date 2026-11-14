using dataproduct.api.Services.Kdl;

namespace dataproduct.api.Services.Background
{
    /// <summary>
    /// Tự động đẩy số về Kho dữ liệu động cho mọi module có nguồn đăng ký (IKdlNguon), mỗi Kdl:SyncIntervalMinutes phút,
    /// khoảng (hôm nay − Kdl:SyncSoNgayLui) → hôm nay (BKM hay được sửa lùi vài ngày).
    /// Kdl:TatDongBo = true để tắt. Đăng ký thủ công trong Program.cs (Scrutor không quét hậu tố Worker).
    /// </summary>
    public class KdlSyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KdlSyncWorker> _logger;

        public KdlSyncWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<KdlSyncWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Chờ app khởi động xong trước lần chạy đầu
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var section = _configuration.GetSection("Kdl");
                var chuKy = section.GetValue("SyncIntervalMinutes", 30);

                if (!section.GetValue("TatDongBo", false))
                {
                    var soNgayLui = Math.Clamp(section.GetValue("SyncSoNgayLui", 3), 0, 30);
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var sync = scope.ServiceProvider.GetRequiredService<KdlSyncService>();
                        var homNay = DateOnly.FromDateTime(DateTime.Now);
                        foreach (var module in sync.DanhSachModule())
                        {
                            try
                            {
                                var log = await sync.SyncAsync(module, homNay.AddDays(-soNgayLui), homNay, KdlConst.NguonChayTuDong, stoppingToken);
                                _logger.LogInformation("Đồng bộ KDL {Module} tự động: {KetQua}, {SoDong} dòng", module, log.ThanhCong ? "thành công" : "lỗi", log.SoDong);
                            }
                            catch (InvalidOperationException ex)
                            {
                                // Đang có lần đồng bộ tay chạy → bỏ qua lượt này
                                _logger.LogInformation("Bỏ qua lượt đồng bộ KDL {Module}: {Message}", module, ex.Message);
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Worker đồng bộ KDL lỗi");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, chuKy)), stoppingToken);
            }
        }
    }
}
