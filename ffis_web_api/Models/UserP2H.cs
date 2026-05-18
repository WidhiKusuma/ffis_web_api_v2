using System.Text.Json.Serialization;

namespace ffis_web_api.Models
{
    public class UserP2H
    {
        public Guid Oid { get; set; }
        public string DriverCode { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string LevelUser { get; set; } = "Driver";
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class RegisterRequestDTO
    {
        [JsonPropertyName("driverCode")]
        public string DriverCode { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; }

        [JsonPropertyName("levelUser")]
        public string LevelUser { get; set; } = "Driver";
    }

    public class ResetPasswordRequestDTO
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("newPassword")]
        public string NewPassword { get; set; }
    }
}
