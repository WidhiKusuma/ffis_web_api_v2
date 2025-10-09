using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using static ffis_web_api.Controllers.tps_online;

namespace ffis_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class moda_system : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<moda_system> _logger;
        private const string LogFilePath = "C:\\LogAPIFFIS\\ApiLogModa.txt";

        public moda_system(IConfiguration configuration, ILogger<moda_system> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Cek dan buat folder jika belum ada
            var logDir = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        #region API_GET
        [HttpGet("GetDataBookingByCustomer_AFF")]
        [Authorize(Roles = "MODA-API")]
        public async Task<IActionResult> GetDataBookingByCustomer_AFF(
        [FromQuery] string? CustomerName_OR_CustomerCode = null,
        [FromQuery] int PageNumber = 1,
        [FromQuery] int PageSize = 10)
        {
            var sqlDataSource = _configuration.GetConnectionString("FFISDB");

            // ✅ Log awal saat request diterima
            LogToFileRetrive("REQUEST", "Incoming request received.", CustomerName_OR_CustomerCode, $"PageNumber: {PageNumber}, PageSize: {PageSize}");

            try
            {
                await using var connection = new SqlConnection(sqlDataSource);
                await connection.OpenAsync();

                var command = new SqlCommand("sp_FFIS_API_Moda", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.Add(new SqlParameter("@StatementType", SqlDbType.NVarChar) { Value = "AFF_ListBookingByCustomer" });
                command.Parameters.Add(new SqlParameter("@CustomerName_OR_CustomerCode", SqlDbType.NVarChar)
                {
                    Value = string.IsNullOrWhiteSpace(CustomerName_OR_CustomerCode)
                    ? DBNull.Value
                    : CustomerName_OR_CustomerCode
                });
                command.Parameters.Add(new SqlParameter("@PageNumber", SqlDbType.Int) { Value = PageNumber });
                command.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = PageSize });

                await using var reader = await command.ExecuteReaderAsync();
                var result = new List<ResponseDataAFFListBooking>();

                while (await reader.ReadAsync())
                {
                    var data = new ResponseDataAFFListBooking
                    {
                        OrderType = reader["OrderType"]?.ToString(),
                        OrderSpecification = reader["OrderSpecification"]?.ToString(),
                        JobBranch = reader["JobBranch"]?.ToString(),
                        DebtorCode = reader["DebtorCode"]?.ToString(),
                        Customer = reader["Customer"]?.ToString(),
                        BookingID = reader["BookingID"]?.ToString(),
                        NoJob = reader["NoJob"]?.ToString(),
                        NoDO = reader["NoDO"]?.ToString(),
                        Commodity = reader["Commodity"]?.ToString(),
                        EstimatedWeight = reader["EstimatedWeight"] != DBNull.Value ? Convert.ToDecimal(reader["EstimatedWeight"]) : 0m,
                        EstimatedWeight_UOM = reader["EstimatedWeight_UOM"]?.ToString(),
                        EstimatedPacks = reader["EstimatedPacks"] != DBNull.Value ? Convert.ToDecimal(reader["EstimatedPacks"]) : 0m,
                        EstimatedPacks_UOM = reader["EstimatedPacks_UOM"]?.ToString(),
                        CargoDetailQtyVolume = reader["CargoDetailQtyVolume"]?.ToString(),
                        TotalPack = reader["TotalPack"]?.ToString(),
                        TotalPackUOM = reader["TotalPackUOM"]?.ToString(),
                        DeliveredPackTotal = reader["DeliveredPackTotal"]?.ToString(),
                        TruckRequired = reader["TruckRequired"]?.ToString(),
                        PickupLocationName = reader["PickupLocationName"]?.ToString(),
                        ReqPickupDate = reader["ReqPickupDate"]?.ToString(),
                        OIDShipmentAFF = reader["OIDShipmentAFF"]?.ToString(),
                        OIDCS = reader["OIDCS"]?.ToString()
                    };

                    result.Add(data);
                }

                if (result.Any())
                {
                    LogToFileRetrive("SUCCESS", "Successfully retrieved booking data.", CustomerName_OR_CustomerCode, $"Page: {PageNumber}, PageSize: {PageSize}, Records: {result.Count}");
                    return Ok(result);
                }

                LogToFileRetrive("INFO", "No booking data found for given customer.", CustomerName_OR_CustomerCode);
                return NotFound(new { message = "CustomerName or CustomerCode not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching data for CustomerName or CustomerCode : {CustomerName_OR_CustomerCode}");
                LogToFileRetrive("ERROR", "Exception occurred while fetching data.", CustomerName_OR_CustomerCode, ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching data", error = ex.Message });
            }
        }

        #endregion

        #region API_POST
        [HttpPost("CreateDataTruckingAFF")]
        [Authorize(Roles = "MODA-API")]
        public async Task<IActionResult> CreateDataTrucking([FromBody] CreateDataTruckingRequest request)
        {
            if (request == null || request.details == null || !request.details.Any())
            {
                return BadRequest(new { message = "Payload cannot be empty." });
            }

            var connStr = _configuration.GetConnectionString("FFISDB");
            var success = new List<ResponseCreateDataTruckingAFF>();
            var failed = new List<(ResponseCreateDataTruckingAFF record, string error)>();

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            Guid headerId;

            try
            {
                // Save Header
                using (var headerCmd = new SqlCommand("sp_FFIS_API_Moda", conn, tx))
                {
                    headerCmd.CommandType = CommandType.StoredProcedure;
                    AddSqlParameter(headerCmd, "@StatementType", SqlDbType.NVarChar, "SaveHeaderAFF");
                    AddSqlParameter(headerCmd, "@OidTrafficHeader", SqlDbType.NVarChar, request.OidTrafficHeader);
                    AddSqlParameter(headerCmd, "@Section", SqlDbType.NVarChar, request.Section);
                    AddSqlParameter(headerCmd, "@Vendor", SqlDbType.NVarChar, request.Vendor);
                    AddSqlParameter(headerCmd, "@PickupDeliveryType", SqlDbType.NVarChar, request.PickupDeliveryType);
                    AddSqlParameter(headerCmd, "@Truck", SqlDbType.NVarChar, request.Truck);
                    AddSqlParameter(headerCmd, "@Driver", SqlDbType.NVarChar, request.Driver);
                    AddSqlParameter(headerCmd, "@Remark", SqlDbType.NVarChar, request.Remark);
                    AddSqlParameter(headerCmd, "@Branch", SqlDbType.NVarChar, request.Branch);
                    AddSqlParameter(headerCmd, "@Direction", SqlDbType.NVarChar, request.Direction);

                    var pickupDate = string.IsNullOrWhiteSpace(request.PickupDate) ? (DateTime?)null : ParseDateTimeIfValid(request.PickupDate, "PickupDate");
                    var deliveryDate = string.IsNullOrWhiteSpace(request.DeliveryDate) ? (DateTime?)null : ParseDateTimeIfValid(request.DeliveryDate, "DeliveryDate");
                    var assignTruckDate = string.IsNullOrWhiteSpace(request.AssignTruckDate) ? (DateTime?)null : ParseDateTimeIfValid(request.AssignTruckDate, "AssignTruckDate");

                    if (request.PickupDate != null && pickupDate == null)
                        return BadRequest(new { message = "Invalid PickupDate format." });
                    if (request.DeliveryDate != null && deliveryDate == null)
                        return BadRequest(new { message = "Invalid DeliveryDate format." });
                    if (request.AssignTruckDate != null && assignTruckDate == null)
                        return BadRequest(new { message = "Invalid AssignTruckDate format." });

                    AddSqlParameter(headerCmd, "@PickupDate", SqlDbType.DateTime, pickupDate);
                    AddSqlParameter(headerCmd, "@DeliveryDate", SqlDbType.DateTime, deliveryDate);
                    AddSqlParameter(headerCmd, "@AssignTruckDate", SqlDbType.DateTime, assignTruckDate);
                    AddSqlParameter(headerCmd, "@User", SqlDbType.NVarChar, request.User);

                    var paramHeaderId = new SqlParameter("@FlagTrafficHeaderID_Output", SqlDbType.UniqueIdentifier)
                    {
                        Direction = ParameterDirection.Output
                    };
                    headerCmd.Parameters.Add(paramHeaderId);

                    await headerCmd.ExecuteNonQueryAsync();

                    headerId = (Guid)paramHeaderId.Value;
                }

                // Save Detail Items
                foreach (var item in request.details)
                {
                    bool isBookingNotFound = false;

                    try
                    {
                        // CheckTruckBookingFromCS
                        using (var checkCmd = new SqlCommand("sp_FFIS_API_Moda", conn, tx))
                        {
                            checkCmd.CommandType = CommandType.StoredProcedure;
                            AddSqlParameter(checkCmd, "@StatementType", SqlDbType.NVarChar, "CheckTruckBookingFromCS");
                            AddSqlParameter(checkCmd, "@NoJob_OR_NoDO", SqlDbType.NVarChar, item.NoJobOrNoDO);

                            using var reader = await checkCmd.ExecuteReaderAsync();
                            if (reader.Read())
                            {
                                var exists = reader.GetInt32(0);
                                if (exists == 0) isBookingNotFound = true;
                            }
                            else
                            {
                                isBookingNotFound = true;
                            }
                        }

                        if (isBookingNotFound)
                        {
                            using (var bookingCmd = new SqlCommand("sp_FFIS_API_Moda", conn, tx))
                            {
                                bookingCmd.CommandType = CommandType.StoredProcedure;
                                AddSqlParameter(bookingCmd, "@StatementType", SqlDbType.NVarChar, "SaveBookingListOutStanding");
                                AddSqlParameter(bookingCmd, "@ShipmentID", SqlDbType.NVarChar, item.ShipmentID);
                                AddSqlParameter(bookingCmd, "@HouseRef", SqlDbType.NVarChar, item.HouseRef);
                                AddSqlParameter(bookingCmd, "@TotalPack", SqlDbType.Int, item.TotalPack);
                                AddSqlParameter(bookingCmd, "@TotalPackUOMID", SqlDbType.NVarChar, item.TotalPackUOMID);
                                AddSqlParameter(bookingCmd, "@FlagTrafficHeaderID_Output", SqlDbType.UniqueIdentifier, headerId);
                                AddSqlParameter(bookingCmd, "@User", SqlDbType.NVarChar, request.User);

                                await bookingCmd.ExecuteNonQueryAsync();
                            }
                        }

                        using (var detailCmd = new SqlCommand("sp_FFIS_API_Moda", conn, tx))
                        {
                            detailCmd.CommandType = CommandType.StoredProcedure;
                            AddSqlParameter(detailCmd, "@StatementType", SqlDbType.NVarChar, "SaveDetailTraffic");
                            AddSqlParameter(detailCmd, "@ShipmentID", SqlDbType.NVarChar, item.ShipmentID);
                            AddSqlParameter(detailCmd, "@OIDShipmentAFF", SqlDbType.NVarChar, item.OIDShipmentAFF);
                            AddSqlParameter(detailCmd, "@HouseRef", SqlDbType.NVarChar, item.HouseRef);
                            AddSqlParameter(detailCmd, "@PickupAreaName", SqlDbType.NVarChar, item.PickupAreaName);
                            AddSqlParameter(detailCmd, "@DeliveryAreaName", SqlDbType.NVarChar, item.DeliveryAreaName);
                            AddSqlParameter(detailCmd, "@DeliveredPack", SqlDbType.Int, item.DeliveredPack);
                            AddSqlParameter(detailCmd, "@UOM_Pack", SqlDbType.NVarChar, item.DeliveredPackUOM);
                            AddSqlParameter(detailCmd, "@DeliveryType", SqlDbType.NVarChar, item.DeliveryType);
                            AddSqlParameter(detailCmd, "@OIDCS", SqlDbType.NVarChar, item.OIDCS);
                            AddSqlParameter(detailCmd, "@TotalPack", SqlDbType.Int, item.TotalPack);
                            AddSqlParameter(detailCmd, "@TotalPackUOMID", SqlDbType.NVarChar, item.TotalPackUOMID);
                            AddSqlParameter(detailCmd, "@PickupLocationName", SqlDbType.NVarChar, item.PickupLocationName);
                            AddSqlParameter(detailCmd, "@FlagTrafficHeaderID_Output", SqlDbType.UniqueIdentifier, headerId);

                            await detailCmd.ExecuteNonQueryAsync();
                        }

                        success.Add(item);
                    }
                    catch (Exception exItem)
                    {
                        failed.Add((item, exItem.Message));
                        LogToFile("ERROR", "Exception occurred in detail", item.NoJobOrNoDO, exItem.ToString());
                    }
                }

                // Commit or Rollback
                if (!failed.Any())
                {
                    tx.Commit();
                    LogToFile("SUCCESS", "Successfully submitted header and all details.", success.FirstOrDefault()?.NoJobOrNoDO ?? "-", $"TrafficHeaderID: {headerId}");
                    return Ok(new
                    {
                        message = "Header and all details inserted successfully.",
                        header = success.FirstOrDefault()?.NoJobOrNoDO ?? "-",
                        TrafficHeaderID = headerId,
                        detailSuccessCount = success.Count
                    });
                }
                else
                {
                    tx.Rollback();
                    return BadRequest(new
                    {
                        message = "Some details failed. Transaction rolled back.",
                        header = failed.FirstOrDefault().record.NoJobOrNoDO ?? "-",
                        TrafficHeaderID = headerId,
                        detailSuccessCount = success.Count,
                        detailFailedCount = failed.Count,
                        errors = failed.Select(x => new
                        {
                            NoJobOrNoDO = x.record.NoJobOrNoDO,
                            ErrorMessage = x.error
                        })
                    });
                }
            }
            catch (Exception ex)
            {
                tx.Rollback();
                var fallbackNoJobOrNoDO = success.FirstOrDefault()?.NoJobOrNoDO ?? failed.FirstOrDefault().record.NoJobOrNoDO ?? "-";
                LogToFile("ERROR", "Exception occurred when saving header.", fallbackNoJobOrNoDO, ex.ToString());

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Internal server error. Transaction rolled back.",
                    error = ex.Message,
                    header = fallbackNoJobOrNoDO
                });
            }
        }

        private void AddSqlParameter(SqlCommand cmd, string name, SqlDbType type, object? value)
        {
            cmd.Parameters.Add(new SqlParameter(name, type)
            {
                Value = value ?? DBNull.Value
            });
        }

        private void LogToFile(string status, string message, string? no_Job_or_no_DO = null, string? errorDetails = null)
        {
            try
            {
                var logMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [{status}] No.Job/No.DO: {no_Job_or_no_DO ?? "-"} - {message} {errorDetails ?? ""}";
                System.IO.File.AppendAllText(LogFilePath, logMessage + Environment.NewLine); // Use System.IO.File
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write log to file.");
            }
        }

        private void LogToFileRetrive(string status, string message, string? CustomerName_OR_CustomerCode = null, string? errorDetails = null)
        {
            try
            {
                var logMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [{status}] Cust.Name/Cust.Code: {CustomerName_OR_CustomerCode ?? "-"} - {message} {errorDetails ?? ""}";
                System.IO.File.AppendAllText(LogFilePath, logMessage + Environment.NewLine); // Use System.IO.File
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write log to file.");
            }
        }

        private DateTime? ParseDateTimeIfValid(string? dateTimeString, string paramName, string format = "yyyyMMddHHmmss")
        {
            if (string.IsNullOrEmpty(dateTimeString) || dateTimeString.Length != format.Length)
                return null;

            if (DateTime.TryParseExact(dateTimeString, format, null, DateTimeStyles.None, out var parsedDate))
                return parsedDate;

            _logger.LogWarning("Invalid format for {ParamName}: {DateTimeString}", paramName, dateTimeString);
            return null;
        }
        #endregion

        public class ResponseDataAFFListBooking
        {
            public string? OrderType { get; set; }
            public string? OrderSpecification { get; set; }
            public string? JobBranch { get; set; }
            public string? DebtorCode { get; set; }
            public string? Customer { get; set; }
            public string? BookingID { get; set; }
            public string? NoJob { get; set; }
            public string? NoDO { get; set; }
            public string? Commodity { get; set; }
            public decimal? EstimatedWeight { get; set; }
            public string? EstimatedWeight_UOM { get; set; }
            public decimal? EstimatedPacks { get; set; }
            public string? EstimatedPacks_UOM { get; set; }
            public string? CargoDetailQtyVolume { get; set; }
            public string? TotalPack { get; set; }
            public string? TotalPackUOM { get; set; }
            public string? DeliveredPackTotal { get; set; }
            public string? TruckRequired { get; set; }
            public string? PickupLocationName { get; set; }
            public string? ReqPickupDate { get; set; }
            public string? OIDShipmentAFF { get; set; }
            public string? OIDCS { get; set; }
        }

        public class ResponseCreateDataTruckingAFF
        {
            // --- Untuk StatementType = 'CheckTruckBookingFromCS'
            public string? NoJobOrNoDO { get; set; }

            // --- Untuk StatementType = 'SaveDetailTraffic'
            public string? OIDShipmentAFF { get; set; }
            public string? ShipmentID { get; set; }
            public string? HouseRef { get; set; }
            public string? PickupAreaName { get; set; }
            public string? DeliveryAreaName { get; set; }
            public int DeliveredPack { get; set; }
            public string? DeliveredPackUOM { get; set; }
            public string? DeliveryType { get; set; }
            public string? OIDCS { get; set; }

            // Update tambahan untuk ShipmentMonitoringCsAFF
            public int TotalPack { get; set; }
            public string? TotalPackUOMID { get; set; }
            public string? PickupLocationName { get; set; }

        }

        public class CreateDataTruckingRequest
        {
            //public string? NoJobOrNoDO { get; set; }
            public string? OidTrafficHeader { get; set; }
            public string? Section { get; set; }
            public string? Vendor { get; set; }
            public string? PickupDeliveryType { get; set; }
            public string? Truck { get; set; }
            public string? Driver { get; set; }
            public string? Remark { get; set; }
            public string? Branch { get; set; }
            public string? Direction { get; set; }
            public string? PickupDate { get; set; }
            public string? DeliveryDate { get; set; }
            public string? AssignTruckDate { get; set; }
            public string? User { get; set; }

            public List<ResponseCreateDataTruckingAFF> details { get; set; }
        }

    }
}
