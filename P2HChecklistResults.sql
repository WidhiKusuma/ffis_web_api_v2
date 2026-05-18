
-- Create Header Table for P2H Checklist Results
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[P2HHeader]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[P2HHeader] (
        [Oid] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [TransactionNo] NVARCHAR(50) UNIQUE NOT NULL,
        [DriverCode] NVARCHAR(50) NOT NULL,
        [DriverName] NVARCHAR(200),
        [VehicleNo] NVARCHAR(20) NOT NULL,
        [Odometer] INT,
        [OperationalArea] NVARCHAR(100),
        [TransporterName] NVARCHAR(200),
        [StartTime] DATETIME DEFAULT GETDATE(),
        [EndTime] DATETIME,
        [Status] NVARCHAR(20) DEFAULT 'Pending', -- Pending, Completed, Checked
        [QrCodeData] NVARCHAR(MAX),
        [IsAllowedToExit] BIT DEFAULT 0,
        [GateOfficerName] NVARCHAR(200),
        [ExitTime] DATETIME,
        [CreatedAt] DATETIME DEFAULT GETDATE()
    );
END
GO

-- Create Detail Table for P2H Checklist Answers
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[P2HDetail]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[P2HDetail] (
        [Oid] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [HeaderOid] UNIQUEIDENTIFIER NOT NULL,
        [QuestionOid] UNIQUEIDENTIFIER NOT NULL,
        [Answer] NVARCHAR(MAX), -- Yes/No/Text
        [CreatedAt] DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_P2HDetail_Header FOREIGN KEY (HeaderOid) REFERENCES P2HHeader(Oid),
        CONSTRAINT FK_P2HDetail_Question FOREIGN KEY (QuestionOid) REFERENCES MasterP2HChecklist(Oid)
    );
END
GO

-- Create a View for Excel-like Output (Flat Structure)
IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[v_P2HChecklistReport]'))
BEGIN
    DROP VIEW [dbo].[v_P2HChecklistReport];
END
GO

-- Note: A flat view with all 71+ questions as columns requires dynamic SQL or a long PIVOT.
-- For now, providing a basic relational view.
CREATE VIEW [dbo].[v_P2HChecklistReport] AS
SELECT 
    H.TransactionNo,
    H.DriverCode,
    H.DriverName,
    H.VehicleNo,
    H.StartTime,
    H.EndTime,
    H.Status,
    H.IsAllowedToExit,
    H.ExitTime,
    M.Category,
    M.Question,
    D.Answer
FROM P2HHeader H
JOIN P2HDetail D ON H.Oid = D.HeaderOid
JOIN MasterP2HChecklist M ON D.QuestionOid = M.Oid;
GO
