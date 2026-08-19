using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ffis_web_api.Services;

namespace ffis_web_api.Controllers.CmsYlid
{
    [Authorize]
    [ApiController]
    [Route("api/cms_ylid/send-detail")]
    public class CmsYlidMailController : ControllerBase
    {
        private readonly IConfiguration  _configuration;
        private readonly GraphMailService _mail;

        public CmsYlidMailController(IConfiguration configuration, GraphMailService mail)
        {
            _configuration = configuration;
            _mail = mail;
        }

        // POST api/cms_ylid/send-detail
        // App mengirim data rute + pemohon; server menyusun pesan dari template
        // (appsettings) lalu mengirim email via Microsoft Graph. Tidak perlu aplikasi
        // email di perangkat pengguna.
        [HttpPost]
        public async Task<IActionResult> SendDetail([FromBody] SendDetailRequest req)
        {
            if (req == null) return BadRequest(new { message = "Data permintaan kosong." });

            // Penerima (To) = email tim Transport dari config.
            var contacts = _configuration.GetSection("CmsYlid:TransportContacts")
                               .Get<List<TransportContact>>() ?? new();
            var to = contacts.Select(c => c.Email)
                .Where(e => !string.IsNullOrWhiteSpace(e) && e.Contains('@'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (to.Count == 0)
                return BadRequest(new { message = "Alamat email tim Transport belum diatur di server." });

            // Template subject & body dari config (kosong → default).
            var subjectTpl = _configuration["CmsYlid:MessageSubject"];
            var bodyTpl    = _configuration["CmsYlid:MessageBody"];
            if (string.IsNullOrWhiteSpace(subjectTpl)) subjectTpl = DefaultSubject;
            if (string.IsNullOrWhiteSpace(bodyTpl))    bodyTpl    = DefaultBody;

            string subject = ApplyPlaceholders(subjectTpl, req);
            string body    = ApplyPlaceholders(bodyTpl, req);

            // Cc = email pemohon (dari user login) bila valid.
            var cc = new List<string>();
            if (!string.IsNullOrWhiteSpace(req.PemohonEmail) && req.PemohonEmail.Contains('@'))
                cc.Add(req.PemohonEmail);

            try
            {
                await _mail.SendAsync(subject, body, to, cc);
                return Ok(new { message = "Permintaan detail berhasil dikirim ke tim Transport." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Gagal mengirim email: " + ex.Message });
            }
        }

        private static string ApplyPlaceholders(string tpl, SendDetailRequest r)
        {
            string jenisUsaha = string.IsNullOrWhiteSpace(r.JenisUsaha) ? "Semua" : r.JenisUsaha;
            string waktu = DateTime.Now.ToString("dddd, d MMMM yyyy, HH.mm", new CultureInfo("id-ID"));

            string pemohon = string.IsNullOrWhiteSpace(r.Pemohon) ? "-" : r.Pemohon;
            string pemohonEmailRaw = r.PemohonEmail ?? "";
            string pemohonEmail    = string.IsNullOrWhiteSpace(pemohonEmailRaw) ? "" : $" ({pemohonEmailRaw})";

            return tpl
                .Replace("{Pemohon}", pemohon)
                .Replace("{PemohonEmail}", pemohonEmail)
                .Replace("{PemohonEmailRaw}", pemohonEmailRaw)
                .Replace("{Pickup}", r.Pickup ?? "")
                .Replace("{Destination}", r.Destination ?? "")
                .Replace("{VehicleType}", r.VehicleType ?? "")
                .Replace("{JenisUsaha}", jenisUsaha)
                .Replace("{Waktu}", waktu);
        }

        private const string DefaultSubject =
            "Permintaan Detail Vendor — {Pickup} → {Destination} ({VehicleType})";

        private const string DefaultBody =
            "Yth. Tim Admin Transport,\n\n" +
            "Saya membutuhkan informasi detail vendor untuk rute berikut:\n\n" +
            "• Pemohon : {Pemohon}{PemohonEmail}\n" +
            "• Asal : {Pickup}\n" +
            "• Tujuan : {Destination}\n" +
            "• Jenis Kendaraan : {VehicleType}\n" +
            "• Jenis Usaha : {JenisUsaha}\n\n" +
            "Mohon bantuannya untuk memberikan detail vendor beserta harga terbaik untuk rute ini.\n\n" +
            "Terima kasih.\n\n" +
            "Hormat saya,\n{Pemohon}\n\n" +
            "---\n" +
            "Dikirim melalui CMS Trucking YLID Mobile\n" +
            "{Waktu}";
    }

    public class SendDetailRequest
    {
        public string Pickup       { get; set; } = string.Empty;
        public string Destination  { get; set; } = string.Empty;
        public string VehicleType  { get; set; } = string.Empty;
        public string JenisUsaha   { get; set; } = string.Empty;
        public string Pemohon      { get; set; } = string.Empty;
        public string PemohonEmail { get; set; } = string.Empty;
    }
}
