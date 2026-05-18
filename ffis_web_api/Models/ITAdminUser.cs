using System;

namespace ffis_web_api.Models
{
    public class ITAdminUser
    {
        public string? kode_karyawan { get; set; }
        public string? fullname { get; set; }
        public string? nik { get; set; }
        public string? tittle { get; set; }
        public string? division { get; set; }
        public string? section { get; set; }
        public string? nama_branch { get; set; }
        public string? status { get; set; }
        public string? email_karyawan { get; set; }
        public string? username { get; set; }
        public string? password { get; set; }
        public string? level_user { get; set; }
        public string? signature { get; set; }
        public string? size { get; set; }
        public string? username_AD { get; set; }
        public string? user_approval_yunas { get; set; }
        public string? user_request_system { get; set; }

        // Field tambahan untuk JWT / Logic
        public string? Role { get; set; } = "ITAdmin";
    }
}
