-- Tambah kolom baru di P2HHeader
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('P2HHeader') AND name = 'ActivityDate')
    ALTER TABLE P2HHeader ADD ActivityDate DATETIME;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('P2HHeader') AND name = 'ChecklistStartTime')
    ALTER TABLE P2HHeader ADD ChecklistStartTime DATETIME;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('P2HHeader') AND name = 'ChecklistEndTime')
    ALTER TABLE P2HHeader ADD ChecklistEndTime DATETIME;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('P2HHeader') AND name = 'GateInTime')
    ALTER TABLE P2HHeader ADD GateInTime DATETIME;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('P2HHeader') AND name = 'GateInOdometer')
    ALTER TABLE P2HHeader ADD GateInOdometer INT;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('P2HHeader') AND name = 'NeedsApproval')
    ALTER TABLE P2HHeader ADD NeedsApproval BIT DEFAULT 1;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('P2HHeader') AND name = 'SupervisorNote')
    ALTER TABLE P2HHeader ADD SupervisorNote NVARCHAR(MAX);

-- Tambah kolom baru di P2HDetail (jika belum ada)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('P2HDetail') AND name = 'Remarks')
BEGIN
    ALTER TABLE P2HDetail ADD Remarks NVARCHAR(MAX);
END

-- Buat tabel Log Aktivitas Persetujuan
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[P2HApprovalLog]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[P2HApprovalLog] (
        [Oid] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [HeaderOid] UNIQUEIDENTIFIER NOT NULL,
        [Status] NVARCHAR(50) NOT NULL, -- Submitted, Approved, Rejected, BATAL, etc.
        [ActionBy] NVARCHAR(200),
        [ActionTime] DATETIME DEFAULT GETDATE(),
        [Notes] NVARCHAR(MAX),
        CONSTRAINT FK_P2HApprovalLog_Header FOREIGN KEY (HeaderOid) REFERENCES P2HHeader(Oid)
    );
END
GO
