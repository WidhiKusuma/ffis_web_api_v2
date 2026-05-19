using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ffis_web_api.Repositories;
using ffis_web_api.Models;

namespace ffis_web_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GateController : Controller
    {
        private readonly DriverRepository _driverRepository;

        public GateController(DriverRepository driverRepository)
        {
            _driverRepository = driverRepository;
        }

        private string GetCurrentUserName() => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Gate Officer";

        [HttpPost("verify")]
        public IActionResult Verify([FromBody] GateVerifyRequest request)
        {
            if (string.IsNullOrEmpty(request.BarcodeData))
            {
                return BadRequest("Barcode data is required.");
            }

            // Extract Transaction No from Barcode Data (Format: TransactionNo|DriverCode|VehicleNo)
            string transactionNo = request.BarcodeData.Split('|')[0];
            
            // Re-use P2HController logic via repository or direct call
            // For now, let's just return a placeholder or call a shared service logic
            // Since we already have verify-gate in P2HController, we can keep using that for Gate Out.
            
            return Ok(new { 
                success = true, 
                message = "Validation successful. Use P2H/verify-gate for Gate Out." 
            });
        }

        [HttpPost("gate-in")]
        public IActionResult GateIn([FromBody] GateInRequest request)
        {
            if (string.IsNullOrEmpty(request.TransactionNo))
                return BadRequest("Transaction No is required.");

            if (request.Odometer <= 0)
                return BadRequest("Odometer must be greater than 0.");

            // Workflow Check: Must have Gate Out first
            var p2h = _driverRepository.GetHistoryByDriver(null).FirstOrDefault(h => h.TransactionNo == request.TransactionNo);
            if (p2h == null) return NotFound("Data P2H tidak ditemukan.");
            
            if (p2h.ExitTime == null)
            {
                return Ok(new { success = false, message = "Unit belum tercatat keluar gate. Silakan lakukan scan keluar terlebih dahulu sebelum mencatat masuk." });
            }

            if (p2h.Status == "Closed")
            {
                return Ok(new { success = false, message = "Unit sudah pernah melakukan Gate In. Status perjalanan ini sudah selesai (Closed)." });
            }

            if (_driverRepository.UpdateGateIn(request.TransactionNo, request.Odometer, GetCurrentUserName()))
            {
                var p2hData = _driverRepository.GetByTransactionNo(request.TransactionNo);
                if (p2hData != null)
                {
                    _ = Task.Run(() => NotifyAdminGateActivity(p2hData, "GATE IN"));
                }
                return Ok(new { success = true, message = "Kendaraan berhasil masuk (Gate In). Status Closed." });
            }

            return StatusCode(500, "Gagal memproses Gate In. Silakan coba lagi.");
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
    }

    public class GateVerifyRequest
    {
        public string BarcodeData { get; set; } = string.Empty;
    }

    public class GateInRequest
    {
        public string TransactionNo { get; set; }
        public int Odometer { get; set; }
    }
}
