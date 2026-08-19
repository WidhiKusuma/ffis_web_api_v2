using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ffis_web_api.Controllers.CmsYlid
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/cms_ylid/version")]
    public class CmsYlidVersionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        private const string ApkFileName = "cms_ylid.apk";

        public CmsYlidVersionController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
            var request = HttpContext.Request;
            var downloadUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/api/cms_ylid/version/download";

            var info = new CmsYlidVersionInfo
            {
                LatestVersion  = _configuration["AppUpdateCmsYlid:LatestVersion"]  ?? "1.0",
                MinimumVersion = _configuration["AppUpdateCmsYlid:MinimumVersion"] ?? "1.0",
                ReleaseDate    = _configuration["AppUpdateCmsYlid:ReleaseDate"]    ?? "",
                DownloadUrl    = downloadUrl,
                ReleaseNotes   = _configuration["AppUpdateCmsYlid:ReleaseNotes"]   ?? "",
                ForceUpdate    = bool.TryParse(_configuration["AppUpdateCmsYlid:ForceUpdate"], out var force) && force
            };

            return Ok(info);
        }

        [HttpGet("download")]
        public IActionResult DownloadApk()
        {
            var apkFolder = Path.Combine(_env.ContentRootPath, "downloads");
            var apkPath   = Path.Combine(apkFolder, ApkFileName);

            if (!System.IO.File.Exists(apkPath))
                return NotFound("File APK cms_ylid tidak ditemukan di server.");

            var stream = new FileStream(apkPath, FileMode.Open, FileAccess.Read);
            return File(stream, "application/vnd.android.package-archive", ApkFileName);
        }
    }

    public class CmsYlidVersionInfo
    {
        public string LatestVersion  { get; set; } = string.Empty;
        public string MinimumVersion { get; set; } = string.Empty;
        public string ReleaseDate    { get; set; } = string.Empty;
        public string DownloadUrl    { get; set; } = string.Empty;
        public string ReleaseNotes   { get; set; } = string.Empty;
        public bool   ForceUpdate    { get; set; }
    }
}
