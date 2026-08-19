using Dapper;
using Npgsql;
using System.Data;
using System.DirectoryServices.AccountManagement;
using System.Security.Cryptography;
using ffis_web_api.Models.CmsYlid;

namespace ffis_web_api.Repositories
{
    public interface ICmsYlidRepository
    {
        Task<CmsYlidUserDto?> LoginAsync(string username, string password);
        Task<List<string>> GetPickupsAsync(string division);
        Task<List<string>> GetDestinationsAsync(string pickup, string division);
        Task<CmsYlidSearchResult> SearchPricesAsync(CmsYlidSearchRequest req);
    }

    public class CmsYlidRepository : ICmsYlidRepository
    {
        private readonly string _connStr;

        // ── Konfigurasi LDAP (Active Directory) ───────────────────────────────────
        private readonly bool   _ldapEnabled;
        private readonly string _ldapServer;
        private readonly string _ldapPrefix;

        public CmsYlidRepository(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("cms_ylid")
                       ?? throw new InvalidOperationException("ConnectionStrings:cms_ylid belum dikonfigurasi.");

            var ldap = configuration.GetSection("CmsYlid:Ldap");
            _ldapEnabled = ldap.GetValue("Enabled", false);
            _ldapServer  = ldap.GetValue("Server", "") ?? "";
            _ldapPrefix  = ldap.GetValue("UsernamePrefix", "YLID-") ?? "YLID-";
        }

        private IDbConnection Db() => new NpgsqlConnection(_connStr);

        // Normalisasi divisi: "Semua"/"ALL"/null -> "" (tanpa filter).
        private static string NormalizeDivision(string? division)
        {
            if (string.IsNullOrWhiteSpace(division)) return "";
            if (string.Equals(division, "ALL", StringComparison.OrdinalIgnoreCase)) return "";
            if (string.Equals(division, "Semua", StringComparison.OrdinalIgnoreCase)) return "";
            return division.Trim();
        }

        // ── Password helpers (PBKDF2-SHA256, sama dgn aplikasi mobile) ────────────
        private static string HashPassword(string password, string salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, Convert.FromBase64String(salt), 10000, HashAlgorithmName.SHA256);
            return Convert.ToBase64String(pbkdf2.GetBytes(32));
        }

        private static bool VerifyPassword(string password, string hash, string salt)
            => HashPassword(password, salt) == hash;

        // ── Auth ─────────────────────────────────────────────────────────────────
        public async Task<CmsYlidUserDto?> LoginAsync(string username, string password)
        {
            // Mode hybrid: kalau LDAP aktif → verifikasi password ke Active Directory,
            // data Role/Division tetap dari cms_users (dicari via NIK).
            if (_ldapEnabled && OperatingSystem.IsWindows())
                return await LoginViaLdapAsync(username, password);

            // Fallback lokal: verifikasi hash di cms_users (dicari via username).
            using var db = Db();
            var row = await db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT * FROM sp_user_get_by_username(@p_username)",
                new { p_username = username });

            if (row == null) return null;

            string status = (string)(row.status ?? "");
            string hash   = (string)(row.password_hash ?? "");
            string salt   = (string)(row.password_salt ?? "");

