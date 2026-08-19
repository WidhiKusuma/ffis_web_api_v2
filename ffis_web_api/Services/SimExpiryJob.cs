using System.Text.Json;
using ffis_web_api.Repositories;

namespace ffis_web_api.Services
{
    /// <summary>Ringkasan status eksekusi job pengingat SIM (dibaca admin lewat endpoint status).</summary>
    public class SimExpiryStatus
    {
        public bool IsRunning { get; set; }
        public DateTime? LastRunStart { get; set; }
        public DateTime? LastRunEnd { get; set; }
        public double? LastDurationSeconds { get; set; }
        public int LastSentCount { get; set; }
        public int LastDriverCount { get; set; }
        public string? LastTrigger { get; set; }
        public string? LastError { get; set; }
        public int TotalRuns { get; set; }
        public DateTime? NextScheduledRun { get; set; }
        public string ServerTime { get; set; } = "";
    }

    /// <summary>
    /// Logika pengecekan + pengiriman notifikasi pengingat SIM. Singleton agar status bisa
    /// dibaca controller, dan bisa dipicu manual (endpoint admin) maupun terjadwal (BackgroundService).
    /// </summary>
    public class SimExpiryJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SimExpiryJob> _logger;

        private static readonly int[] Milestones = { 60, 30, 15, 7, 0 };
        public const int RunAtHour = 7;

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly SimExpiryStatus _status = new();

