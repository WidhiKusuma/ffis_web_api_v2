using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Security.Cryptography;
using ffis_web_api.Models;
using Dapper;

namespace ffis_web_api.Repositories
{
    public class DriverRepository
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public DriverRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("FFISDB");
            EnsureNotificationTableExists();
        }

        private void EnsureNotificationTableExists()
        {
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    db.Open();
                    var sql = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[P2HNotification]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[P2HNotification] (
                                [Oid] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                                [Nik] VARCHAR(50) NOT NULL,
                                [Title] NVARCHAR(250) NOT NULL,
                                [Body] NVARCHAR(MAX) NOT NULL,
                                [ReceivedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                                [IsRead] BIT NOT NULL DEFAULT 0,
                                [AttachmentUrl] NVARCHAR(500) NULL,
                                [TransactionNo] VARCHAR(50) NULL,
                                [Type] VARCHAR(50) NULL
                            );
                        END";
                    db.Execute(sql);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DriverRepository] Error ensuring notification table exists: {ex.Message}");
            }
        }

        private string EncryptAes(string clearText)
        {
            try
            {
                string encryptionKey = _configuration["EncryptionKey"] ?? "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
                byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);

                using (Aes encryptor = Aes.Create())
                {
                    // Menentukan HashAlgorithm dan Iterations secara eksplisit untuk kompatibilitas .NET Core/5/6/7
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(encryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }, 1000, HashAlgorithmName.SHA1);
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(clearBytes, 0, clearBytes.Length);
                            cs.Close();
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Encryption error: {ex.Message}");
            }
        }

        public List<DriverUser> SearchDrivers(string name)
        {
            var results = new List<DriverUser>();
            var query = "SELECT TOP 10 Oid, Code, Name FROM Driver WHERE Name LIKE @Name AND Active = 1";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Name", $"%{name}%");
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new DriverUser
                        {
                            Oid = reader.GetGuid(reader.GetOrdinal("Oid")),
                            Code = reader["Code"].ToString(),
                            Name = reader["Name"].ToString()
                        });
                    }
                }
            }
            return results;
        }

        public DriverUser GetDriverByCode(string code)
        {
            var query = "SELECT Oid, Code, Name FROM Driver WHERE Code = @Code AND Active = 1";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Code", code);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new DriverUser
                        {
                            Oid = reader.GetGuid(reader.GetOrdinal("Oid")),
                            Code = reader["Code"].ToString(),
                            Name = reader["Name"].ToString()
                        };
                    }
                }
            }
            return null;
        }

        public List<UserP2H> GetAllUsers()
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "SELECT Oid, DriverCode, Username, FullName, LevelUser, CreatedAt, IsActive FROM UserP2H ORDER BY CreatedAt DESC";
                return db.Query<UserP2H>(sql).ToList();
            }
        }

        public bool IsUserRegistered(string driverCode, string username)
        {
            var query = "SELECT COUNT(*) FROM UserP2H WHERE DriverCode = @DriverCode OR Username = @Username";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@DriverCode", driverCode);
                command.Parameters.AddWithValue("@Username", username);
                connection.Open();
                return (int)command.ExecuteScalar() > 0;
            }
        }

        public bool Register(UserP2H user)
        {
            var query = @"INSERT INTO UserP2H (Oid, DriverCode, Username, Password, FullName, LevelUser, CreatedAt, IsActive) 
                         VALUES (@Oid, @DriverCode, @Username, @Password, @FullName, @LevelUser, GETDATE(), 1)";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Oid", Guid.NewGuid());
                command.Parameters.AddWithValue("@DriverCode", user.DriverCode);
                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@Password", EncryptAes(user.Password));
                command.Parameters.AddWithValue("@FullName", user.FullName);
                command.Parameters.AddWithValue("@LevelUser", user.LevelUser ?? "Driver");

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
        }

        public UserP2H Login(string username, string password)
        {
            try
            {
                var encryptedPassword = EncryptAes(password);
                var query = "SELECT Oid, DriverCode, Username, FullName, LevelUser FROM UserP2H WHERE Username = @Username AND Password = @Password AND IsActive = 1";

                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", encryptedPassword);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new UserP2H
                            {
                                Oid = reader.GetGuid(reader.GetOrdinal("Oid")),
                                DriverCode = reader["DriverCode"]?.ToString(),
                                Username = reader["Username"]?.ToString(),
                                FullName = reader["FullName"]?.ToString(),
                                LevelUser = reader["LevelUser"]?.ToString()
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Re-throw dengan pesan yang lebih deskriptif untuk mempermudah debug jika 500 tetap terjadi
                throw new Exception($"Database Login Error: {ex.Message}");
            }
            return null;
        }

        public bool UpdatePassword(string identifier, string newPassword)
        {
            var query = "UPDATE UserP2H SET Password = @Password WHERE Username = @Identifier OR DriverCode = @Identifier";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Identifier", identifier);
                command.Parameters.AddWithValue("@Password", EncryptAes(newPassword));
                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
        }
        public List<MasterP2HChecklist> GetMasterChecklist()
        {
            using (var db = new SqlConnection(_connectionString))
            {
                return db.Query<MasterP2HChecklist>("SELECT * FROM MasterP2HChecklist ORDER BY DisplayOrder").ToList();
            }
        }

        public bool SaveP2H(P2HHeader header)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();
                using (var trans = db.BeginTransaction())
                {
                    try
                    {
                        header.Oid = Guid.NewGuid();
                        header.TransactionNo = "P2H-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                        header.StartTime = header.ChecklistStartTime ?? DateTime.Now;
                        
                        if (!header.NeedsApproval)
                        {
                            header.QrCodeData = header.TransactionNo + "|" + header.DriverCode + "|" + header.VehicleNo;
                        }
                        else 
                        {
                            header.QrCodeData = null;
                        }

                        var headerSql = @"INSERT INTO P2HHeader (Oid, TransactionNo, DriverCode, DriverName, VehicleNo, Odometer, OperationalArea, TransporterName, StartTime, Status, NeedsApproval, IsAllowedToExit, QrCodeData, ActivityDate, ChecklistStartTime, ChecklistEndTime) 
                                        VALUES (@Oid, @TransactionNo, @DriverCode, @DriverName, @VehicleNo, @Odometer, @OperationalArea, @TransporterName, @StartTime, @Status, @NeedsApproval, @IsAllowedToExit, @QrCodeData, @ActivityDate, @ChecklistStartTime, @ChecklistEndTime)";
                        
                        db.Execute(headerSql, header, trans);

                        foreach (var detail in header.Details)
                        {
                            detail.Oid = Guid.NewGuid();
                            detail.HeaderOid = header.Oid;
                            var detailSql = "INSERT INTO P2HDetail (Oid, HeaderOid, QuestionOid, Answer, Remarks) VALUES (@Oid, @HeaderOid, @QuestionOid, @Answer, @Remarks)";
                            db.Execute(detailSql, detail, trans);
                        }

                        UpsertVehicleIssues(db, trans, header);

                        // Log Aktivitas
                        var logSql = "INSERT INTO P2HApprovalLog (Oid, HeaderOid, Status, ActionBy, ActionTime, Notes) VALUES (NEWID(), @HeaderOid, @Status, @ActionBy, GETDATE(), @Notes)";
                        if (!header.NeedsApproval)
                        {
                            // Auto-approve: log langsung sebagai Approved oleh Sistem
                            db.Execute(logSql, new { HeaderOid = header.Oid, Status = "Approved", ActionBy = "Sistem", Notes = "Disetujui otomatis - checklist lengkap, tidak ada temuan" }, trans);
                        }
                        else
                        {
                            db.Execute(logSql, new { HeaderOid = header.Oid, Status = "Waiting Approval", ActionBy = header.DriverName, Notes = "Checklist diajukan oleh driver" }, trans);
                        }

                        trans.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool UpdateUserLevel(string identifier, string newLevel)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE UserP2H SET LevelUser = @Level WHERE Username = @Identifier OR DriverCode = @Identifier";
                var result = db.Execute(sql, new { Level = newLevel, Identifier = identifier });
                return result > 0;
            }
        }

        public List<P2HHeader> GetPendingApprovals()
        {
            using (var db = new SqlConnection(_connectionString))
            {
                // Fetch headers and join with details to make filtering easier on the UI
                var sql = "SELECT * FROM P2HHeader WHERE Status = 'Waiting Approval' ORDER BY StartTime DESC";
                var headers = db.Query<P2HHeader>(sql).ToList();
                foreach (var h in headers)
                {
                    // Also need to get question text if possible, but for now just the details
                    h.Details = db.Query<P2HDetail>("SELECT d.*, m.Question as QuestionText, m.Category, m.ItemCode FROM P2HDetail d JOIN MasterP2HChecklist m ON d.QuestionOid = m.Oid WHERE d.HeaderOid = @Oid", new { Oid = h.Oid }).ToList();
                }
                return headers;
            }
        }

        public P2HHeader GetByTransactionNo(string transactionNo)
        {
            if (string.IsNullOrEmpty(transactionNo)) return null;
            string cleanId = transactionNo.Trim();

            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM P2HHeader WHERE TransactionNo LIKE '%' + @TransactionNo + '%'";
                var header = db.QueryFirstOrDefault<P2HHeader>(sql, new { TransactionNo = cleanId });
                if (header != null)
                {
                    header.Details = db.Query<P2HDetail>("SELECT d.*, m.Question as QuestionText, m.Category, m.ItemCode FROM P2HDetail d JOIN MasterP2HChecklist m ON d.QuestionOid = m.Oid WHERE d.HeaderOid = @Oid", new { Oid = header.Oid }).ToList();
                    try
                    {
                        header.Logs = db.Query<P2HApprovalLog>("SELECT * FROM P2HApprovalLog WHERE HeaderOid = @Oid ORDER BY ActionTime DESC", new { Oid = header.Oid }).ToList();
                    }
                    catch { header.Logs = new List<P2HApprovalLog>(); }
                }
                return header;
            }
        }

        public P2HHeader GetP2HWithDetails(Guid oid)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var header = db.QueryFirstOrDefault<P2HHeader>("SELECT * FROM P2HHeader WHERE Oid = @Oid", new { Oid = oid });
                if (header != null)
                {
                    var detailsSql = "SELECT d.*, m.Question as QuestionText, m.Category, m.ItemCode FROM P2HDetail d JOIN MasterP2HChecklist m ON d.QuestionOid = m.Oid WHERE d.HeaderOid = @Oid";
                    header.Details = db.Query<P2HDetail>(detailsSql, new { Oid = oid }).ToList();
                    
                    try
                    {
                        header.Logs = db.Query<P2HApprovalLog>("SELECT * FROM P2HApprovalLog WHERE HeaderOid = @Oid ORDER BY ActionTime DESC", new { Oid = oid }).ToList();
                    }
                    catch { header.Logs = new List<P2HApprovalLog>(); }
                }
                return header;
            }
        }

        public bool ApproveP2H(Guid oid, string? note, string actionBy)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();
                using (var trans = db.BeginTransaction())
                {
                    try
                    {
                        var sql = @"UPDATE P2HHeader 
                                   SET Status = 'Approved', 
                                       IsAllowedToExit = 1, 
                                       EndTime = GETDATE(), 
                                       SupervisorNote = @Note,
                                       QrCodeData = TransactionNo + '|' + DriverCode + '|' + VehicleNo
                                   WHERE Oid = @Oid AND Status = 'Waiting Approval'";
                        var rows = db.Execute(sql, new { Oid = oid, Note = note }, trans);
                        if (rows == 0) return false;

                        // Log Aktivitas
                        var logSql = "INSERT INTO P2HApprovalLog (Oid, HeaderOid, Status, ActionBy, ActionTime, Notes) VALUES (NEWID(), @HeaderOid, 'Approved', @ActionBy, GETDATE(), @Notes)";
                        db.Execute(logSql, new { HeaderOid = oid, ActionBy = actionBy, Notes = string.IsNullOrWhiteSpace(note) ? null : note }, trans);

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool RejectP2H(Guid oid, string? note, string actionBy)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();
                using (var trans = db.BeginTransaction())
                {
                    try
                    {
                        var sql = @"UPDATE P2HHeader 
                                   SET Status = 'Rejected', 
                                       IsAllowedToExit = 0, 
                                       EndTime = GETDATE(), 
                                       SupervisorNote = @Note,
                                       QrCodeData = NULL
                                   WHERE Oid = @Oid AND Status = 'Waiting Approval'";
                        var rows = db.Execute(sql, new { Oid = oid, Note = note }, trans);
                        if (rows == 0) return false;

                        // Log Aktivitas
                        var logSql = "INSERT INTO P2HApprovalLog (Oid, HeaderOid, Status, ActionBy, ActionTime, Notes) VALUES (NEWID(), @HeaderOid, 'Rejected', @ActionBy, GETDATE(), @Notes)";
                        db.Execute(logSql, new { HeaderOid = oid, ActionBy = actionBy, Notes = string.IsNullOrWhiteSpace(note) ? null : note }, trans);

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool SyncFcmToken(string nik, string token)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();
                using (var trans = db.BeginTransaction())
                {
                    try
                    {
                        string? dbToken = string.IsNullOrEmpty(token) ? null : token;

                        // 1. Clear this token from ANY other users (One device, one active user)
                        if (dbToken != null)
                        {
                            db.Execute("UPDATE UserP2H SET FcmToken = NULL WHERE FcmToken = @Token", new { Token = dbToken }, trans);
                        }

                        // 2. Assign token to the current user (sets to NULL on logout)
                        var sql = "UPDATE UserP2H SET FcmToken = @Token WHERE DriverCode = @Nik OR Username = @Nik";
                        db.Execute(sql, new { Token = dbToken, Nik = nik }, trans);

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }

        public List<P2HHeader> GetHistoryByDriver(string? driverCode, DateTime? start = null, DateTime? end = null)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM P2HHeader WHERE (@DriverCode IS NULL OR TRIM(DriverCode) = TRIM(@DriverCode))";
                
                if (start.HasValue)
                {
                    start = start.Value.Date;
                    sql += " AND StartTime >= @Start";
                }
                if (end.HasValue)
                {
                    end = end.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    sql += " AND StartTime <= @End";
                }
                
                sql += " ORDER BY StartTime DESC";
                
                var headers = db.Query<P2HHeader>(sql, new { DriverCode = driverCode, Start = start, End = end }).ToList();
                
                foreach (var h in headers)
                {
                    try
                    {
                        h.Logs = db.Query<P2HApprovalLog>("SELECT * FROM P2HApprovalLog WHERE HeaderOid = @Oid ORDER BY ActionTime DESC", new { Oid = h.Oid }).ToList();
                    }
                    catch (Exception)
                    {
                        // Jika tabel log belum ada atau ada kolom yang beda, biarkan kosong agar riwayat utama tetap tampil
                        h.Logs = new List<P2HApprovalLog>();
                    }
                }
                
                return headers;
            }
        }

        public List<P2HDetail> GetP2HDetails(Guid headerOid)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "SELECT d.*, m.Question as QuestionText, m.Category, m.ItemCode FROM P2HDetail d JOIN MasterP2HChecklist m ON d.QuestionOid = m.Oid WHERE d.HeaderOid = @Oid";
                return db.Query<P2HDetail>(sql, new { Oid = headerOid }).ToList();
            }
        }

        public bool UpdateP2H(P2HHeader header)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();
                using (var trans = db.BeginTransaction())
                {
                    try
                    {
                        // Update Header (Hanya jika status masih Waiting Approval atau Draft)
                        var headerSql = @"UPDATE P2HHeader 
                                         SET Odometer = @Odometer, 
                                             Status = @Status, 
                                             NeedsApproval = @NeedsApproval,
                                             IsAllowedToExit = @IsAllowedToExit,
                                             ActivityDate = @ActivityDate,
                                             ChecklistStartTime = @ChecklistStartTime,
                                             ChecklistEndTime = @ChecklistEndTime
                                         WHERE Oid = @Oid AND Status = 'Waiting Approval'";
                        
                        var rows = db.Execute(headerSql, header, trans);
                        if (rows == 0) return false;

                        // Update Details - Kita hapus yang lama dan masukkan yang baru agar lebih simpel
                        db.Execute("DELETE FROM P2HDetail WHERE HeaderOid = @Oid", new { Oid = header.Oid }, trans);

                        foreach (var detail in header.Details)
                        {
                            detail.Oid = Guid.NewGuid();
                            detail.HeaderOid = header.Oid;
                            var detailSql = "INSERT INTO P2HDetail (Oid, HeaderOid, QuestionOid, Answer, Remarks) VALUES (@Oid, @HeaderOid, @QuestionOid, @Answer, @Remarks)";
                            db.Execute(detailSql, detail, trans);
                        }

                        UpsertVehicleIssues(db, trans, header);

                        // Log Aktivitas
                        var logSql = "INSERT INTO P2HApprovalLog (Oid, HeaderOid, Status, ActionBy, ActionTime, Notes) VALUES (NEWID(), @HeaderOid, @Status, @ActionBy, GETDATE(), @Notes)";
                        db.Execute(logSql, new { HeaderOid = header.Oid, Status = header.Status, ActionBy = header.DriverName, Notes = "Checklist diperbarui oleh driver" }, trans);

                        trans.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public int? GetLastOdometer(string nopol)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT TOP 1 COALESCE(GateInOdometer, Odometer) 
                            FROM P2HHeader 
                            WHERE VehicleNo = @Nopol AND (GateInOdometer IS NOT NULL OR Odometer IS NOT NULL)
                            ORDER BY StartTime DESC";
                return db.QueryFirstOrDefault<int?>(sql, new { Nopol = nopol });
            }
        }

        public P2HHeader? GetActiveChecklistByVehicle(string vehicleNo)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT TOP 1 * FROM P2HHeader
                            WHERE VehicleNo = @VehicleNo
                            AND (Status = 'Waiting Approval' OR Status = 'Approved' OR Status = 'Gate Out' OR (ExitTime IS NOT NULL AND GateInTime IS NULL))
                            ORDER BY StartTime DESC";
                return db.QueryFirstOrDefault<P2HHeader>(sql, new { VehicleNo = vehicleNo });
            }
        }

        public bool UpdateGateIn(string transactionNo, int odometer, string actionBy)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();
                using (var trans = db.BeginTransaction())
                {
                    try
                    {
                        var sql = @"UPDATE P2HHeader 
                                    SET GateInTime = GETDATE(), 
                                        GateInOdometer = @Odometer,
                                        Status = 'Closed'
                                    WHERE TransactionNo = @TransactionNo";
                        var rows = db.Execute(sql, new { TransactionNo = transactionNo, Odometer = odometer }, trans);
                        if (rows == 0) return false;

                        // Log Aktivitas
                        var logSql = @"INSERT INTO P2HApprovalLog (Oid, HeaderOid, Status, ActionBy, ActionTime, Notes)
                                       SELECT NEWID(), Oid, 'Closed', @ActionBy, GETDATE(), 'Kendaraan Masuk Gate'
                                       FROM P2HHeader WHERE TransactionNo = @TransactionNo";
                        db.Execute(logSql, new { TransactionNo = transactionNo, ActionBy = actionBy }, trans);

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool MarkAsCancelled(Guid oid, string reason, string actionBy)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();
                using (var trans = db.BeginTransaction())
                {
                    try
                    {
                        var sql = "UPDATE P2HHeader SET Status = 'BATAL', IsAllowedToExit = 0, QrCodeData = NULL WHERE Oid = @Oid";
                        db.Execute(sql, new { Oid = oid }, trans);

                        var logSql = "INSERT INTO P2HApprovalLog (Oid, HeaderOid, Status, ActionBy, ActionTime, Notes) VALUES (NEWID(), @HeaderOid, 'BATAL', @ActionBy, GETDATE(), @Notes)";
                        db.Execute(logSql, new { HeaderOid = oid, ActionBy = actionBy, Notes = reason }, trans);

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool UpdateGateOut(string transactionNo, string actionBy)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();
                using (var trans = db.BeginTransaction())
                {
                    try
                    {
                        var sql = "UPDATE P2HHeader SET ExitTime = GETDATE(), Status = 'Gate Out' WHERE TransactionNo = @TransactionNo";
                        var rows = db.Execute(sql, new { TransactionNo = transactionNo }, trans);
                        if (rows == 0) return false;

                        var logSql = @"INSERT INTO P2HApprovalLog (Oid, HeaderOid, Status, ActionBy, ActionTime, Notes)
                                       SELECT NEWID(), Oid, 'Gate Out', @ActionBy, GETDATE(), 'Kendaraan keluar gate'
                                       FROM P2HHeader WHERE TransactionNo = @TransactionNo";
                        db.Execute(logSql, new { TransactionNo = transactionNo, ActionBy = actionBy }, trans);

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<string> GetAdminTransportTokens()
        {
            using (var db = new SqlConnection(_connectionString))
            {
                // Use LIKE to be more flexible with spaces/casing
                var sql = "SELECT FcmToken FROM UserP2H WHERE (LevelUser LIKE '%Admin%Transport%' OR LevelUser = '4') AND FcmToken IS NOT NULL AND FcmToken <> ''";
                return db.Query<string>(sql).ToList();
            }
        }
        public string? GetDriverToken(string driverCode)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "SELECT FcmToken FROM UserP2H WHERE DriverCode = @DriverCode AND FcmToken IS NOT NULL AND FcmToken <> ''";
                return db.QueryFirstOrDefault<string>(sql, new { DriverCode = driverCode });
            }
        }
        public List<P2HHeader> GetApprovalHistory(string? approverName = null, DateTime? start = null, DateTime? end = null)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = @"
                    SELECT 
                        h.*,
                        (SELECT TOP 1 ActionBy FROM P2HApprovalLog WHERE HeaderOid = h.Oid AND Status IN ('Approved', 'Rejected', 'BATAL') ORDER BY ActionTime DESC) as ApprovedBy,
                        (SELECT TOP 1 ActionTime FROM P2HApprovalLog WHERE HeaderOid = h.Oid AND Status IN ('Approved', 'Rejected', 'BATAL') ORDER BY ActionTime DESC) as ApprovedTime
                    FROM P2HHeader h 
                    WHERE h.Status IN ('Approved', 'Closed', 'BATAL', 'Rejected', 'Gate Out')
                    AND (@ApproverName IS NULL
                         OR h.NeedsApproval = 0
                         OR EXISTS (SELECT 1 FROM P2HApprovalLog WHERE HeaderOid = h.Oid AND ActionBy = @ApproverName AND Status IN ('Approved', 'Rejected', 'BATAL')))";
                
                if (start.HasValue)
                {
                    start = start.Value.Date;
                    sql += " AND h.StartTime >= @Start";
                }
                if (end.HasValue)
                {
                    end = end.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    sql += " AND h.StartTime <= @End";
                }
                
                sql += " ORDER BY h.StartTime DESC";
                
                var headers = db.Query<P2HHeader>(sql, new { ApproverName = approverName, Start = start, End = end }).ToList();
                
                foreach (var h in headers)
                {
                    try
                    {
                        h.Logs = db.Query<P2HApprovalLog>("SELECT * FROM P2HApprovalLog WHERE HeaderOid = @Oid ORDER BY ActionTime DESC", new { Oid = h.Oid }).ToList();
                    }
                    catch (Exception)
                    {
                        h.Logs = new List<P2HApprovalLog>();
                    }
                }
                
                return headers;
            }
        }

        public List<P2HHeader> GetAllChecklists()
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM P2HHeader ORDER BY StartTime DESC";
                return db.Query<P2HHeader>(sql).ToList();
            }
        }

        // Item pertanyaan yang dianggap "temuan" pakai logika yang sama persis dengan
        // blok "TEMUAN MASALAH" di Approvals.razor (ItemCode P2H-01/P2H-12 = pertanyaan
        // induk, N/A = di-skip karena induk TIDAK, pertanyaan "keluhan" dibalik logikanya).
        private static bool IsTemuan(P2HDetail d)
        {
            if (d.ItemCode == "P2H-01" || d.ItemCode == "P2H-12") return false;
            if (d.Answer == "N/A") return false;
            bool isKeluhan = d.QuestionText?.Contains("keluhan", StringComparison.OrdinalIgnoreCase) ?? false;
            return (!isKeluhan && d.Answer == "TIDAK") || (isKeluhan && d.Answer == "YA");
        }

        public List<VehicleIssue> GetOpenVehicleIssues(string vehicleNo)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM P2HVehicleIssue WHERE VehicleNo = @VehicleNo AND IsResolved = 0 ORDER BY ReportedTime DESC";
                return db.Query<VehicleIssue>(sql, new { VehicleNo = vehicleNo }).ToList();
            }
        }

        public List<VehicleIssue> GetAllVehicleIssues()
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM P2HVehicleIssue ORDER BY IsResolved ASC, ReportedTime DESC";
                return db.Query<VehicleIssue>(sql).ToList();
            }
        }

        private void UpsertVehicleIssues(SqlConnection db, IDbTransaction trans, P2HHeader header)
        {
            foreach (var d in header.Details.Where(IsTemuan))
            {
                var existingOid = db.QueryFirstOrDefault<Guid?>(
                    "SELECT Oid FROM P2HVehicleIssue WHERE VehicleNo = @VehicleNo AND ItemCode = @ItemCode AND IsResolved = 0",
                    new { VehicleNo = header.VehicleNo, ItemCode = d.ItemCode }, trans);

                if (existingOid.HasValue)
                {
                    // ReportedBy/ReportedTime SENGAJA tidak di-update di sini: temuan yang sama muncul lagi
                    // di submission berikutnya bukan berarti "baru dilaporkan" — durasi "sudah terbuka" harus
                    // akumulasi sejak pertama kali dilaporkan, bukan reset tiap kali driver mengonfirmasi ulang.
                    var updateSql = @"UPDATE P2HVehicleIssue
                                     SET QuestionText = @QuestionText, Category = @Category, Remarks = @Remarks,
                                         HeaderOid = @HeaderOid
                                     WHERE Oid = @Oid";
                    db.Execute(updateSql, new
                    {
                        Oid = existingOid.Value,
                        QuestionText = d.QuestionText,
                        Category = d.Category,
                        Remarks = d.Remarks,
                        HeaderOid = header.Oid
                    }, trans);
                }
                else
                {
                    var insertSql = @"INSERT INTO P2HVehicleIssue
                                     (Oid, VehicleNo, ItemCode, QuestionText, Category, Remarks, ReportedBy, ReportedTime, HeaderOid, IsResolved)
                                     VALUES (NEWID(), @VehicleNo, @ItemCode, @QuestionText, @Category, @Remarks, @ReportedBy, GETDATE(), @HeaderOid, 0)";
                    db.Execute(insertSql, new
                    {
                        VehicleNo = header.VehicleNo,
                        ItemCode = d.ItemCode,
                        QuestionText = d.QuestionText,
                        Category = d.Category,
                        Remarks = d.Remarks,
                        ReportedBy = header.DriverName,
                        HeaderOid = header.Oid
                    }, trans);
                }
            }
        }

        public bool ResolveVehicleIssue(Guid issueOid, string resolvedBy, string resolutionNote)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = @"UPDATE P2HVehicleIssue
                           SET IsResolved = 1,
                               ResolvedBy = @ResolvedBy,
                               ResolvedTime = GETDATE(),
                               ResolutionNote = @ResolutionNote
                           WHERE Oid = @IssueOid AND IsResolved = 0";
                var rows = db.Execute(sql, new { IssueOid = issueOid, ResolvedBy = resolvedBy, ResolutionNote = resolutionNote });
                return rows > 0;
            }
        }

        public List<TruckStatus> GetTrucksWithServiceStatus()
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = @"
                    WITH LatestData AS (
                        SELECT 
                            VehicleNo,
                            MAX(COALESCE(GateInOdometer, Odometer)) as LastOdometer,
                            MAX(StartTime) as LastActivity
                        FROM P2HHeader
                        GROUP BY VehicleNo
                    )
                    SELECT * FROM LatestData";
                
                var results = db.Query<TruckStatus>(sql).ToList();
                const int Interval = 10000;

                foreach (var t in results)
                {
                    t.NextServiceKM = ((t.LastOdometer / Interval) + 1) * Interval;
                    t.RemainingKM = t.NextServiceKM - t.LastOdometer;

                    if (t.RemainingKM <= 500)
                        t.Status = "Overdue / Service Now";
                    else if (t.RemainingKM <= 1500)
                        t.Status = "Service Soon";
                    else
                        t.Status = "Normal";
                }

                return results;
            }
        }
        public List<GateActivity> GetGateHistory(string? gateOfficerName = null, DateTime? start = null, DateTime? end = null)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = @"
                    SELECT 
                        h.Oid,
                        h.TransactionNo, 
                        h.VehicleNo, 
                        h.DriverName, 
                        h.ExitTime,
                        h.GateInTime,
                        (SELECT TOP 1 ActionBy FROM P2HApprovalLog WHERE HeaderOid = h.Oid AND Status = 'Gate Out' ORDER BY ActionTime DESC) as ExitBy,
                        (SELECT TOP 1 ActionBy FROM P2HApprovalLog WHERE HeaderOid = h.Oid AND Status = 'Closed' ORDER BY ActionTime DESC) as EntryBy,
                        h.Status,
                        h.Odometer as ExitOdometer,
                        h.GateInOdometer
                    FROM P2HHeader h
                    WHERE h.ExitTime IS NOT NULL
                    AND (@GateOfficerName IS NULL OR EXISTS (SELECT 1 FROM P2HApprovalLog WHERE HeaderOid = h.Oid AND ActionBy = @GateOfficerName AND Status IN ('Gate Out', 'Closed')))";

                if (start.HasValue)
                {
                    start = start.Value.Date;
                    sql += " AND h.ExitTime >= @Start";
                }
                if (end.HasValue)
                {
                    end = end.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    sql += " AND h.ExitTime <= @End";
                }

                sql += " ORDER BY h.ExitTime DESC";
                
                return db.Query<GateActivity>(sql, new { GateOfficerName = gateOfficerName, Start = start, End = end }).ToList();
            }
        }

        public List<AdminUserToken> GetAdminTransportUsersAndTokens()
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "SELECT Username, DriverCode, ISNULL(FcmToken, '') as FcmToken FROM UserP2H WHERE (LevelUser LIKE '%Admin%Transport%' OR LevelUser = '4')";
                return db.Query<AdminUserToken>(sql).ToList();
            }
        }

        public void SaveNotification(string nik, string title, string body, string? attachmentUrl, string? transactionNo, string? type)
        {
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    var sql = @"INSERT INTO P2HNotification (Oid, Nik, Title, Body, ReceivedAt, IsRead, AttachmentUrl, TransactionNo, Type) 
                                VALUES (NEWID(), @Nik, @Title, @Body, GETDATE(), 0, @AttachmentUrl, @TransactionNo, @Type)";
                    db.Execute(sql, new { Nik = nik, Title = title, Body = body, AttachmentUrl = attachmentUrl, TransactionNo = transactionNo, Type = type });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DriverRepository] Error saving notification: {ex.Message}");
            }
        }

        /// <summary>Cek apakah notifikasi (mis. pengingat SIM) dengan kunci TransactionNo tertentu sudah pernah dibuat untuk nik ini. Dipakai untuk dedup job terjadwal.</summary>
        public bool SimNotifExists(string nik, string transactionNo)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "SELECT COUNT(1) FROM P2HNotification WHERE Nik = @Nik AND TransactionNo = @TransactionNo";
                return db.ExecuteScalar<int>(sql, new { Nik = nik, TransactionNo = transactionNo }) > 0;
            }
        }

        public List<P2HNotification> GetNotificationsByNik(string nik)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT * FROM P2HNotification 
                            WHERE (Nik = @Nik 
                               OR Nik = (SELECT TOP 1 Username FROM UserP2H WHERE DriverCode = @Nik)
                               OR Nik = (SELECT TOP 1 DriverCode FROM UserP2H WHERE Username = @Nik))
                               AND ReceivedAt >= DATEADD(day, -7, GETDATE())
                            ORDER BY ReceivedAt DESC";
                return db.Query<P2HNotification>(sql, new { Nik = nik }).ToList();
            }
        }

        public bool MarkNotificationAsRead(Guid oid)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE P2HNotification SET IsRead = 1 WHERE Oid = @Oid";
                return db.Execute(sql, new { Oid = oid }) > 0;
            }
        }

        public bool MarkAllNotificationsAsRead(string nik)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = @"UPDATE P2HNotification SET IsRead = 1 
                            WHERE Nik = @Nik 
                               OR Nik = (SELECT TOP 1 Username FROM UserP2H WHERE DriverCode = @Nik)
                               OR Nik = (SELECT TOP 1 DriverCode FROM UserP2H WHERE Username = @Nik)";
                return db.Execute(sql, new { Nik = nik }) > 0;
            }
        }

        public bool DeleteNotification(Guid oid)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                var sql = "DELETE FROM P2HNotification WHERE Oid = @Oid";
                return db.Execute(sql, new { Oid = oid }) > 0;
            }
        }
    }
}
