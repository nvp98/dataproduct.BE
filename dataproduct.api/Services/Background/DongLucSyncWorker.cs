namespace dataproduct.api.Services.Background
{
    /// <summary>
    /// Tự động kéo dữ liệu Động lực từ EMS mỗi EmsApi:SyncIntervalMinutes phút, khoảng
    /// (hôm nay − EmsApi:SyncSoNgayLui) → hôm nay. Bỏ qua khi chưa cấu hình EmsApi:BaseUrl.
    /// Đăng ký thủ công trong Program.cs (Scrutor không quét hậu tố Worker).
    /// </summary>
    public class DongLucSyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DongLucSyncWorker> _logger;

        public DongLucSyncWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<DongLucSyncWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Chờ app khởi động xong trước lần chạy đầu
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var opts = _configuration.GetSection("EmsApi").Get<EmsApiOptions>() ?? new EmsApiOptions();

                if (opts.DaCauHinh)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var sync = scope.ServiceProvider.GetRequiredService<DongLucSyncService>();
                        var homNay = DateOnly.FromDateTime(DateTime.Now);
                        var log = await sync.SyncAsync(homNay.AddDays(-Math.Max(0, opts.SyncSoNgayLui)), homNay, null, DongLucSyncService.NguonTuDong, stoppingToken);
                        _logger.LogInformation("Đồng bộ Động lực tự động: {KetQua}, {SoDong} dòng", log.ThanhCong ? "thành công" : "lỗi", log.SoDong);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Đang có lần đồng bộ tay chạy → bỏ qua lượt này
                        _logger.LogInformation("Bỏ qua lượt đồng bộ Động lực tự động: {Message}", ex.Message);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Worker đồng bộ Động lực lỗi");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, opts.SyncIntervalMinutes)), stoppingToken);
            }
        }
    }
}
