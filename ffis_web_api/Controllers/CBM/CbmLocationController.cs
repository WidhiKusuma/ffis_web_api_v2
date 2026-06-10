using ffis_web_api.Models.CBM;
using ffis_web_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ffis_web_api.Controllers.CBM
{
    [ApiController]
    [Route("api/cbm/locations")]
    public class CbmLocationController : ControllerBase
    {
        private readonly ICbmRepository _cbmRepository;

        public CbmLocationController(ICbmRepository cbmRepository)
        {
            _cbmRepository = cbmRepository;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchLocations([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Ok(new List<CbmLocation>());
            }

            var results = await _cbmRepository.SearchLocationsAsync(keyword);
            return Ok(results);
        }
    }
}