            if (!string.Equals(status, "Aktif", StringComparison.OrdinalIgnoreCase)) return null;
            if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))           return null;
            if (!VerifyPassword(password, hash, salt))                              return null;

            return new CmsYlidUserDto
            {
                UserId   = (int)row.user_id,
                Username = (string)(row.username ?? ""),
                FullName = (string)(row.full_name ?? ""),
                Email    = (string)(row.email ?? ""),
                Role     = (string)(row.role ?? ""),
                Status   = status
            };
        }

        // AD memakai format akun "YLID-<NIK>". User mengetik username lengkap;
        // buang prefix untuk dapat NIK murni (lookup cms_users), tempel kembali
        // untuk bind ke Active Directory.
        private async Task<CmsYlidUserDto?> LoginViaLdapAsync(string username, string password)
        {
            string input = (username ?? "").Trim();
            string nik = input.StartsWith(_ldapPrefix, StringComparison.OrdinalIgnoreCase)
                ? input.Substring(_ldapPrefix.Length)
                : input;
            string adUsername = _ldapPrefix + nik;

            // User wajib terdaftar di cms_users (untuk Role & Division), dicari via NIK.
            using var db = Db();
            var row = await db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT user_id, username, full_name, email, role, status
                  FROM cms_users WHERE nik = @nik LIMIT 1",
                new { nik });

            if (row == null) return null;

            string status = (string)(row.status ?? "");
            if (!string.Equals(status, "Aktif", StringComparison.OrdinalIgnoreCase)) return null;

            if (!ValidateAdCredentials(adUsername, password)) return null;

            return new CmsYlidUserDto
            {
                UserId   = (int)row.user_id,
                Username = (string)(row.username ?? ""),
                FullName = (string)(row.full_name ?? ""),
                Email    = (string)(row.email ?? ""),
                Role     = (string)(row.role ?? ""),
                Status   = status
            };
        }

        // Verifikasi kredensial ke Active Directory. Windows-only.
        private bool ValidateAdCredentials(string adUsername, string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain, _ldapServer);
                return ctx.ValidateCredentials(adUsername, password);
            }
            catch
            {
                return false;
            }
        }

        // ── Pickups / Destinations ────────────────────────────────────────────────
        public async Task<List<string>> GetPickupsAsync(string division)
        {
            using var db = Db();
            var list = await db.QueryAsync<string>(
                "SELECT * FROM sp_cost_get_distinct_pickups(@p_division)",
                new { p_division = NormalizeDivision(division) });
            return list.Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        public async Task<List<string>> GetDestinationsAsync(string pickup, string division)
        {
            using var db = Db();
            var list = await db.QueryAsync<string>(
                "SELECT * FROM sp_cost_get_distinct_destinations(@p_pickup, @p_division)",
                new { p_pickup = pickup ?? "", p_division = NormalizeDivision(division) });
            return list.Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        // ── Search prices ─────────────────────────────────────────────────────────
        public async Task<CmsYlidSearchResult> SearchPricesAsync(CmsYlidSearchRequest req)
        {
            var result = new CmsYlidSearchResult
            {
                Pickup      = req.Pickup,
                Destination = req.Destination,
                VehicleType = req.VehicleType,
                JenisUsaha  = req.JenisUsaha,
                Division    = req.Division,
                OwnTruck    = req.OwnTruck
            };

            using var db = Db();

            string div = NormalizeDivision(req.Division);

            // 1) Tipe truk yang tersedia untuk rute & jenis kendaraan
            var truckTypes = await db.QueryAsync<string>(
                "SELECT * FROM sp_cost_search_truck_types(@p_pickup, @p_destination, @p_vehicle_type, @p_division)",
                new { p_pickup = req.Pickup, p_destination = req.Destination, p_vehicle_type = req.VehicleType, p_division = div });
            result.TruckTypes = truckTypes.Where(t => !string.IsNullOrEmpty(t)).ToList();

            // 2) Harga per vendor
            string jf = (req.JenisUsaha == "Semua" || string.IsNullOrEmpty(req.JenisUsaha)) ? "" : req.JenisUsaha;

            var priceRows = (await db.QueryAsync<dynamic>(
                "SELECT * FROM sp_cost_search_prices(@p_pickup, @p_destination, @p_vehicle_type, @p_jenis_usaha, @p_division)",
                new
                {
                    p_pickup       = req.Pickup,
                    p_destination  = req.Destination,
                    p_vehicle_type = req.VehicleType,
                    p_jenis_usaha  = jf,
                    p_division     = div
                })).ToList();

            // Filter OWN TRUCK: hanya vendor Yusen (PT Yusen Logistics Indonesia / Yusen).
            if (req.OwnTruck)
            {
                priceRows = priceRows
                    .Where(r => ((string)(r.vendor_name ?? ""))
                        .IndexOf("yusen", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                // Sesuaikan kolom tipe truk dengan yang dimiliki vendor Yusen saja.
                result.TruckTypes = priceRows
                    .Where(r => !string.IsNullOrEmpty((string)(r.truck_type ?? "")))
                    .Select(r => new { Name = (string)r.truck_type, Sort = (int)r.sort_order })
                    .GroupBy(x => x.Name)
                    .OrderBy(g => g.First().Sort)
                    .Select(g => g.Key)
                    .ToList();
            }

            var dict = new Dictionary<int, CmsYlidVendorPriceRow>();
            foreach (var r in priceRows)
            {
                int vendorId = (int)r.vendor_id;
                if (!dict.ContainsKey(vendorId))
                {
                    var row = new CmsYlidVendorPriceRow
                    {
                        VendorId       = vendorId,
                        VendorName     = (string)(r.vendor_name ?? ""),
                        VendorDivision = (string)(r.vendor_division ?? ""),
                        JenisUsaha     = (string)(r.jenis_usaha ?? "")
                    };
                    foreach (var tt in result.TruckTypes) row.Prices[tt] = null;
                    dict[vendorId] = row;
                }

                string  ttName = (string)(r.truck_type ?? "");
                decimal price  = r.price == null ? 0m : (decimal)r.price;
                if (!string.IsNullOrEmpty(ttName) && dict[vendorId].Prices.ContainsKey(ttName))
                    dict[vendorId].Prices[ttName] = price;
                dict[vendorId].Total += price;
            }
            result.Rows = dict.Values.OrderBy(v => v.VendorName).ToList();

            // 3) Min/Max per tipe truk
            foreach (var tt in result.TruckTypes)
            {
                var prices = result.Rows
                    .Where(r => r.Prices.ContainsKey(tt) && r.Prices[tt].HasValue && r.Prices[tt]!.Value > 0)
                    .Select(r => r.Prices[tt]!.Value)
                    .ToList();
                if (prices.Any())
                {
                    result.MinPrices[tt] = prices.Min();
                    result.MaxPrices[tt] = prices.Max();
                }
            }

            // 4) Vendor termurah / termahal (total)
            var withData = result.Rows.Where(r => r.Total > 0).ToList();
            if (withData.Any())
            {
                result.MaxTotalVendorId = withData.OrderByDescending(r => r.Total).First().VendorId;
                result.MinTotalVendorId = withData.OrderBy(r => r.Total).First().VendorId;
            }

            return result;
        }
    }
}
