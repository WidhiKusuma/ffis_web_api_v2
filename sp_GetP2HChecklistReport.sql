
-- Stored Procedure for P2H Checklist Report
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetP2HChecklistReport]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_GetP2HChecklistReport];
END
GO

CREATE PROCEDURE [dbo].[sp_GetP2HChecklistReport]
    @StartDate DATETIME = NULL,
    @EndDate DATETIME = NULL,
    @VehicleNo NVARCHAR(20) = NULL,
    @DriverCode NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        H.TransactionNo,
        H.DriverCode,
        H.DriverName,
        H.VehicleNo,
        H.Odometer,
        H.OperationalArea,
        H.TransporterName,
        H.StartTime,
        H.EndTime,
        H.Status,
        H.IsAllowedToExit,
        H.GateOfficerName,
        H.ExitTime,
        M.ItemCode,
        M.Category,
        M.Question,
        D.Answer
    FROM P2HHeader H
    JOIN P2HDetail D ON H.Oid = D.HeaderOid
    JOIN MasterP2HChecklist M ON D.QuestionOid = M.Oid
    WHERE 
        (@StartDate IS NULL OR H.StartTime >= @StartDate) AND
        (@EndDate IS NULL OR H.StartTime <= @EndDate) AND
        (@VehicleNo IS NULL OR H.VehicleNo = @VehicleNo) AND
        (@DriverCode IS NULL OR H.DriverCode = @DriverCode)
    ORDER BY H.StartTime DESC, M.DisplayOrder ASC;
END
GO
