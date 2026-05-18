namespace ffis_web_api.Models
{
    public class DriverUser
    {
        public Guid Oid { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Vendor { get; set; }
        public bool Active { get; set; }
        public string SignNo { get; set; }
        public string Position { get; set; }
    }
}
