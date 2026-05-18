using System;
using System.Collections.Generic;

namespace ffis_web_api.Models
{
    public class P2HHeader
    {
        public Guid Oid { get; set; }
        public string? TransactionNo { get; set; }
        public string DriverCode { get; set; }
        public string DriverName { get; set; }
        public string VehicleNo { get; set; }
        public int Odometer { get; set; }
        public string OperationalArea { get; set; }
        public string TransporterName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime ActivityDate { get; set; }
        public DateTime? ChecklistStartTime { get; set; }
        public DateTime? ChecklistEndTime { get; set; }
        public DateTime? GateInTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public int? GateInOdometer { get; set; }
        public string? Status { get; set; } // Draft, Submitted, Waiting Approval, Approved, Closed, BATAL
        public bool NeedsApproval { get; set; }
        public bool IsAllowedToExit { get; set; }
        public string? QrCodeData { get; set; }
        public string? SupervisorNote { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedTime { get; set; }
        public List<P2HDetail> Details { get; set; } = new();
        public List<P2HApprovalLog> Logs { get; set; } = new();
    }

    public class P2HDetail
    {
        public Guid Oid { get; set; }
        public Guid HeaderOid { get; set; }
        public Guid QuestionOid { get; set; }
        public string Answer { get; set; } // YA, TIDAK, NA
        public string? Remarks { get; set; }
        public string? QuestionText { get; set; } // Virtual for display
        public string? Category { get; set; }
    }

    public class MasterP2HChecklist
    {
        public Guid Oid { get; set; }
        public string ItemCode { get; set; }
        public string Category { get; set; }
        public string Question { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class P2HApprovalLog
    {
        public Guid Oid { get; set; }
        public Guid HeaderOid { get; set; }
        public string Status { get; set; }
        public string? ActionBy { get; set; }
        public DateTime ActionTime { get; set; }
        public string? Notes { get; set; }
    }

    public class GateActivity
    {
        public Guid Oid { get; set; }
        public string TransactionNo { get; set; }
        public string VehicleNo { get; set; }
        public string DriverName { get; set; }
        public DateTime ExitTime { get; set; }
        public DateTime? GateInTime { get; set; }
        public string ExitBy { get; set; }
        public string EntryBy { get; set; }
        public string Status { get; set; }
    }

    public class TruckStatus
    {
        public string VehicleNo { get; set; } = string.Empty;
        public int LastOdometer { get; set; }
        public DateTime LastActivity { get; set; }
        public string Status { get; set; } = string.Empty; // Normal, Service Soon, Overdue
        public int NextServiceKM { get; set; }
        public int RemainingKM { get; set; }
    }

    public class P2HNotification
    {
        public Guid Oid { get; set; }
        public string Nik { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public string? AttachmentUrl { get; set; }
        public string? TransactionNo { get; set; }
        public string? Type { get; set; }
    }

    public class AdminUserToken
    {
        public string Username { get; set; } = string.Empty;
        public string DriverCode { get; set; } = string.Empty;
        public string FcmToken { get; set; } = string.Empty;
    }
}
