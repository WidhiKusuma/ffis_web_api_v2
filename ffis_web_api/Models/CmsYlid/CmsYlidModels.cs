namespace ffis_web_api.Models.CmsYlid
{
    // ── Auth ─────────────────────────────────────────────────────────────────
    public class CmsYlidLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class CmsYlidUserDto
    {
        public int    UserId   { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email    { get; set; } = string.Empty;
        public string Role     { get; set; } = string.Empty;
        public string Status   { get; set; } = string.Empty;
    }

    public class CmsYlidLoginResponse
    {
        public bool           Success { get; set; }
        public string         Message { get; set; } = string.Empty;
        public string         Token   { get; set; } = string.Empty;
        public CmsYlidUserDto? User    { get; set; }
    }

    // ── Search ───────────────────────────────────────────────────────────────
    public class CmsYlidSearchRequest
    {
        public string Pickup      { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string VehicleType { get; set; } = "DRY";   // "DRY" / "RF" / "ISOTANK"
        public string JenisUsaha  { get; set; } = string.Empty; // "Semua"/""/"FTL"/...
        public string Division    { get; set; } = string.Empty; // "ALL"/""/"AFF"/"OFF"
        public bool   OwnTruck    { get; set; } = false;        // true = hanya vendor Yusen
    }

    public class CmsYlidVendorPriceRow
    {
        public int     VendorId       { get; set; }
        public string  VendorName     { get; set; } = string.Empty;
        public string  VendorDivision { get; set; } = string.Empty; // "AFF"/"OFF"
        public string  JenisUsaha     { get; set; } = string.Empty;
        public decimal Total          { get; set; }
        public Dictionary<string, decimal?> Prices { get; set; } = new();
    }

    public class CmsYlidSearchResult
    {
        public string Pickup      { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string JenisUsaha  { get; set; } = string.Empty;
        public string Division    { get; set; } = string.Empty;
        public bool   OwnTruck    { get; set; } = false;

        public List<string>                TruckTypes { get; set; } = new();
        public List<CmsYlidVendorPriceRow> Rows       { get; set; } = new();

        public Dictionary<string, decimal> MinPrices { get; set; } = new();
        public Dictionary<string, decimal> MaxPrices { get; set; } = new();

        public int MaxTotalVendorId { get; set; }
        public int MinTotalVendorId { get; set; }
    }
}
