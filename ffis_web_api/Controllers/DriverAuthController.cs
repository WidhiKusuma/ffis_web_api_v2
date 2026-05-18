using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Json;
using Microsoft.IdentityModel.Tokens;
using ffis_web_api.Models;
using ffis_web_api.Repositories;

namespace ffis_web_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DriverAuthController : Controller
    {
        private readonly DriverRepository _driverRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public DriverAuthController(DriverRepository driverRepository, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _driverRepository = driverRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequestDTO request)
        {
            // 1. Cek apakah NIK atau Username sudah terdaftar di UserP2H
            if (_driverRepository.IsUserRegistered(request.DriverCode, request.Username))
            {
                // Kita perlu tahu mana yang duplikat, tapi untuk sederhananya 
                // kita kembalikan pesan umum atau cek satu per satu jika diperlukan.
                // Disini kita asumsikan DriverCode adalah NIK.
                return BadRequest("NIK atau Username sudah terdaftar. Gunakan NIK lain atau login jika sudah punya akun.");
            }

            // 2. Simpan ke UserP2H (NIK disimpan ke kolom DriverCode)
            var newUser = new UserP2H
            {
                DriverCode = request.DriverCode, // NIK
                Username = request.Username,
                Password = request.Password,
                FullName = request.FullName,
                LevelUser = request.LevelUser ?? "Driver"
            };

            if (_driverRepository.Register(newUser))
            {
                return Ok("Registrasi berhasil. Silakan login.");
            }

            return StatusCode(500, "Terjadi kesalahan saat registrasi.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginRequest)
        {
            var user = _driverRepository.Login(loginRequest.Username, loginRequest.Password);

            if (user == null)
            {
                return Unauthorized("Username atau Password salah.");
            }

            // AMBIL DATA PROFIL DARI API EKSTERNAL (BERDASARKAN NIK)
            ExternalDriverResponse? profile = null;
            try
            {
                var baseUrl = _configuration["ExternalApi:BaseUrl"];
                var apiKey = _configuration["ExternalApi:ApiKey"];
                var url = $"{baseUrl}get-driver-licenses?search={user.DriverCode}";

                var client = _httpClientFactory.CreateClient("YLIDClient");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("APIKEY", apiKey);

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var apiResult = await response.Content.ReadFromJsonAsync<ExternalApiResponse>();
                    profile = apiResult?.Data?.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                // Jika API eksternal gagal, kita tetap izinkan login tapi profil kosong
                Console.WriteLine($"[LoginProfile] Error fetching external profile: {ex.Message}");
            }

            var token = GenerateJwtToken(user);
            
            return Ok(new
            {
                Token = token,
                Username = user.Username,
                FullName = user.FullName,
                NIK = user.DriverCode,
                Level = user.LevelUser,
                Profile = profile // Sertakan data profil lengkap (License No, Expiry, dll)
            });
        }
        
        [Authorize]
        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest("Username and New Password are required.");
            }

            if (_driverRepository.UpdatePassword(request.Username, request.NewPassword))
            {
                return Ok("Password reset successfully.");
            }

            return BadRequest("Failed to reset password. User might not exist.");
        }

        [Authorize]
        [HttpPost("update-level")]
        public IActionResult UpdateLevel([FromBody] UpdateLevelRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.NewLevel))
            {
                return BadRequest("Username and New Level are required.");
            }

            if (_driverRepository.UpdateUserLevel(request.Username, request.NewLevel))
            {
                return Ok("User level updated successfully.");
            }

            return BadRequest("Failed to update user level.");
        }

        private string GenerateJwtToken(UserP2H user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username ?? ""),
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim(ClaimTypes.Role, user.LevelUser ?? "Driver"),
                new Claim("nik", user.DriverCode ?? ""),
                new Claim("level", user.LevelUser ?? "Driver"),
                new Claim("oid", user.Oid.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class UpdateLevelRequestDTO
    {
        public string Username { get; set; }
        public string NewLevel { get; set; }
    }
}
