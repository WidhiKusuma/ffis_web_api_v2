using ffis_web_api.Models.CBM;
using ffis_web_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ffis_web_api.Controllers.CBM
{
    [Route("api/cbm/auth")]
    [ApiController]
    public class CbmAuthController : ControllerBase
    {
        private readonly ICbmRepository _repository;

        public CbmAuthController(ICbmRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] CbmLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.PhoneNumber))
            {
                return BadRequest(new CbmLoginResponse { Success = false, Message = "Phone number is required." });
            }

            var response = await _repository.LoginAsync(request.PhoneNumber);
            if (response != null)
            {
                return Ok(response);
            }

            return Unauthorized(new CbmLoginResponse { Success = false, Message = "Invalid phone number." });
        }
    }
}
