using System.Data;
using ffis_web_api.Models;

namespace ffis_web_api.Repositories
{
    public class UserRepository
    {
        private readonly IDbConnection _dbConnection;

        public UserRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public APIUserFFIS GetUserByUsernameAndPassword(string username, string password)
        {
            // Menggunakan stored procedure untuk mendapatkan pengguna
            var query = "sp_APIUser_FFIS"; // Nama stored procedure

            using (var command = _dbConnection.CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.StoredProcedure;

                // Menambahkan parameter untuk stored procedure
                var usernameParam = command.CreateParameter();
                usernameParam.ParameterName = "@Username";
                usernameParam.Value = username;
                command.Parameters.Add(usernameParam);

                var passwordParam = command.CreateParameter();
                passwordParam.ParameterName = "@Password";
                passwordParam.Value = password;
                command.Parameters.Add(passwordParam);

                _dbConnection.Open();
                using (var reader = command.ExecuteReader())
                {
                    // Jika ada hasil, mengembalikan data pengguna
                    if (reader.Read())
                    {
                        return new APIUserFFIS
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Password = reader.GetString(2),
                            Role = reader.GetString(3)
                        };
                    }
                }
                _dbConnection.Close();
            }

            return null;
        }
    }
}
