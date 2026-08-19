using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ffis_web_api.Models.CmsYlid;
using ffis_web_api.Repositories;

namespace ffis_web_api.Controllers.CmsYlid
{
    [ApiController]
    [Route("api/cms_ylid")]
    [Authorize]
    public class CmsYlidController : ControllerBase
    {
        private readonly ICmsYlidRepository _repository;

        public CmsYlidController(ICmsYlidRepository repository)
        {
            _repository = repository;
        }

        // GET api/cms_ylid/pickups?division=AFF
        [HttpGet("pickups")]
        public async Task<IActionResult> GetPickups([FromQuery] string division = "")
        {
            var data = await _repository.GetPickupsAsync(division);
            return Ok(data);
        }

        // GET api/cms_ylid/destinations?pickup=BANDARA/CENGKARENG&division=AFF
        [HttpGet("destinations")]
        public async Task<IActionResult> GetDestinations([FromQuery] string pickup = "", [FromQuery] string division = "")
        {
            var data = await _repository.GetDestinationsAsync(pickup, division);
            return Ok(data);
        }

        // POST api/cms_ylid/search
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] CmsYlidSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Pickup) || string.IsNullOrWhiteSpace(request.Destination))
                return BadRequest(new { Message = "Pickup dan destination wajib diisi." });

            var result = await _repository.SearchPricesAsync(request);
            return Ok(result);
        }
    }
}
