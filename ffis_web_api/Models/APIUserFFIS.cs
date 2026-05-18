using System; // Tambahkan namespace ini

namespace ffis_web_api.Models
{
    public class APIUserFFIS
    {
        // Data Login (Input/Output)
        public string Username { get; set; }
        public string Password { get; set; }

        // Data dari SP (Output)
        public Guid? Id { get; set; } // Oid (uniqueidentifier -> Guid)
        public string? Role { get; set; } = "User"; // Role diset default atau diisi jika ada GroupName

        // Data Tambahan dari SP sp_FFIS_OFF_Dashboard_Login
        public string? nik { get; set; }
        public string? fullname { get; set; }
        public Guid? Position { get; set; }
        public string? Section { get; set; }
        public Guid? Division { get; set; }
        public string? Location { get; set; }
        public string? username_AD { get; set; }
        public string? email { get; set; }
        public DateTime? CreateDate { get; set; } // CreateDate
        public Guid? Employees { get; set; } // Employees (uniqueidentifier -> Guid)

        // StoredPassword, CreateBy, ModifiedBy, ModifiedDate tidak dimasukkan ke sini untuk keamanan/simplisitas response
    }
}