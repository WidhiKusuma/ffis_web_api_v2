using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Security.Cryptography;
using ffis_web_api.Models;
using System.DirectoryServices.AccountManagement;

namespace ffis_web_api.Repositories
{
    public class UserRepository
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        // Constructor perlu IConfiguration untuk mendapatkan ConnectionString dan key enkripsi
        public UserRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("FFISDB");
        }

        #region Helper: Encrypt/Decrypt (Diperlukan karena logika SP menggunakan password terenkripsi)
        // PERHATIAN: Fungsi ini adalah ENKRIPSI, BUKAN DEKRIPSI, meskipun namanya mungkin "Decrypt"
        // di kode sumber yang Anda berikan. Fungsinya mengenkripsi plaintext menjadi Base64.
        // Kita namakan EncryptAes agar lebih sesuai dengan fungsionalitasnya.
        private string EncryptAes(string clearText)
        {
            // Pastikan key ada di konfigurasi
            string encryptionKey = _configuration["EncryptionKey"] ?? "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);

            using (Aes encryptor = Aes.Create())
            {
                // Salt dan Iteration Count yang hardcoded: PRAKTIK BURUK UNTUK KEAMANAN
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(encryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        #endregion

        #region Helper: AD Authentication (Diperlukan untuk logika login)
        // PERHATIAN: Implementasi ini adalah MOCK/PLACEHOLDER.
        // Otentikasi Active Directory yang sebenarnya membutuhkan System.DirectoryServices
        // atau pustaka yang setara dan konfigurasi domain.
        private bool IsAuthenticated(string username, string password)
        {
            // Ganti ini dengan logika AD Anda yang sesungguhnya.
            // Contoh menggunakan System.DirectoryServices.AccountManagement (membutuhkan referensi)

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, "172.19.160.4"))
                {
                    return context.ValidateCredentials(username, password);
                }
            }
            catch
            {
                return false;
            }


            // Untuk demonstrasi, kita hanya memalsukan (mock) jika username mengandung "AD"
            //return username.Contains("AD", StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        public APIUserFFIS GetUserByUsernameAndPassword(string username, string password)
        {
            // 1. Tentukan Flag dan Password yang akan digunakan
            bool isAuthenticatedAD = IsAuthenticated(username, password);
            int flag = isAuthenticatedAD ? 1 : 0;
            string passwordToUse = flag == 0 ? EncryptAes(password) : string.Empty; // Enkripsi hanya jika non-AD

            var query = "sp_FFIS_OFF_Dashboard_Login";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.StoredProcedure;

                // Parameter SP
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", passwordToUse);
                command.Parameters.AddWithValue("@Flag", flag);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Memetakan hasil dari SP ke APIUserFFIS
                        var user = new APIUserFFIS
                        {
                            // Oid (Guid)
                            Id = reader.IsDBNull(reader.GetOrdinal("Oid"))
                                  ? (Guid?)null
                                  : reader.GetGuid(reader.GetOrdinal("Oid")),

                            // String Columns
                            nik = reader.IsDBNull(reader.GetOrdinal("nik")) ? null : reader.GetString(reader.GetOrdinal("nik")),
                            fullname = reader.IsDBNull(reader.GetOrdinal("fullname")) ? null : reader.GetString(reader.GetOrdinal("fullname")),
                            Position = reader.IsDBNull(reader.GetOrdinal("Position"))
                                 ? (Guid?)null
                                 : reader.GetGuid(reader.GetOrdinal("Position")),
                            Section = reader.IsDBNull(reader.GetOrdinal("Section")) ? null : reader.GetString(reader.GetOrdinal("Section")),
                            Division = reader.IsDBNull(reader.GetOrdinal("Division"))
                                 ? (Guid?)null
                                 : reader.GetGuid(reader.GetOrdinal("Division")),
                            Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
                            Username = reader.IsDBNull(reader.GetOrdinal("UserName")) ? null : reader.GetString(reader.GetOrdinal("UserName")),
                            Password = reader.IsDBNull(reader.GetOrdinal("StoredPassword")) ? null : reader.GetString(reader.GetOrdinal("StoredPassword")),
                            username_AD = reader.IsDBNull(reader.GetOrdinal("username_AD")) ? null : reader.GetString(reader.GetOrdinal("username_AD")),
                            email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),

                            // Tambahan dari Session.Add: CreateDate (DateTime)
                            CreateDate = reader.IsDBNull(reader.GetOrdinal("CreateDate"))
                                 ? (DateTime?)null
                                 : reader.GetDateTime(reader.GetOrdinal("CreateDate")),

                            // Tambahan dari Session.Add: Employees (Guid)
                            Employees = reader.IsDBNull(reader.GetOrdinal("Employees"))
                                 ? (Guid?)null
                                 : reader.GetGuid(reader.GetOrdinal("Employees")),

                            // Role (Dummy)
                            Role = "AuthenticatedUser"
                        };

                        // Gunakan username_AD jika ada, jika tidak, gunakan UserName sebagai Claim Subject
                        user.Username = user.username_AD ?? user.Username;

                        return user;
                    }
                }
            }

            return null;
        }
    }
}