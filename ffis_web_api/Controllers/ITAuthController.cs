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
    }

    public class FcmTokenRequest
    {
        public string Nik { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
