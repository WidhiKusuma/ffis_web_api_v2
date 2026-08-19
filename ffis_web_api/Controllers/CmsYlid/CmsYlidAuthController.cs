using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ffis_web_api.Models.CmsYlid;
using ffis_web_api.Repositories;

namespace ffis_web_api.Controllers.CmsYlid
{
    [ApiController]
    [Route("api/cms_ylid/auth")]
    public class CmsYlidAuthController : ControllerBase
    {
        private readonly ICmsYlidRepository _repository;
        private readonly IConfiguration _configuration;

        public CmsYlidAuthController(ICmsYlidRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] CmsYlidLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new CmsYlidLoginResponse { Success = false, Message = "Username dan password wajib diisi." });

            var user = await _repository.LoginAsync(request.Username, request.Password);
            if (user == null)
                return Unauthorized(new CmsYlidLoginResponse { Success = false, Message = "Username atau password salah." });

            var token = GenerateJwtToken(user);
            return Ok(new CmsYlidLoginResponse
            {
                Success = true,
                Message = "Login berhasil.",
                Token   = token,
                User    = user
            });
        }

        private string GenerateJwtToken(CmsYlidUserDto user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("user_id", user.UserId.ToString()),
                new Claim("email", user.Email ?? ""),
                new Claim("app", "cms_ylid"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
