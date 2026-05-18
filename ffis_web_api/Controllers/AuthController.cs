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
    public class AuthController : Controller
    {
        private readonly UserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthController(UserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDTO loginRequest)
        {
            // Akses properti menggunakan loginRequest.Username dan loginRequest.Password
            var existingUser = _userRepository.GetUserByUsernameAndPassword(loginRequest.Username, loginRequest.Password);

            if (existingUser == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            var token = GenerateJwtToken(existingUser);
            // Mengembalikan JWT dan data pengguna yang relevan
            return Ok(new
            {
                Token = token,
                Username = existingUser.Username,
                FullName = existingUser.fullname,
                NIK = existingUser.nik,
                Email = existingUser.email
                // Anda dapat menambahkan data lain yang dikembalikan oleh SP di sini
            });
        }

        private string GenerateJwtToken(APIUserFFIS user)
        {
            // Menggunakan data pengguna yang lengkap dari SP untuk claims
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(ClaimTypes.Name, user.fullname), // Nama lengkap
                new Claim(ClaimTypes.Role, user.Role), // Role
                new Claim("nik", user.nik ?? ""), // NIK sebagai custom claim
                new Claim("location", user.Location ?? ""), // Lokasi sebagai custom claim
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Tambahkan claim lainnya jika diperlukan (Position, Section, Division, dll.)

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}