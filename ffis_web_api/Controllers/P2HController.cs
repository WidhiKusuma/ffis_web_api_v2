using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ffis_web_api.Repositories;
using ffis_web_api.Models;
using System.Net.Http.Json;

namespace ffis_web_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class P2HController : Controller
    {
        private readonly DriverRepository _driverRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ffis_web_api.Services.SimExpiryJob _simExpiryJob;

        public P2HController(DriverRepository driverRepository, IHttpClientFactory httpClientFactory, IConfiguration configuration, ffis_web_api.Services.SimExpiryJob simExpiryJob)
        {
            _driverRepository = driverRepository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _simExpiryJob = simExpiryJob;
        }

        private string GetCurrentUserNIK() => User.FindFirst("nik")?.Value ?? "";
        private string GetCurrentUserName() => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
        private string GetCurrentUserRole() => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        private bool IsAdminOrGate()
        {
            var role = GetCurrentUserRole().ToUpper();
            var level = User.FindFirst("level")?.Value?.ToUpper() ?? "";
            
            return role.Contains("ADMIN") || role.Contains("GATE") || 
                   level.Contains("ADMIN") || level.Contains("GATE") || 
                   level == "3" || level == "2" || level == "1" || role == "1" ||
                   role == "ADMINISTRATOR" || level == "ADMINISTRATOR"; // 1 Administrator, 2 Admin, 3 Gate
        }

        [HttpGet("master-checklist")]
        public IActionResult GetMasterChecklist()
        {
            var items = _driverRepository.GetMasterChecklist();
            return Ok(items);
        }

        [HttpGet("last-odometer/{nopol}")]
        public IActionResult GetLastOdometer(string nopol)
        {
            var km = _driverRepository.GetLastOdometer(nopol);
            return Ok(new { odometer = km ?? 0 });
        }

        [HttpGet("validate-vehicle/{nopol}")]
        public async Task<IActionResult> ValidateVehicle(string nopol)
        {
            // PLACEHOLDER: Nanti masukkan URL API Eksternal di sini
            // Untuk sekarang kita simulasi sukses
            try
            {
                // Contoh call:
                // var client = _httpClientFactory.CreateClient("YLIDClient");
                // var response = await client.GetAsync($"https://external-api/validate-vehicle?nopol={nopol}");
                
                // Simulasi data dari API Eksternal
                var result = new
                {
                    IsSuccess = true,
                    Message = "Dokumen kendaraan lengkap",
                    Details = new
                    {
                        StnkExpiry = "2027-01-01",
                        KirExpiry = "2026-12-12",
                        PajakExpiry = "2026-10-10"
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error validating vehicle: {ex.Message}");
            }
        }

        [HttpGet("check-active/{vehicleNo}")]
        public IActionResult CheckActiveChecklist(string vehicleNo)
        {
            try
            {
                var vehicle = Uri.UnescapeDataString(vehicleNo).Trim().ToUpper();
                var active = _driverRepository.GetActiveChecklistByVehicle(vehicle);

                if (active != null)
                {
                    return Ok(new
                    {
                        IsActive = true,
                        TransactionNo = active.TransactionNo,
                        ExitTime = active.ExitTime,
                        DriverName = active.DriverName,
                        Status = active.Status,
                        ActivityDate = active.ActivityDate
                    });
                }

                return Ok(new { IsActive = false });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("get-vehicle-licenses")]
        public async Task<IActionResult> GetVehicleLicenses([FromQuery] string search)
        {
            try
            {
                var baseUrl = _configuration["ExternalApi:BaseUrl"];
                var apiKey = _configuration["ExternalApi:ApiKey"];
                var url = $"{baseUrl}get-vehicle-licenses?search={search}";

                var client = _httpClientFactory.CreateClient("YLIDClient");
                
                // Menggunakan HttpRequestMessage agar header APIKEY dipastikan terkirim dengan benar
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("APIKEY", apiKey);
                
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>();
                    return Ok(result);
                }

                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, error);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("update-vehicle-km")]
        public async Task<IActionResult> UpdateVehicleCurrentKM([FromQuery] string noPol, [FromQuery] int currentKM)
        {
            try
            {
                var baseUrl = _configuration["ExternalApi:BaseUrl"];
                var apiKey = _configuration["ExternalApi:ApiKey"];
                var url = $"{baseUrl}post-vehicle-licenses-currentkm?noPol={Uri.EscapeDataString(noPol)}&currentKM={currentKM}";

                var client = _httpClientFactory.CreateClient("YLIDClient");
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("APIKEY", apiKey);

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return Ok(new { success = true, message = "Odometer berhasil diperbarui.", data = result });
                }

                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { success = false, message = error });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("submit")]
        public IActionResult SubmitP2H([FromBody] P2HHeader header)
        {
            try
            {
                if (header == null || string.IsNullOrEmpty(header.VehicleNo))
                {
                    return BadRequest("Invalid checklist data.");
                }

                var openIssues = _driverRepository.GetOpenVehicleIssues(header.VehicleNo);
                if (openIssues.Any())
                {
                    foreach (var d in header.Details)
                    {
                        bool isComplaint = d.Category == "KELUHAN" || (d.QuestionText?.Contains("keluhan", StringComparison.OrdinalIgnoreCase) ?? false);
                        string okAnswer = isComplaint ? "TIDAK" : "YA";
                        if (d.Answer == okAnswer && openIssues.Any(i => i.ItemCode == d.ItemCode))
                        {
                            return BadRequest($"Tidak bisa menjawab {okAnswer} pada '{d.QuestionText}' — masih ada temuan yang belum diselesaikan untuk unit ini. Selesaikan dulu di Menu Utama.");
                        }
                    }
                }

                header.Status = header.NeedsApproval ? "Waiting Approval" : "Approved";
                header.IsAllowedToExit = !header.NeedsApproval; // Auto allow if no approval needed

                if (_driverRepository.SaveP2H(header))
                {
                    if (header.NeedsApproval)
                    {
                        // Notifikasi admin transport: ada temuan, perlu persetujuan manual
                        _ = Task.Run(() => NotifyAdminTransport(header));
                    }
                    else
                    {
                        // Notifikasi admin transport: checklist lengkap, disetujui otomatis
                        _ = Task.Run(() => NotifyAdminTransportAutoApproved(header));
                    }

                    return Ok(new { Message = "P2H Submitted Successfully", QrCode = header.QrCodeData, TransactionNo = header.TransactionNo });
                }

                return StatusCode(500, "Gagal menyimpan checklist P2H ke database.");
            }
            catch (Exception ex)
            {
                // Mengembalikan pesan error asli agar bisa di-debug dari mobile
                return StatusCode(500, $"Database Error: {ex.Message} | {ex.InnerException?.Message}");
            }
        }

        private async Task NotifyAdminTransport(P2HHeader header)
        {
            try
            {
                var adminTokens = _driverRepository.GetAdminTransportUsersAndTokens();
                if (adminTokens == null || !adminTokens.Any()) return;

                var messaging = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;

                foreach (var admin in adminTokens)
                {
                    try
                    {
                        // 1. Selalu simpan notifikasi ke database (untuk in-app inbox & kebutuhan audit)
                        _driverRepository.SaveNotification(
                            admin.DriverCode, 
                            "Persetujuan P2H Baru", 
                            $"Unit {header.VehicleNo} ({header.DriverName}) memerlukan persetujuan Anda.", 
                            null, 
                            header.TransactionNo, 
                            "approval_request"
                        );
                    }
                    catch (Exception dbEx)
                    {
                        Console.WriteLine($"[NotifyAdminTransport] Gagal menyimpan notifikasi ke DB untuk {admin.Username}: {dbEx.Message}");
                    }

                    // 2. Kirim push notification jika token FCM ada dan Firebase aktif
                    if (messaging != null && !string.IsNullOrEmpty(admin.FcmToken))
                    {
                        try
                        {
                            var message = new FirebaseAdmin.Messaging.Message()
                            {
                                Token = admin.FcmToken,
                                Notification = new FirebaseAdmin.Messaging.Notification()
                                {
                                    Title = "Persetujuan P2H Baru",
                                    Body = $"Unit {header.VehicleNo} ({header.DriverName}) memerlukan persetujuan Anda."
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
                                    Headers = new Dictionary<string, string>()
                                    {
                                        { "apns-priority", "10" }
                                    },
                                    Aps = new FirebaseAdmin.Messaging.Aps()
                                    {
                                        Sound = "default",
                                        Badge = 1
                                    }
                                },
                                Data = new Dictionary<string, string>()
                                {
                                    { "title", "Persetujuan P2H Baru" },
                                    { "body", $"Unit {header.VehicleNo} ({header.DriverName}) memerlukan persetujuan Anda." },
                                    { "type", "approval_request" },
                                    { "transaction_no", header.TransactionNo ?? "" },
                                    { "vehicle_no", header.VehicleNo ?? "" },
                                    { "nik", header.DriverCode ?? "" }
                                }
                            };

                            await messaging.SendAsync(message);
                        }
                        catch (Exception fcmEx)
                        {
                            Console.WriteLine($"[FCM_ERROR] Gagal mengirim push ke {admin.Username}: {fcmEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FCM_ERROR_OUTER] {ex.Message}");
            }
        }

        private async Task NotifyAdminTransportAutoApproved(P2HHeader header)
        {
            try
            {
                var adminTokens = _driverRepository.GetAdminTransportUsersAndTokens();
                if (adminTokens == null || !adminTokens.Any()) return;

                var messaging = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;
                string title = "P2H Disetujui Otomatis";
                string body = $"Unit {header.VehicleNo} ({header.DriverName}) checklist lengkap, tidak ada temuan. Disetujui otomatis.";

                foreach (var admin in adminTokens)
                {
                    try
                    {
                        _driverRepository.SaveNotification(
                            admin.DriverCode,
                            title,
                            body,
                            null,
                            header.TransactionNo,
                            "auto_approved"
                        );
                    }
                    catch (Exception dbEx)
                    {
                        Console.WriteLine($"[NotifyAdminAutoApproved] Gagal menyimpan notifikasi ke DB untuk {admin.Username}: {dbEx.Message}");
                    }

                    if (messaging != null && !string.IsNullOrEmpty(admin.FcmToken))
                    {
                        try
                        {
                            var message = new FirebaseAdmin.Messaging.Message()
                            {
                                Token = admin.FcmToken,
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
                                    Headers = new Dictionary<string, string>()
                                    {
                                        { "apns-priority", "10" }
                                    },
                                    Aps = new FirebaseAdmin.Messaging.Aps()
                                    {
                                        Sound = "default",
                                        Badge = 1
                                    }
                                },
                                Data = new Dictionary<string, string>()
                                {
                                    { "title", title },
                                    { "body", body },
                                    { "type", "auto_approved" },
                                    { "transaction_no", header.TransactionNo ?? "" },
                                    { "vehicle_no", header.VehicleNo ?? "" },
                                    { "nik", header.DriverCode ?? "" }
                                }
                            };

                            await messaging.SendAsync(message);
                        }
                        catch (Exception fcmEx)
                        {
                            Console.WriteLine($"[FCM_AUTO_APPROVED_ERROR] Gagal mengirim push ke {admin.Username}: {fcmEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FCM_AUTO_APPROVED_OUTER] {ex.Message}");
            }
        }

        [HttpPost("cancel")]
        public IActionResult CancelP2H([FromBody] CancelP2HRequest request)
        {
            if (request.Oid == Guid.Empty)
                return BadRequest("Oid tidak valid.");

            var p2h = _driverRepository.GetP2HWithDetails(request.Oid);
            if (p2h == null)
                return NotFound("Data P2H tidak ditemukan.");

            // Hanya driver pemilik atau admin yang bisa membatalkan
            if (!IsAdminOrGate() && p2h.DriverCode != GetCurrentUserNIK())
                return Forbid("Anda tidak memiliki akses untuk membatalkan checklist ini.");

            // Hanya bisa dibatalkan jika status Approved dan belum keluar gate
            if (p2h.Status != "Approved")
                return BadRequest($"Checklist tidak bisa dibatalkan karena status saat ini adalah '{p2h.Status}'.");

            if (p2h.ExitTime != null)
                return BadRequest("Checklist tidak bisa dibatalkan karena kendaraan sudah keluar gate.");

            string cancelBy = GetCurrentUserName();
            string reason = string.IsNullOrWhiteSpace(request.Reason) ? "Dibatalkan oleh driver" : request.Reason;

            if (_driverRepository.MarkAsCancelled(request.Oid, reason, cancelBy))
            {
                _ = Task.Run(() => NotifyAdminTransportCancelled(p2h, cancelBy, reason));
                return Ok(new { success = true, message = "Checklist berhasil dibatalkan." });
            }

            return StatusCode(500, "Gagal membatalkan checklist.");
        }

        private async Task NotifyAdminTransportCancelled(P2HHeader header, string cancelBy, string reason)
        {
            try
            {
                var adminTokens = _driverRepository.GetAdminTransportUsersAndTokens();
                if (adminTokens == null || !adminTokens.Any()) return;

                var messaging = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;
                string title = "Checklist Dibatalkan";
                string body = $"Unit {header.VehicleNo} ({header.DriverName}) membatalkan checklist yang telah disetujui. Alasan: {reason}";

                foreach (var admin in adminTokens)
                {
                    try
                    {
                        _driverRepository.SaveNotification(
                            admin.DriverCode,
                            title,
                            body,
                            null,
                            header.TransactionNo,
                            "cancellation_alert"
                        );
                    }
                    catch (Exception dbEx)
                    {
                        Console.WriteLine($"[NotifyAdminCancelled] DB error untuk {admin.Username}: {dbEx.Message}");
                    }

                    if (messaging != null && !string.IsNullOrEmpty(admin.FcmToken))
                    {
                        try
                        {
                            var message = new FirebaseAdmin.Messaging.Message()
                            {
                                Token = admin.FcmToken,
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
                                    Headers = new Dictionary<string, string> { { "apns-priority", "10" } },
                                    Aps = new FirebaseAdmin.Messaging.Aps() { Sound = "default", Badge = 1 }
                                },
                                Data = new Dictionary<string, string>()
                                {
                                    { "title", title },
                                    { "body", body },
                                    { "type", "cancellation_alert" },
                                    { "transaction_no", header.TransactionNo ?? "" },
                                    { "vehicle_no", header.VehicleNo ?? "" },
                                    { "nik", header.DriverCode ?? "" }
                                }
                            };
                            await messaging.SendAsync(message);
                        }
                        catch (Exception fcmEx)
                        {
                            Console.WriteLine($"[FCM_CANCEL_ADMIN_ERROR] {admin.Username}: {fcmEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FCM_CANCEL_ADMIN_OUTER] {ex.Message}");
            }
        }

        [HttpGet("pending-approvals")]
        public IActionResult GetPendingApprovals()
        {
            var items = _driverRepository.GetPendingApprovals();
            return Ok(items);
        }

        [HttpGet("approval-history")]
        public IActionResult GetApprovalHistory([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            var role = GetCurrentUserRole().ToUpper();
            var level = User.FindFirst("level")?.Value?.ToUpper() ?? "";
            bool isAdministrator = (role == "ADMINISTRATOR" || role == "1" || level == "ADMINISTRATOR" || level == "1");

            // Only for Admin Transport or Admin
            if (!IsAdminOrGate() && role != "ADMIN TRANSPORT")
            {
                return Forbid();
            }

            string? approverFilter = null;
            if (!isAdministrator)
            {
                approverFilter = GetCurrentUserName();
            }

            var items = _driverRepository.GetApprovalHistory(approverFilter, start, end);
            return Ok(items);
        }

        [HttpGet("all-checklists")]
        public IActionResult GetAllChecklists()
        {
            // Only for Administrator (Level 1)
            var role = GetCurrentUserRole().ToUpper();
            if (role != "ADMINISTRATOR" && role != "ADMIN" && GetCurrentUserRole() != "1")
            {
                return Forbid();
            }

            var items = _driverRepository.GetAllChecklists();
            return Ok(items);
        }

        [HttpGet("trucks")]
        public async Task<IActionResult> GetTruckStatus()
        {
            var items = _driverRepository.GetTrucksWithServiceStatus();

            var baseUrl = _configuration["ExternalApi:BaseUrl"];
            var apiKey = _configuration["ExternalApi:ApiKey"];

            if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(apiKey))
            {
                var client = _httpClientFactory.CreateClient("YLIDClient");

                // Fetch currentKM dari proxy eksternal untuk semua unit secara paralel
                var tasks = items.Select(async truck =>
                {
                    try
                    {
                        var url = $"{baseUrl}get-vehicle-licenses?search={Uri.EscapeDataString(truck.VehicleNo)}";
                        var req = new HttpRequestMessage(HttpMethod.Get, url);
                        req.Headers.Add("APIKEY", apiKey);
                        var resp = await client.SendAsync(req);
                        if (!resp.IsSuccessStatusCode) return;

                        var json = await resp.Content.ReadAsStringAsync();
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        var arr = root.ValueKind == System.Text.Json.JsonValueKind.Array ? root :
                                  root.TryGetProperty("data", out var d) ? d : default;

                        if (arr.ValueKind != System.Text.Json.JsonValueKind.Array) return;

                        foreach (var item in arr.EnumerateArray())
                        {
                            string? itemNoPol = null;
                            if (item.TryGetProperty("noPol", out var np)) itemNoPol = np.GetString();
                            else if (item.TryGetProperty("NoPol", out np)) itemNoPol = np.GetString();

                            if (!string.Equals(itemNoPol, truck.VehicleNo, StringComparison.OrdinalIgnoreCase)) continue;

                            int? proxyKM = null;
                            if (item.TryGetProperty("currentKM", out var km) || item.TryGetProperty("CurrentKM", out km))
                            {
                                if (km.ValueKind == System.Text.Json.JsonValueKind.Number) proxyKM = km.GetInt32();
                                else if (km.ValueKind == System.Text.Json.JsonValueKind.String && int.TryParse(km.GetString(), out var v)) proxyKM = v;
                            }

                            int? thisMonthSvcKm = null;
                            if (item.TryGetProperty("thisMonthServiceKm", out var svcKm) || item.TryGetProperty("ThisMonthServiceKm", out svcKm))
                            {
                                if (svcKm.ValueKind == System.Text.Json.JsonValueKind.Number) thisMonthSvcKm = svcKm.GetInt32();
                                else if (svcKm.ValueKind == System.Text.Json.JsonValueKind.String && int.TryParse(svcKm.GetString(), out var sv)) thisMonthSvcKm = sv;
                            }

                            truck.LastOdometer = proxyKM ?? 0;
                            if (thisMonthSvcKm.HasValue && thisMonthSvcKm.Value > 0)
                            {
                                truck.ThisMonthServiceKm = thisMonthSvcKm.Value;
                                truck.NextServiceKM = thisMonthSvcKm.Value + 10000;
                                truck.RemainingKM = truck.NextServiceKM - truck.LastOdometer;
                            }
                            else
                            {
                                const int Interval = 10000;
                                truck.NextServiceKM = ((truck.LastOdometer / Interval) + 1) * Interval;
                                truck.RemainingKM = truck.NextServiceKM - truck.LastOdometer;
                            }
                            truck.Status = truck.RemainingKM <= 500 ? "Servis Sekarang"
                                         : truck.RemainingKM <= 1500 ? "Segera Servis"
                                         : "Normal";
                            break;
                        }
                    }
                    catch { /* Jika proxy gagal, tetap pakai nilai dari DB */ }
                });

                await Task.WhenAll(tasks);
            }

            return Ok(items);
        }

        [HttpGet("details/{oid}")]
        public IActionResult GetDetails(Guid oid)
        {
            var header = _driverRepository.GetP2HWithDetails(oid);
            if (header == null) return NotFound();

            // Security Check: Drivers only see their own, Admin/Gate see all
            if (!IsAdminOrGate() && header.DriverCode != GetCurrentUserNIK())
            {
                return Forbid("Anda tidak memiliki akses ke data ini.");
            }

            return Ok(header);
        }

        [HttpGet("history/{driverCode}")]
        public IActionResult GetHistory(string driverCode, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            string? finalDriverCode = driverCode;
            bool isPrivileged = IsAdminOrGate() || GetCurrentUserRole().ToUpper() == "ADMIN TRANSPORT";

            if (!isPrivileged)
            {
                finalDriverCode = GetCurrentUserNIK();
            }
            else if (driverCode == "ALL" || string.IsNullOrEmpty(driverCode))
            {
                finalDriverCode = null;
            }

            var items = _driverRepository.GetHistoryByDriver(finalDriverCode, start, end);
            return Ok(items);
        }

        [HttpPost("update")]
        public IActionResult UpdateP2H([FromBody] P2HHeader header)
        {
            if (header == null || header.Oid == Guid.Empty)
            {
                return BadRequest("Invalid checklist data.");
            }

            var openIssuesOnUpdate = _driverRepository.GetOpenVehicleIssues(header.VehicleNo);
            if (openIssuesOnUpdate.Any())
            {
                foreach (var d in header.Details)
                {
                    bool isComplaint = d.Category == "KELUHAN" || (d.QuestionText?.Contains("keluhan", StringComparison.OrdinalIgnoreCase) ?? false);
                    string okAnswer = isComplaint ? "TIDAK" : "YA";
                    if (d.Answer == okAnswer && openIssuesOnUpdate.Any(i => i.ItemCode == d.ItemCode))
                    {
                        return BadRequest($"Tidak bisa menjawab {okAnswer} pada '{d.QuestionText}' — masih ada temuan yang belum diselesaikan untuk unit ini. Selesaikan dulu di Menu Utama.");
                    }
                }
            }

            if (_driverRepository.UpdateP2H(header))
            {
                // Jika butuh approval lagi, kirim notif ulang
                if (header.Status == "Waiting Approval")
                {
                    _ = Task.Run(() => NotifyAdminTransport(header));
                }

                return Ok(new { Message = "P2H Updated Successfully" });
            }

            return StatusCode(500, "Gagal mengupdate checklist P2H. Pastikan status masih Waiting Approval.");
        }

        [HttpGet("by-transaction/{transactionNo}")]
        public IActionResult GetByTransactionNo(string transactionNo)
        {
            var result = _driverRepository.GetByTransactionNo(transactionNo);
            if (result == null) return NotFound();

            // Security Check
            if (!IsAdminOrGate() && result.DriverCode != GetCurrentUserNIK())
            {
                return Forbid("Anda tidak memiliki akses ke transaksi ini.");
            }
            
            result.Details = _driverRepository.GetP2HDetails(result.Oid).ToList();
            return Ok(result);
        }

        [HttpGet("verify-gate/{transactionNo}")]
        public IActionResult VerifyGate(string transactionNo)
        {
            var p2h = _driverRepository.GetHistoryByDriver(null).FirstOrDefault(h => h.TransactionNo == transactionNo);
            if (p2h == null) return NotFound(new { success = false, message = "Data P2H tidak ditemukan." });

            // Cek Expired (2 hari dari ActivityDate)
            if (p2h.ActivityDate.Date.AddDays(2) < DateTime.Now.Date)
            {
                if (p2h.Status != "BATAL" && p2h.Status != "Closed")
                {
                    _driverRepository.MarkAsCancelled(p2h.Oid, "Checklist kedaluwarsa (lebih dari 2 hari dari tanggal kegiatan)", "System");
                    p2h.Status = "BATAL";
                    
                    // Notify Driver about cancellation
                    _ = Task.Run(() => NotifyDriverCancellation(p2h, "Checklist kedaluwarsa (lebih dari 2 hari)"));
                }
            }

            if (p2h.Status == "BATAL")
            {
                return Ok(new { success = false, message = "Checklist sudah kedaluwarsa (BATAL). Silakan buat checklist baru." });
            }

            if (p2h.Status == "Closed")
            {
                return Ok(new { success = false, message = "Checklist sudah selesai digunakan (Closed)." });
            }

            if (p2h.Status == "Approved")
            {
                // Cek jika sudah pernah Gate Out
                if (p2h.ExitTime != null)
                {
                    return Ok(new { success = false, message = "Kendaraan sudah pernah keluar gate. QRCode tidak berlaku lagi untuk keluar." });
                }

                return Ok(new { 
                    success = true, 
                    message = "Checklist Valid. Silakan konfirmasi untuk mengijinkan keluar.",
                    data = new {
                        p2h.VehicleNo,
                        p2h.DriverName,
                        p2h.TransactionNo
                    }
                });
            }
            else if (p2h.Status == "Waiting Approval")
            {
                return Ok(new { success = false, message = "P2H masih menunggu persetujuan Admin Transport." });
            }
            else
            {
                return Ok(new { success = false, message = $"Status P2H: {p2h.Status}. Truck tidak diijinkan keluar." });
            }
        }

        [HttpPost("gate-out")]
        public IActionResult GateOut([FromBody] GateOutRequest request)
        {
            if (string.IsNullOrEmpty(request.TransactionNo))
                return BadRequest("Transaction No is required.");

            if (_driverRepository.UpdateGateOut(request.TransactionNo, GetCurrentUserName()))
            {
                var p2h = _driverRepository.GetByTransactionNo(request.TransactionNo);
                if (p2h != null)
                {
                    _ = Task.Run(() => NotifyAdminGateActivity(p2h, "GATE OUT"));
                }
                return Ok(new { success = true, message = "Truck diijinkan keluar. Jam keluar telah dicatat." });
            }

            return StatusCode(500, "Gagal mencatat jam keluar gate.");
        }

        [HttpGet("gate-history")]
        public IActionResult GetGateHistory([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            var role = GetCurrentUserRole().ToUpper();
            var level = User.FindFirst("level")?.Value?.ToUpper() ?? "";
            bool isAdministrator = (role == "ADMINISTRATOR" || role == "1" || level == "ADMINISTRATOR" || level == "1");

            if (!IsAdminOrGate()) return Forbid();

            string? gateOfficerFilter = null;
            if (!isAdministrator)
            {
                gateOfficerFilter = GetCurrentUserName();
            }

            return Ok(_driverRepository.GetGateHistory(gateOfficerFilter, start, end));
        }

        [HttpPost("approve")]
        public IActionResult Approve([FromBody] SupervisorApprovalRequest request)
        {
            bool success = false;
            string status = request.Status ?? "Approved";

            if (status == "Approved")
                success = _driverRepository.ApproveP2H(request.Oid, request.Note, GetCurrentUserName());
            else
                success = _driverRepository.RejectP2H(request.Oid, request.Note, GetCurrentUserName());

            if (success)
            {
                // Trigger Notification to Driver
                var p2h = _driverRepository.GetP2HWithDetails(request.Oid);
                if (p2h != null)
                {
                    _ = Task.Run(() => NotifyDriver(p2h));
                }

                return Ok(new { Message = $"P2H {status} Successfully" });
            }
            return BadRequest("Gagal memproses. Form P2H ini sudah disetujui atau ditolak oleh Admin Transport lain.");
        }

        [HttpGet("vehicle-issues/{vehicleNo}")]
        public IActionResult GetVehicleIssues(string vehicleNo)
        {
            var items = _driverRepository.GetOpenVehicleIssues(vehicleNo);
            return Ok(items);
        }

        [HttpPost("vehicle-issues/resolve")]
        public IActionResult ResolveVehicleIssue([FromBody] VehicleIssueResolveRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ResolutionNote))
            {
                return BadRequest("Keterangan penyelesaian wajib diisi.");
            }

            bool success = _driverRepository.ResolveVehicleIssue(request.IssueOid, GetCurrentUserName(), request.ResolutionNote);
            if (!success) return BadRequest("Gagal menyimpan. Temuan ini mungkin sudah diselesaikan sebelumnya.");

            return Ok(new { Message = "Temuan berhasil diselesaikan." });
        }

        [HttpGet("vehicle-issues-report")]
        public IActionResult GetVehicleIssuesReport()
        {
            var role = GetCurrentUserRole().ToUpper();
            if (!IsAdminOrGate() && role != "ADMIN TRANSPORT")
            {
                return Forbid();
            }

            var items = _driverRepository.GetAllVehicleIssues();
            return Ok(items);
        }

        private async Task NotifyDriver(P2HHeader header)
        {
            try
            {
                bool isApproved = header.Status == "Approved";
                string title = "Status P2H Anda";
                string body = isApproved 
                    ? $"Checklist {header.VehicleNo} telah DISETUJUI oleh Admin Transport." 
                    : $"Checklist {header.VehicleNo} telah DITOLAK oleh Admin Transport. Silakan cek catatan.";

                _driverRepository.SaveNotification(header.DriverCode, title, body, null, header.TransactionNo, "approval_result");

                var token = _driverRepository.GetDriverToken(header.DriverCode);
                if (string.IsNullOrEmpty(token)) return;

                var messaging = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;
                if (messaging == null) return;

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
                        Headers = new Dictionary<string, string>()
                        {
                            { "apns-priority", "10" }
                        },
                        Aps = new FirebaseAdmin.Messaging.Aps()
                        {
                            Sound = "default",
                            Badge = 1
                        }
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "title", title },
                        { "body", body },
                        { "type", "approval_result" },
                        { "status", header.Status ?? "Approved" },
                        { "transaction_no", header.TransactionNo ?? "" },
                        { "nik", header.DriverCode ?? "" }
                    }
                };

                await messaging.SendAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FCM_DRIVER_ERROR] {ex.Message}");
            }
        }

        private async Task NotifyDriverCancellation(P2HHeader header, string reason)
        {
            try
            {
                string title = "Checklist Dibatalkan";
                string body = $"Checklist {header.VehicleNo} telah dibatalkan otomatis: {reason}.";

                _driverRepository.SaveNotification(header.DriverCode, title, body, null, header.TransactionNo, "cancellation_alert");

                var token = _driverRepository.GetDriverToken(header.DriverCode);
                if (string.IsNullOrEmpty(token)) return;

                var messaging = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;
                if (messaging == null) return;

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
                        Headers = new Dictionary<string, string>()
                        {
                            { "apns-priority", "10" }
                        },
                        Aps = new FirebaseAdmin.Messaging.Aps()
                        {
                            Sound = "default",
                            Badge = 1
                        }
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "title", title },
                        { "body", body },
                        { "type", "cancellation_alert" },
                        { "transaction_no", header.TransactionNo ?? "" },
                        { "nik", header.DriverCode ?? "" }
                    }
                };
                await messaging.SendAsync(message);
            }
            catch (Exception ex) { Console.WriteLine($"[FCM_CANCEL_ERROR] {ex.Message}"); }
        }

        private async Task NotifyAdminGateActivity(P2HHeader header, string activityType)
        {
            try
            {
                var adminTokens = _driverRepository.GetAdminTransportUsersAndTokens();
                if (adminTokens == null || !adminTokens.Any()) return;

                var messaging = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;

                foreach (var admin in adminTokens)
                {
                    try
                    {
                        // 1. Selalu simpan notifikasi ke database (untuk in-app inbox & kebutuhan audit)
                        _driverRepository.SaveNotification(
                            admin.DriverCode, 
                            $"Aktivitas Gate: {activityType}", 
                            $"Unit {header.VehicleNo} ({header.DriverName}) telah melakukan {activityType}.", 
                            null, 
                            header.TransactionNo, 
                            "gate_activity"
                        );
                    }
                    catch (Exception dbEx)
                    {
                        Console.WriteLine($"[NotifyAdminGateActivity] Gagal menyimpan notifikasi ke DB untuk {admin.Username}: {dbEx.Message}");
                    }

                    // 2. Kirim push jika token FCM ada dan Firebase aktif
                    if (messaging != null && !string.IsNullOrEmpty(admin.FcmToken))
                    {
                        try
                        {
                            var message = new FirebaseAdmin.Messaging.Message()
                            {
                                Token = admin.FcmToken,
                                Notification = new FirebaseAdmin.Messaging.Notification()
                                {
                                    Title = $"Aktivitas Gate: {activityType}",
                                    Body = $"Unit {header.VehicleNo} ({header.DriverName}) telah melakukan {activityType}."
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
                                    Headers = new Dictionary<string, string>()
                                    {
                                        { "apns-priority", "10" }
                                    },
                                    Aps = new FirebaseAdmin.Messaging.Aps()
                                    {
                                        Sound = "default",
                                        Badge = 1
                                    }
                                },
                                Data = new Dictionary<string, string>()
                                {
                                    { "title", $"Aktivitas Gate: {activityType}" },
                                    { "body", $"Unit {header.VehicleNo} ({header.DriverName}) telah melakukan {activityType}." },
                                    { "type", "gate_activity" },
                                    { "transaction_no", header.TransactionNo ?? "" },
                                    { "vehicle_no", header.VehicleNo ?? "" }
                                }
                            };
                            await messaging.SendAsync(message);
                        }
                        catch (Exception fcmEx)
                        {
                            Console.WriteLine($"[FCM_GATE_ERROR] Gagal mengirim push ke {admin.Username}: {fcmEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"[FCM_GATE_ERROR_OUTER] {ex.Message}"); 
            }
        }

        [HttpGet("document-monitoring")]
        public async Task<IActionResult> GetDocumentMonitoring()
        {
            var results = new System.Collections.Concurrent.ConcurrentBag<DocumentMonitorItem>();
            var baseUrl = _configuration["ExternalApi:BaseUrl"];
            var apiKey  = _configuration["ExternalApi:ApiKey"];

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
                return Ok(results.ToList());

            var client    = _httpClientFactory.CreateClient("YLIDClient");
            var semaphore = new SemaphoreSlim(5);

            static string CalcStatus(int days) =>
                days < 0 ? "Sudah Expired" : days <= 14 ? "Akan Expired" : "Mendekati Expired";

            // --- Vehicle documents ---
            var trucks = _driverRepository.GetTrucksWithServiceStatus();
            var vehicleTasks = trucks.Select(async truck =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var url = $"{baseUrl}get-vehicle-licenses?search={Uri.EscapeDataString(truck.VehicleNo)}";
                    var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.Add("APIKEY", apiKey);
                    var resp = await client.SendAsync(req);
                    if (!resp.IsSuccessStatusCode) return;

                    var json = await resp.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var arr = root.ValueKind == System.Text.Json.JsonValueKind.Array ? root :
                              root.TryGetProperty("data", out var d) ? d : default;
                    if (arr.ValueKind != System.Text.Json.JsonValueKind.Array) return;

                    foreach (var item in arr.EnumerateArray())
                    {
                        string? noPol = null;
                        if (item.TryGetProperty("noPol", out var np) || item.TryGetProperty("NoPol", out np))
                            noPol = np.GetString();
                        if (!string.Equals(noPol, truck.VehicleNo, StringComparison.OrdinalIgnoreCase)) continue;

                        string? docType = null;
                        if (item.TryGetProperty("vehicleDocuments", out var vd) || item.TryGetProperty("VehicleDocuments", out vd))
                            docType = vd.GetString();

                        string? expiryStr = null;
                        if (item.TryGetProperty("expiryDate", out var ed) || item.TryGetProperty("ExpiryDate", out ed))
                            expiryStr = ed.GetString();

                        if (string.IsNullOrEmpty(docType) || !DateTime.TryParse(expiryStr, out var expDate)) continue;

                        var days = (int)(expDate.Date - DateTime.Today).TotalDays;
                        if (days > 30) continue; // hanya tampilkan yang perlu perhatian

                        results.Add(new DocumentMonitorItem
                        {
                            Identifier   = truck.VehicleNo,
                            DocType      = docType,
                            Category     = "Kendaraan",
                            ExpiryDate   = expDate,
                            DaysRemaining = days,
                            DocStatus    = CalcStatus(days)
                        });
                    }
                }
                catch { }
                finally { semaphore.Release(); }
            });

            // --- Driver SIM ---
            var users = _driverRepository.GetAllUsers()
                .Where(u => u.LevelUser == "Driver" && !string.IsNullOrEmpty(u.DriverCode))
                .ToList();

            var driverTasks = users.Select(async user =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var url = $"{baseUrl}get-driver-licenses?nik={Uri.EscapeDataString(user.DriverCode)}";
                    var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.Add("APIKEY", apiKey);
                    var resp = await client.SendAsync(req);
                    if (!resp.IsSuccessStatusCode) return;

                    var json = await resp.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var arr = root.TryGetProperty("data", out var d) ? d :
                              root.ValueKind == System.Text.Json.JsonValueKind.Array ? root : default;
                    if (arr.ValueKind != System.Text.Json.JsonValueKind.Array) return;

                    foreach (var item in arr.EnumerateArray())
                    {
                        string? expiryStr = null;
                        if (item.TryGetProperty("expiryDate", out var ed) || item.TryGetProperty("ExpiryDate", out ed))
                            expiryStr = ed.GetString();
                        if (!DateTime.TryParse(expiryStr, out var expDate)) continue;

                        string? driverName = null;
                        if (item.TryGetProperty("fullName", out var fn) || item.TryGetProperty("FullName", out fn))
                            driverName = fn.GetString();

                        var days = (int)(expDate.Date - DateTime.Today).TotalDays;
                        if (days > 30) continue;

                        results.Add(new DocumentMonitorItem
                        {
                            Identifier    = driverName ?? user.FullName,
                            DocType       = "SIM",
                            Category      = "Driver",
                            ExpiryDate    = expDate,
                            DaysRemaining = days,
                            DocStatus     = CalcStatus(days)
                        });
                    }
                }
                catch { }
                finally { semaphore.Release(); }
            });

            await Task.WhenAll(vehicleTasks.Concat(driverTasks));

            return Ok(results.OrderBy(r => r.DaysRemaining).ToList());
        }

        // ── Job pengingat SIM: status & trigger manual (admin only) ──

        private bool IsAdmin()
        {
            var role = GetCurrentUserRole().ToUpper();
            var level = User.FindFirst("level")?.Value?.ToUpper() ?? "";
            return role.Contains("ADMIN") || level.Contains("ADMIN") ||
                   role == "ADMINISTRATOR" || level == "ADMINISTRATOR";
        }

        /// <summary>Status eksekusi terakhir job pengingat SIM (kapan jalan, berapa notif terkirim, error, jadwal berikutnya).</summary>
        [HttpGet("jobs/sim-expiry-status")]
        public IActionResult GetSimExpiryStatus()
        {
            if (!IsAdmin()) return Forbid();
            return Ok(_simExpiryJob.GetStatus());
        }

        /// <summary>Jalankan job pengingat SIM sekarang juga (untuk testing/verifikasi tanpa menunggu jadwal harian).</summary>
        [HttpPost("jobs/check-sim-expiry")]
        public async Task<IActionResult> TriggerSimExpiry(CancellationToken ct)
        {
            if (!IsAdmin()) return Forbid();
            var status = await _simExpiryJob.RunAsync("manual", ct);
            return Ok(status);
        }

        [HttpGet("notifications/{nik}")]
        public IActionResult GetNotifications(string nik)
        {
            try
            {
                var list = _driverRepository.GetNotificationsByNik(nik);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("notifications/read/{oid}")]
        public IActionResult MarkAsRead(Guid oid)
        {
            try
            {
                bool success = _driverRepository.MarkNotificationAsRead(oid);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("notifications/read-all/{nik}")]
        public IActionResult MarkAllAsRead(string nik)
        {
            try
            {
                bool success = _driverRepository.MarkAllNotificationsAsRead(nik);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpDelete("notifications/{oid}")]
        public IActionResult DeleteNotification(Guid oid)
        {
            try
            {
                bool success = _driverRepository.DeleteNotification(oid);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }

    public class SupervisorApprovalRequest
    {
        public Guid Oid { get; set; }
        public string? Note { get; set; }
        public string? Status { get; set; } // Approved or Rejected
    }

    public class VehicleIssueResolveRequest
    {
        public Guid IssueOid { get; set; }
        public string? ResolutionNote { get; set; }
    }

    public class GateOutRequest
    {
        public string TransactionNo { get; set; } = string.Empty;
    }

    public class CancelP2HRequest
    {
        public Guid Oid { get; set; }
        public string? Reason { get; set; }
    }
}
