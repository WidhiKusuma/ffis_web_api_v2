using Dapper;
using Npgsql;
using ffis_web_api.Models.CBM;
using System.Data;

namespace ffis_web_api.Repositories
{
    public interface ICbmRepository
    {
        Task<CbmLoginResponse?> LoginAsync(string phoneNumber);
        Task<IEnumerable<CbmLocation>> SearchLocationsAsync(string keyword);
        Task<string> CreateOrderAsync(int userId, string serviceType, string pickup, string dropoff);
        Task<IEnumerable<CbmOrder>> GetUserActiveOrdersAsync(int userId);
        Task<IEnumerable<CbmOrder>> GetPendingOrdersAsync();
        Task<bool> AssignDriverAsync(int orderId, int driverId);
    }

    public class CbmRepository : ICbmRepository
    {
        private readonly IDbConnection _db;

        public CbmRepository(IConfiguration configuration)
        {
            _db = new NpgsqlConnection(configuration.GetConnectionString("dbpath"));
        }

        public async Task<IEnumerable<CbmLocation>> SearchLocationsAsync(string keyword)
        {
            var query = "SELECT * FROM fn_cbm_search_locations(@Keyword)";
            var locations = await _db.QueryAsync<dynamic>(query, new { Keyword = keyword ?? "" });

            return locations.Select(l => new CbmLocation
            {
                Id = l.id,
                LocationName = l.location_name,
                Address = l.address,
                Latitude = l.latitude ?? 0,
                Longitude = l.longitude ?? 0,
                LocationType = l.location_type
            });
        }

        public async Task<CbmLoginResponse?> LoginAsync(string phoneNumber)
        {
            var query = "SELECT * FROM fn_cbm_login_by_phone(@Phone)";
            var user = await _db.QueryFirstOrDefaultAsync<dynamic>(query, new { Phone = phoneNumber });
            
            if (user != null)
            {
                return new CbmLoginResponse
                {
                    Success = true,
                    Role = user.user_role,
                    UserId = user.user_id,
                    Message = "Login successful",
                    Token = "cbm-jwt-token-placeholder" // Diubah menggunakan JWT asli jika dibutuhkan
                };
            }
            return null;
        }

        public async Task<string> CreateOrderAsync(int userId, string serviceType, string pickup, string dropoff)
        {
            var query = "SELECT fn_cbm_create_order(@UserId, @ServiceType, @Pickup, @Dropoff)";
            return await _db.ExecuteScalarAsync<string>(query, new { UserId = userId, ServiceType = serviceType, Pickup = pickup, Dropoff = dropoff });
        }

        public async Task<IEnumerable<CbmOrder>> GetUserActiveOrdersAsync(int userId)
        {
            var query = "SELECT * FROM fn_cbm_get_user_active_orders(@UserId)";
            return await _db.QueryAsync<CbmOrder>(query, new { UserId = userId });
        }

        public async Task<IEnumerable<CbmOrder>> GetPendingOrdersAsync()
        {
            var query = "SELECT * FROM fn_cbm_get_pending_orders()";
            return await _db.QueryAsync<CbmOrder>(query);
        }

        public async Task<bool> AssignDriverAsync(int orderId, int driverId)
        {
            var query = "SELECT fn_cbm_assign_driver(@OrderId, @DriverId)";
            return await _db.ExecuteScalarAsync<bool>(query, new { OrderId = orderId, DriverId = driverId });
        }
    }
}
