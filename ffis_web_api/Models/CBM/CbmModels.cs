namespace ffis_web_api.Models.CBM
{
    public class CbmLoginRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Employee, Driver, Admin
    }

    public class CbmUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class CbmLocation
    {
        public int Id { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string LocationType { get; set; } = string.Empty;
    }

    public class CbmLoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }
    }

    public class CbmCreateOrderRequest
    {
        public int UserId { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
    }

    public class CbmOrder
    {
        public string OrderId { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty; // CBM Car / CBM Send
        public string Status { get; set; } = string.Empty; // Pending, Assigned, InProgress, Completed
        public string PassengerName { get; set; } = string.Empty;
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
