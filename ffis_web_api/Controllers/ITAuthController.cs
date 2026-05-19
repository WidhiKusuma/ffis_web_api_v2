using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ffis_web_api.Models;
using ffis_web_api.Repositories;

namespace ffis_web_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ITAuthController : Controller
    {
        private readonly ITStockRepository _itStockRepository;
        private readonly IConfiguration _configuration;

        public ITAuthController(ITStockRepository itStockRepository, IConfiguration configuration)
        {
            _itStockRepository = itStockRepository;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDTO loginRequest)
        {
            var existingUser = _itStockRepository.Login(loginRequest.Username, loginRequest.Password);

            if (existingUser == null)
            {
                return Unauthorized("Invalid IT Admin username or password.");
            }

            var token = GenerateJwtToken(existingUser);
            
            return Ok(new
            {
                Token = token,
                Username = existingUser.username,
                FullName = existingUser.fullname,
                NIK = existingUser.nik,
                Email = existingUser.email_karyawan,
                Level = existingUser.level_user,
                Branch = existingUser.nama_branch,
                Title = existingUser.tittle,
                Division = existingUser.division,
                Section = existingUser.section
            });
        }

        private string GenerateJwtToken(ITAdminUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.username ?? ""),
                new Claim(ClaimTypes.Name, user.fullname ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "ITAdmin"),
                new Claim("nik", user.nik ?? ""),
                new Claim("level", user.level_user ?? ""),
                new Claim("title", user.tittle ?? ""),
                new Claim("division", user.division ?? ""),
                new Claim("section", user.section ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8), // Admin biasanya butuh session lebih lama
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("sync-token")]
        public IActionResult SyncToken([FromBody] FcmTokenRequest request)
        {
            try 
            {
                // Kita gunakan DriverRepository untuk update token di UserP2H
                // Ini karena ITAuth sering dipanggil oleh aplikasi mobile p2h
                var driverRepo = (DriverRepository)HttpContext.RequestServices.GetService(typeof(DriverRepository))!;
                if (driverRepo.SyncFcmToken(request.Nik, request.Token))
                {
                    return Ok(new { Message = "Token synced successfully" });
                }
                return BadRequest("Failed to sync token");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("test-push")]
        public async Task<IActionResult> TestPush([FromBody] TestPushRequest request)
        {
            try
            {
                var messaging = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;
                if (messaging == null)
                {
                    return BadRequest("Firebase Admin SDK is not initialized.");
                }

                // Ambil token dari database berdasarkan NIK/Username jika token tidak disediakan langsung
                string? token = request.Token;
                if (string.IsNullOrEmpty(token))
                {
                    var driverRepo = (DriverRepository)HttpContext.RequestServices.GetService(typeof(DriverRepository))!;
                    token = driverRepo.GetDriverToken(request.NikOrUsername);
                    if (string.IsNullOrEmpty(token))
                    {
                        // Coba cari di admin list juga
                        var adminTokens = driverRepo.GetAdminTransportUsersAndTokens();
                        var admin = adminTokens.FirstOrDefault(a => a.DriverCode == request.NikOrUsername || a.Username == request.NikOrUsername);
                        token = admin?.FcmToken;
                    }
                }

                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest($"FCM Token not found in database for user/NIK: {request.NikOrUsername} and no explicit token provided.");
                }

                var message = new FirebaseAdmin.Messaging.Message()
                {
                    Token = token,
                    Notification = new FirebaseAdmin.Messaging.Notification()
                    {
                        Title = request.Title ?? "Test Push Notification",
                        Body = request.Body ?? "This is a diagnostic push notification test."
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
                        { "title", request.Title ?? "Test Push Notification" },
                        { "body", request.Body ?? "This is a diagnostic push notification test." },
                        { "type", request.Type ?? "test_notification" }
                    }
                };

                var result = await messaging.SendAsync(message);
                return Ok(new { Success = true, MessageId = result, TokenUsed = token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }
    }

    public class FcmTokenRequest
    {
        public string Nik { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class TestPushRequest
    {
        public string NikOrUsername { get; set; } = string.Empty;
        public string? Token { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Type { get; set; }
    }
}
