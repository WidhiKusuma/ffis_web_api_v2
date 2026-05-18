using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Json;
using ffis_web_api.Repositories;
using ffis_web_api.Models;

namespace ffis_web_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DriverController : Controller
    {
        private readonly DriverRepository _driverRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public DriverController(DriverRepository driverRepository, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _driverRepository = driverRepository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 3)
            {
                return BadRequest("Search query must be at least 3 characters.");
            }

            try
            {
                var baseUrl = _configuration["ExternalApi:BaseUrl"];
                var apiKey = _configuration["ExternalApi:ApiKey"];
                var url = $"{baseUrl}get-driver-licenses?search={name}";

                var client = _httpClientFactory.CreateClient("YLIDClient");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("APIKEY", apiKey);

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var apiResult = await response.Content.ReadFromJsonAsync<ExternalApiResponse>();
                    return Ok(apiResult);
                }

                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, error ?? "Error calling external driver API");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("detail/{nik}")]
        public async Task<IActionResult> GetDetail(string nik)
        {
            try
            {
                var baseUrl = _configuration["ExternalApi:BaseUrl"];
                var apiKey = _configuration["ExternalApi:ApiKey"];
                var url = $"{baseUrl}get-driver-licenses?nik={nik}";

                var client = _httpClientFactory.CreateClient("YLIDClient");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("APIKEY", apiKey);

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var apiResult = await response.Content.ReadFromJsonAsync<ExternalApiResponse>();
                    return Ok(apiResult);
                }

                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, error ?? "Error calling external driver API");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
