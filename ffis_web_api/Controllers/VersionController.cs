using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ffis_web_api.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class VersionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public VersionController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
            // Bangun download URL otomatis ke endpoint /api/version/download
            var request = HttpContext.Request;
            var downloadUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/api/version/download";

            var info = new AppVersionInfo
            {
                LatestVersion = _configuration["AppUpdate:LatestVersion"] ?? "1.3",
                ReleaseDate   = _configuration["AppUpdate:ReleaseDate"]   ?? "",
                DownloadUrl   = downloadUrl,
                ReleaseNotes  = _configuration["AppUpdate:ReleaseNotes"]  ?? "",
                ForceUpdate   = bool.TryParse(_configuration["AppUpdate:ForceUpdate"], out var force) && force
            };

            return Ok(info);
        }

        [HttpGet("download")]
        public IActionResult DownloadApk()
        {
            var apkFolder = Path.Combine(_env.ContentRootPath, "downloads");
            var apkPath   = Path.Combine(apkFolder, "p2h_trucking.apk");

            if (!System.IO.File.Exists(apkPath))
                return NotFound("File APK tidak ditemukan di server.");

            var stream = new FileStream(apkPath, FileMode.Open, FileAccess.Read);
            return File(stream, "application/vnd.android.package-archive", "p2h_trucking.apk");
        }
    }

    public class AppVersionInfo
    {
        public string LatestVersion { get; set; } = string.Empty;
        public string ReleaseDate   { get; set; } = string.Empty;
        public string DownloadUrl   { get; set; } = string.Empty;
        public string ReleaseNotes  { get; set; } = string.Empty;
        public bool   ForceUpdate   { get; set; }
    }
}
