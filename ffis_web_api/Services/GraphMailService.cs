using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ffis_web_api.Services
{
    /// <summary>
    /// Pengirim email via Microsoft Graph (modern auth / OAuth2 client credentials).
    /// Tidak memakai SMTP — email dikirim langsung dari server, sehingga perangkat
    /// pengguna tidak perlu memasang aplikasi email.
    /// Konfigurasi (ClientId/ClientSecret/TenantId/SendMailEndpoint) ada di appsettings:
    /// section "CmsYlid:Mail".
    /// </summary>
    public class GraphMailService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;

        public GraphMailService(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            _config = config;
        }

        public async Task SendAsync(
            string subject, string body,
            IEnumerable<string> to, IEnumerable<string>? cc = null)
        {
            var clientId     = _config["CmsYlid:Mail:ClientId"];
            var clientSecret = _config["CmsYlid:Mail:ClientSecret"];
            var tenantId     = _config["CmsYlid:Mail:TenantId"];
            var endpoint     = _config["CmsYlid:Mail:SendMailEndpoint"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret)
                || string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(endpoint))
                throw new InvalidOperationException(
                    "Konfigurasi Mail (Graph) belum lengkap di appsettings (CmsYlid:Mail).");

            var http = _httpFactory.CreateClient("YLIDClient");

            // 1) Ambil access token — client credentials flow (app-only).
            var tokenReq = new HttpRequestMessage(HttpMethod.Post,
                $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"]     = clientId,
                    ["client_secret"] = clientSecret,
                    ["scope"]         = "https://graph.microsoft.com/.default",
                    ["grant_type"]    = "client_credentials"
                })
            };

            var tokenResp = await http.SendAsync(tokenReq);
            var tokenJson = await tokenResp.Content.ReadAsStringAsync();
            if (!tokenResp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Gagal memperoleh token Graph: {tokenJson}");

            using var tokenDoc = JsonDocument.Parse(tokenJson);
            var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();

            // 2) Susun penerima & kirim email lewat Graph sendMail.
            var toRecipients = to
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => new { emailAddress = new { address = e } })
                .ToArray();

            var ccRecipients = (cc ?? Enumerable.Empty<string>())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => new { emailAddress = new { address = e } })
                .ToArray();

            var payload = new
            {
                message = new
                {
                    subject,
                    body = new { contentType = "Text", content = body },
                    toRecipients,
                    ccRecipients
                },
                saveToSentItems = false
            };

            var mailReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            mailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var mailResp = await http.SendAsync(mailReq);     // sukses → 202 Accepted
            if (!mailResp.IsSuccessStatusCode)
            {
                var err = await mailResp.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Gagal mengirim email (Graph): {(int)mailResp.StatusCode} {err}");
            }
        }
    }
}
