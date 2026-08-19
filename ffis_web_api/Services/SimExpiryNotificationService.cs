namespace ffis_web_api.Services
{
    /// <summary>
    /// Penjadwal tipis: jalankan <see cref="SimExpiryJob"/> sekali setelah startup, lalu tiap hari
    /// jam <see cref="SimExpiryJob.RunAtHour"/> (waktu server). Semua logika ada di SimExpiryJob
    /// agar bisa juga dipicu manual lewat endpoint admin.
    /// </summary>
    public class SimExpiryNotificationService : BackgroundService
    {
        private readonly SimExpiryJob _job;
        private readonly ILogger<SimExpiryNotificationService> _logger;

        public SimExpiryNotificationService(SimExpiryJob job, ILogger<SimExpiryNotificationService> logger)
        {
            _job = job;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Jeda singkat setelah startup lalu jalankan sekali (dedup mencegah dobel).
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) { return; }

            try { await _job.RunAsync("startup", stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "[SimExpiry] Run startup gagal."); }

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = SimExpiryJob.GetNextScheduledRun() - DateTime.Now;
                if (delay < TimeSpan.Zero) delay = TimeSpan.FromMinutes(1);

                try { await Task.Delay(delay, stoppingToken); }
                catch (OperationCanceledException) { break; }

                try { await _job.RunAsync("scheduled", stoppingToken); }
                catch (Exception ex) { _logger.LogError(ex, "[SimExpiry] Run terjadwal gagal."); }
            }
        }
    }
}
