using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Security.Cryptography;
using ffis_web_api.Models;
using System.DirectoryServices.AccountManagement;

namespace ffis_web_api.Repositories
{
    public class ITStockRepository
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public ITStockRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("ITStockDB");
        }

        #region Helper: Encrypt/Decrypt
        private string EncryptAes(string clearText)
        {
            string encryptionKey = _configuration["EncryptionKey"] ?? "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);

            using (Aes encryptor = Aes.Create())
            {
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

        #region Helper: AD Authentication
        private bool IsAuthenticated(string username, string password)
        {
            try
            {
                // Menggunakan IP yang sama sesuai instruksi
                using (var context = new PrincipalContext(ContextType.Domain, "172.19.160.4"))
                {
                    return context.ValidateCredentials(username, password);
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion

        public ITAdminUser Login(string username, string password)
        {
            bool isAuthenticatedAD = IsAuthenticated(username, password);
            int flag = isAuthenticatedAD ? 1 : 0;
            // Jika AD authenticated, password tidak perlu dikirim (sesuai logika SP)
            string passwordToUse = flag == 0 ? EncryptAes(password) : string.Empty;

            var query = "sp_IT_STOCK_Login";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", passwordToUse);
                command.Parameters.AddWithValue("@Flag", flag);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var user = new ITAdminUser
                        {
                            kode_karyawan = reader.IsDBNull(reader.GetOrdinal("kode_karyawan")) ? null : reader.GetValue(reader.GetOrdinal("kode_karyawan")).ToString(),
                            fullname = reader.IsDBNull(reader.GetOrdinal("fullname")) ? null : reader.GetValue(reader.GetOrdinal("fullname")).ToString(),
                            nik = reader.IsDBNull(reader.GetOrdinal("nik")) ? null : reader.GetValue(reader.GetOrdinal("nik")).ToString(),
                            tittle = reader.IsDBNull(reader.GetOrdinal("tittle")) ? null : reader.GetValue(reader.GetOrdinal("tittle")).ToString(),
                            division = reader.IsDBNull(reader.GetOrdinal("division")) ? null : reader.GetValue(reader.GetOrdinal("division")).ToString(),
                            section = reader.IsDBNull(reader.GetOrdinal("section")) ? null : reader.GetValue(reader.GetOrdinal("section")).ToString(),
                            nama_branch = reader.IsDBNull(reader.GetOrdinal("nama_branch")) ? null : reader.GetValue(reader.GetOrdinal("nama_branch")).ToString(),
                            status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetValue(reader.GetOrdinal("status")).ToString(),
                            email_karyawan = reader.IsDBNull(reader.GetOrdinal("email_karyawan")) ? null : reader.GetValue(reader.GetOrdinal("email_karyawan")).ToString(),
                            username = reader.IsDBNull(reader.GetOrdinal("username")) ? null : reader.GetValue(reader.GetOrdinal("username")).ToString(),
                            password = reader.IsDBNull(reader.GetOrdinal("password")) ? null : reader.GetValue(reader.GetOrdinal("password")).ToString(),
                            level_user = reader.IsDBNull(reader.GetOrdinal("level_user")) ? null : reader.GetValue(reader.GetOrdinal("level_user")).ToString(),
                            signature = reader.IsDBNull(reader.GetOrdinal("signature")) ? null : reader.GetValue(reader.GetOrdinal("signature")).ToString(),
                            size = reader.IsDBNull(reader.GetOrdinal("size")) ? null : reader.GetValue(reader.GetOrdinal("size")).ToString(),
                            username_AD = reader.IsDBNull(reader.GetOrdinal("username_AD")) ? null : reader.GetValue(reader.GetOrdinal("username_AD")).ToString(),
                            user_approval_yunas = reader.IsDBNull(reader.GetOrdinal("user_approval_yunas")) ? null : reader.GetValue(reader.GetOrdinal("user_approval_yunas")).ToString(),
                            user_request_system = reader.IsDBNull(reader.GetOrdinal("user_request_system")) ? null : reader.GetValue(reader.GetOrdinal("user_request_system")).ToString(),
                            Role = "ITAdmin"
                        };

                        // Jika login via AD, gunakan username_AD sebagai identitas utama jika tersedia
                        if (isAuthenticatedAD && !string.IsNullOrEmpty(user.username_AD))
                        {
                            user.username = user.username_AD;
                        }

                        return user;
                    }
                }
            }

            return null;
        }
    }
}
