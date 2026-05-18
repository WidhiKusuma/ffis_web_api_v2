using System.Text.Json.Serialization;

namespace ffis_web_api.Models
{
    public class ExternalApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("data")]
        public List<ExternalDriverResponse> Data { get; set; }
    }

    public class ExternalDriverResponse
    {
        [JsonPropertyName("nik")]
        public string Nik { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; }

        [JsonPropertyName("mobileNumber")]
        public string MobileNumber { get; set; }

        [JsonPropertyName("branch")]
        public string Branch { get; set; }

        [JsonPropertyName("servicesType")]
        public string ServicesType { get; set; }

        [JsonPropertyName("agreementLicensesNumber")]
        public string AgreementLicensesNumber { get; set; }

        [JsonPropertyName("expiryDate")]
        public string ExpiryDate { get; set; }

        [JsonPropertyName("noPol")]
        public string? NoPol { get; set; }
    }
}
