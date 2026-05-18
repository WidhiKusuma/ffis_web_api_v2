using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ffis_web_api.Models
{
    public class LoginRequestDTO
    {
        [Required]
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}