        public SimExpiryJob(
            IServiceScopeFactory scopeFactory,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SimExpiryJob> logger)
        {
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public SimExpiryStatus GetStatus()
        {
            // Kembalikan salinan agar tidak dimutasi dari luar.
            return new SimExpiryStatus
            {
                IsRunning = _status.IsRunning,
                LastRunStart = _status.LastRunStart,
                LastRunEnd = _status.LastRunEnd,
                LastDurationSeconds = _status.LastDurationSeconds,
                LastSentCount = _status.LastSentCount,
                LastDriverCount = _status.LastDriverCount,
                LastTrigger = _status.LastTrigger,
                LastError = _status.LastError,
                TotalRuns = _status.TotalRuns,
                NextScheduledRun = GetNextScheduledRun(),
                ServerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        public static DateTime GetNextScheduledRun()
        {
            var now = DateTime.Now;
            var next = now.Date.AddHours(RunAtHour);
            if (next <= now) next = next.AddDays(1);
            return next;
        }

        /// <summary>Jalankan pengecekan. trigger = "scheduled" | "manual" | "startup".</summary>
        public async Task<SimExpiryStatus> RunAsync(string trigger, CancellationToken ct = default)
        {
            // Cegah eksekusi tumpang tindih (mis. manual saat terjadwal sedang jalan).
            if (!await _gate.WaitAsync(0, ct))
            {
                _logger.LogInformation("[SimExpiry] Trigger '{Trigger}' diabaikan: job sedang berjalan.", trigger);
                return GetStatus();
            }

            var start = DateTime.Now;
            _status.IsRunning = true;
            _status.LastTrigger = trigger;
            _status.LastRunStart = start;
            _status.LastError = null;
            int sent = 0, driverCount = 0;

            try
            {
                (sent, driverCount) = await RunCheckAsync(ct);
                _status.LastSentCount = sent;
                _status.LastDriverCount = driverCount;
            }
            catch (Exception ex)
            {
                _status.LastError = ex.Message;
                _logger.LogError(ex, "[SimExpiry] Gagal menjalankan pengecekan SIM ({Trigger}).", trigger);
            }
            finally
            {
                var end = DateTime.Now;
                _status.LastRunEnd = end;
                _status.LastDurationSeconds = Math.Round((end - start).TotalSeconds, 1);
                _status.TotalRuns++;
                _status.IsRunning = false;
                _gate.Release();
            }

            _logger.LogInformation("[SimExpiry] Selesai ({Trigger}). {Sent} notif dari {Count} driver.", trigger, sent, driverCount);
            return GetStatus();
        }

        private async Task<(int sent, int driverCount)> RunCheckAsync(CancellationToken ct)
        {
            var baseUrl = _configuration["ExternalApi:BaseUrl"];
            var apiKey = _configuration["ExternalApi:ApiKey"];
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("ExternalApi:BaseUrl / ApiKey belum dikonfigurasi.");
            }

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<DriverRepository>();

            var drivers = repo.GetAllUsers()
                .Where(u => u.IsActive
                            && string.Equals(u.LevelUser, "Driver", StringComparison.OrdinalIgnoreCase)
                            && !string.IsNullOrWhiteSpace(u.DriverCode))
                .ToList();

            if (drivers.Count == 0) return (0, 0);

            var client = _httpClientFactory.CreateClient("YLIDClient");
            var semaphore = new SemaphoreSlim(5);
            int sent = 0;

            var tasks = drivers.Select(async driver =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var expiries = await GetSimExpiriesAsync(client, baseUrl, apiKey, driver.DriverCode, ct);
                    foreach (var expDate in expiries)
                    {
                        var days = (int)(expDate.Date - DateTime.Today).TotalDays;
                        if (!Milestones.Contains(days)) continue;

                        var dedupKey = $"SIM_EXP_{driver.DriverCode}_{expDate:yyyyMMdd}_{days}";
                        if (repo.SimNotifExists(driver.DriverCode, dedupKey)) continue;

                        var title = "Pengingat Masa Berlaku SIM";
                        var body = days == 0
                            ? $"SIM Anda kadaluarsa HARI INI ({expDate:dd MMM yyyy}). Segera perpanjang."
                            : $"SIM Anda akan kadaluarsa dalam {days} hari lagi ({expDate:dd MMM yyyy}). Segera perpanjang.";

                        repo.SaveNotification(driver.DriverCode, title, body, null, dedupKey, "sim_expiry");
                        await SendPushAsync(repo, driver.DriverCode, title, body, ct);
                        Interlocked.Increment(ref sent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SimExpiry] Gagal memproses driver {DriverCode}.", driver.DriverCode);
                }
                finally { semaphore.Release(); }
            });

            await Task.WhenAll(tasks);
            return (sent, drivers.Count);
        }

        private async Task<List<DateTime>> GetSimExpiriesAsync(
            HttpClient client, string baseUrl, string apiKey, string driverCode, CancellationToken ct)
        {
            var result = new List<DateTime>();
            try
            {
                var url = $"{baseUrl}get-driver-licenses?nik={Uri.EscapeDataString(driverCode)}";
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Add("APIKEY", apiKey);
                var resp = await client.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode) return result;

                var json = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var arr = root.TryGetProperty("data", out var d) ? d :
                          root.ValueKind == JsonValueKind.Array ? root : default;
                if (arr.ValueKind != JsonValueKind.Array) return result;

                foreach (var item in arr.EnumerateArray())
                {
                    string? expiryStr = null;
                    if (item.TryGetProperty("expiryDate", out var ed) || item.TryGetProperty("ExpiryDate", out ed))
                        expiryStr = ed.GetString();

                    if (DateTime.TryParse(expiryStr, out var expDate))
                        result.Add(expDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SimExpiry] Gagal ambil lisensi driver {DriverCode}.", driverCode);
            }
            return result;
        }

        private async Task SendPushAsync(DriverRepository repo, string driverCode, string title, string body, CancellationToken ct)
        {
            try
            {
                var messaging = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;
                if (messaging == null) return;

                var token = repo.GetDriverToken(driverCode);
                if (string.IsNullOrEmpty(token)) return;

                var message = new FirebaseAdmin.Messaging.Message()
                {
                    Token = token,
                    Notification = new FirebaseAdmin.Messaging.Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Android = new FirebaseAdmin.Messaging.AndroidConfig()
                    {
                        Priority = FirebaseAdmin.Messaging.Priority.High,
                        Notification = new FirebaseAdmin.Messaging.AndroidNotification()
                        {
                            ChannelId = "p2h_notification_channel",
                            Priority = FirebaseAdmin.Messaging.NotificationPriority.HIGH,
                            DefaultSound = true,
                            DefaultVibrateTimings = true
                        }
                    },
                    Apns = new FirebaseAdmin.Messaging.ApnsConfig()
                    {
                        Headers = new Dictionary<string, string>() { { "apns-priority", "10" } },
                        Aps = new FirebaseAdmin.Messaging.Aps() { Sound = "default", Badge = 1 }
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "title", title },
                        { "body", body },
                        { "type", "sim_expiry" },
                        { "nik", driverCode }
                    }
                };

                await messaging.SendAsync(message, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SimExpiry] Gagal kirim FCM ke driver {DriverCode}.", driverCode);
            }
        }
    }
}
