using System.Data;
using System.Data.SqlClient;
using ffis_web_api.Models;

namespace ffis_web_api.Repositories
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("FFISDB");
        }

        public APIUserFFIS GetUserByUsernameAndPassword(string username, string password)
        {
            var query = "sp_APIUser_FFIS";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.StoredProcedure;

                var usernameParam = command.CreateParameter();
                usernameParam.ParameterName = "@Username";
                usernameParam.Value = username;
                command.Parameters.Add(usernameParam);

                var passwordParam = command.CreateParameter();
                passwordParam.ParameterName = "@Password";
                passwordParam.Value = password;
                command.Parameters.Add(passwordParam);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
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
            }

            return null;
        }
    }
}
