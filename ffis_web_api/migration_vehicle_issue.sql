-- Migration: temuan P2H dilacak per kendaraan (nopol), bukan per approval/submission.
-- Satu baris = satu temuan terbuka/tertutup untuk kombinasi (nopol, item checklist).
-- Selama IsResolved = 0, driver tidak bisa menjawab nilai "OK" (YA/TIDAK tergantung
-- jenis pertanyaan) untuk item itu lagi saat submit P2H baru untuk nopol yang sama.

CREATE TABLE P2HVehicleIssue (
    Oid UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    VehicleNo NVARCHAR(50) NOT NULL,
    ItemCode NVARCHAR(50) NOT NULL,
    QuestionText NVARCHAR(500) NULL,
    Category NVARCHAR(100) NULL,
    Remarks NVARCHAR(500) NULL,          -- catatan driver saat temuan dilaporkan
    ReportedBy NVARCHAR(100) NULL,
    ReportedTime DATETIME NOT NULL DEFAULT GETDATE(),
    HeaderOid UNIQUEIDENTIFIER NULL,     -- submission P2H yang terakhir memicu/refresh temuan ini
    IsResolved BIT NOT NULL DEFAULT 0,
    ResolvedBy NVARCHAR(100) NULL,
    ResolvedTime DATETIME NULL,
    ResolutionNote NVARCHAR(500) NULL
);

CREATE INDEX IX_P2HVehicleIssue_Vehicle_Open ON P2HVehicleIssue (VehicleNo, ItemCode, IsResolved);
