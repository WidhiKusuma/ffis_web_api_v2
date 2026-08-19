using System.Text.Json;

namespace ffis_web_api
{
    public class AppVersionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Version _minimumVersion;        // FFIS / P2H
        private readonly Version _cmsYlidMinimumVersion; // CMS YLID

        private const string CmsYlidPrefix = "/api/cms_ylid";

        private static readonly HashSet<string> _bypassPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/version/latest",
            "/api/version/download",
            "/api/auth/login",
            "/api/driverauth/login",
            "/api/cms_ylid/auth/login",   // login bypass; endpoint data tetap kena gerbang versi
            "/api/cms_ylid/version",      // cek versi & download apk (anonim)
            "/api/cms_ylid/config",       // konfigurasi publik (mis. nomor WA transport)
            "/swagger",
        };

        public AppVersionMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _minimumVersion        = ParseVersion(configuration["AppUpdate:MinimumVersion"]);
            _cmsYlidMinimumVersion = ParseVersion(configuration["AppUpdateCmsYlid:MinimumVersion"]);
        }

        private static Version ParseVersion(string? value)
            => Version.TryParse(value ?? "1.0", out var v) ? v : new Version(1, 0);

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";

            // Bypass path tertentu (login, version check, swagger)
            if (_bypassPaths.Any(bp => path.StartsWith(bp, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Pilih minimum versi sesuai aplikasi (cms_ylid punya versioning sendiri)
            bool isCmsYlid = path.StartsWith(CmsYlidPrefix, StringComparison.OrdinalIgnoreCase);
            var minimumVersion = isCmsYlid ? _cmsYlidMinimumVersion : _minimumVersion;

            var appVersionHeader = context.Request.Headers["X-App-Version"].FirstOrDefault();

            // Tidak ada header → app lama, tolak
            if (string.IsNullOrEmpty(appVersionHeader))
            {
                await WriteBlockedResponse(context, "Versi aplikasi tidak dikenali. Silakan install ulang aplikasi terbaru.");
                return;
            }

            // Header ada tapi versi terlalu lama → tolak
            if (Version.TryParse(appVersionHeader, out var clientVersion) && clientVersion < minimumVersion)
            {
                await WriteBlockedResponse(context, $"Versi aplikasi ({appVersionHeader}) sudah tidak didukung. Minimum versi {minimumVersion}. Silakan update aplikasi.");
                return;
            }

            await _next(context);
        }

        private static async Task WriteBlockedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = 426; // Upgrade Required
            context.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new { message, upgradeRequired = true });
            await context.Response.WriteAsync(body);
        }
    }
}
