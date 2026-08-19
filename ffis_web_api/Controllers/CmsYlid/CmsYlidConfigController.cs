using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ffis_web_api.Controllers.CmsYlid
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/cms_ylid/config")]
    public class CmsYlidConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CmsYlidConfigController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET api/cms_ylid/config
        [HttpGet]
        public IActionResult GetConfig()
        {
            var contacts = _configuration.GetSection("CmsYlid:TransportContacts")
                               .Get<List<TransportContact>>() ?? new();

            // Fallback ke nomor tunggal lama (kompatibilitas), jika daftar kosong
            if (contacts.Count == 0)
            {
                var single = _configuration["CmsYlid:TransportWhatsApp"];
                if (!string.IsNullOrWhiteSpace(single))
                    contacts.Add(new TransportContact { Name = "Tim Transport", Number = single });
            }

            // Template pesan (subject & body) dapat diatur dari appsettings.
            // Kosong → aplikasi memakai template default bawaannya.
            var messageSubject = _configuration["CmsYlid:MessageSubject"] ?? string.Empty;
            var messageBody    = _configuration["CmsYlid:MessageBody"] ?? string.Empty;

            return Ok(new CmsYlidConfig
            {
                TransportContacts = contacts,
                MessageSubject    = messageSubject,
                MessageBody       = messageBody
            });
        }
    }

    public class TransportContact
    {
        public string Name   { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Email  { get; set; } = string.Empty;
    }

    public class CmsYlidConfig
    {
        public List<TransportContact> TransportContacts { get; set; } = new();
        public string MessageSubject { get; set; } = string.Empty;
        public string MessageBody    { get; set; } = string.Empty;
    }
}
