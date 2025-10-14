using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using static ffis_web_api.Controllers.tps_online;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ffis_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ResponseReferenceController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResponseReferenceController> _logger;
        private const string LogFilePath = "C:\\LogAPIFFIS\\ApiLogKO.txt";

        public ResponseReferenceController(IConfiguration configuration, ILogger<ResponseReferenceController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "POST-API")]
        public async Task<IActionResult> Post([FromBody] List<ResponseData> responseDataList)
        {
            if (responseDataList == null || !responseDataList.Any())
            {
                return BadRequest(new { message = "Payload cannot be empty." });
            }

            var sqlDataSource = _configuration.GetConnectionString("FFISDB");
            var successfullyProcessed = new List<ResponseData>();
            var failedRecords = new List<ResponseData>();

            SqlConnection? connection = null;
            SqlTransaction? transaction = null;

            try
            {
                connection = new SqlConnection(sqlDataSource);
                await connection.OpenAsync();
                transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                LogToFile("INFO", $"Received API request with {responseDataList.Count} records.");

                foreach (var responseData in responseDataList)
                {
                    LogToFile("INFO", "Processing CustReff", responseData.CustReff);

                    if (!TryValidateModel(responseData))
                    {
                        failedRecords.Add(responseData);
                        LogToFile("ERROR", $"Validation failed for CustReff {responseData.CustReff}: EventDate is required", responseData.CustReff);
                        continue;
                    }

                    // Check CustReff existence
                    await using var checkCustReffCommand = new SqlCommand("sp_FFIS_API_KO_ResponseReferance", connection, (SqlTransaction)transaction)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    checkCustReffCommand.Parameters.Add(new SqlParameter("@StatementType", SqlDbType.NVarChar) { Value = "CheckCustReff" });
                    checkCustReffCommand.Parameters.Add(new SqlParameter("@CustReff", SqlDbType.NVarChar) { Value = responseData.CustReff });

                    var result = await checkCustReffCommand.ExecuteScalarAsync();

                    int existsFlag = 0;
                    if (result != null && result != DBNull.Value)
                    {
                        existsFlag = Convert.ToInt32(result);
                    }

                    if (existsFlag == 0)
                    {
                        LogToFile("NOT_FOUND", "CustReff not found", responseData.CustReff);
                        failedRecords.Add(responseData);
                        continue;
                    }

                    // Map fields if needed
                    if (!string.IsNullOrEmpty(responseData.KodeRespon) ||
                        !string.IsNullOrEmpty(responseData.NamaRespon) ||
                        !string.IsNullOrEmpty(responseData.TglRespon))
                    {
                        responseData.KodeStatus = responseData.KodeRespon;
                        responseData.NamaStatus = responseData.NamaRespon;
                        responseData.TglStatus = responseData.TglRespon;
                        responseData.Tipe = "Respon";
                    }

                    if (!string.IsNullOrEmpty(responseData.KodeStatus) ||
                        !string.IsNullOrEmpty(responseData.NamaStatus) ||
                        !string.IsNullOrEmpty(responseData.TglStatus))
                    {
                        responseData.Tipe ??= "Status";
                    }

                    var parsedTglStatus = ParseDateTimeIfValid(responseData.TglStatus, "TglStatus");
                    var parsedEventDate = ParseDateTimeIfValid(responseData.EventDate, "EventDate");

                    if (parsedEventDate == null)
                    {
                        LogToFile("NOT_FOUND", "CustReff not found", responseData.CustReff);
                        failedRecords.Add(responseData);
                        continue;
                    }

                    // Execute stored procedure
                    await using var command = new SqlCommand("sp_FFIS_API_KO_ResponseReferance", connection, (SqlTransaction)transaction)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    command.Parameters.Add(new SqlParameter("@StatementType", SqlDbType.NVarChar) { Value = "SaveData" });
                    command.Parameters.Add(new SqlParameter("@custReff", SqlDbType.NVarChar) { Value = responseData.CustReff ?? string.Empty });
                    command.Parameters.Add(new SqlParameter("@noAju", SqlDbType.NVarChar) { Value = responseData.NoAju ?? string.Empty });
                    command.Parameters.Add(new SqlParameter("@tipeDoc", SqlDbType.NVarChar) { Value = responseData.TipeDoc ?? string.Empty });
                    command.Parameters.Add(new SqlParameter("@userCreator", SqlDbType.NVarChar) { Value = responseData.UserCreator ?? string.Empty });
                    command.Parameters.Add(new SqlParameter("@itemBarang", SqlDbType.Int) { Value = responseData.ItemBarang ?? (object)DBNull.Value });
                    command.Parameters.Add(new SqlParameter("@kodeStatus", SqlDbType.NVarChar) { Value = responseData.KodeStatus ?? string.Empty });
                    command.Parameters.Add(new SqlParameter("@namaStatus", SqlDbType.NVarChar) { Value = responseData.NamaStatus ?? string.Empty });

                    if (parsedTglStatus.HasValue)
                    command.Parameters.Add(new SqlParameter("@tglStatus", SqlDbType.DateTime) { Value = parsedTglStatus.Value });
                    command.Parameters.Add(new SqlParameter("@eventDate", SqlDbType.DateTime) { Value = parsedEventDate.Value });
                    command.Parameters.Add(new SqlParameter("@tipe", SqlDbType.NVarChar) { Value = responseData.Tipe ?? string.Empty });

                    //await command.ExecuteNonQueryAsync();
                    //successfullyProcessed.Add(responseData);
                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        LogToFile("SUCCESS", "Successfully processed CustReff", responseData.CustReff);
                        successfullyProcessed.Add(responseData);
                    }
                    catch (Exception ex)
                    {
                        LogToFile("ERROR", "Exception occurred", responseData.CustReff, ex.Message);
                        failedRecords.Add(responseData);
                    }
                }

                if (successfullyProcessed.Any())
                {
                    await transaction.CommitAsync();
                    LogToFile("INFO", "Transaction committed successfully.");
                }
                else
                {
                    await transaction.RollbackAsync();
                    var failedCustReffs = string.Join(", ", failedRecords.Select(r => r.CustReff));
                    var logMessage = $"{successfullyProcessed.Count} CustReff berhasil diproses dan ({failedRecords.Count} CustReff gagal diproses/Nomor CustReff Tidak ditemukan)";
                    LogToFile("WARNING", "Transaction rolled back due to errors.", null, logMessage);
                }

                // Notifikasi berdasarkan jumlah yang berhasil dan gagal
                if (failedRecords.Count == 0)
                {
                    return Ok(new { message = "Semua data berhasil diproses." });
                }
                else
                {
                    var failedCustReffs = string.Join(", ", failedRecords.Select(r => r.CustReff));
                    return Ok(new
                    {
                        message = $"{successfullyProcessed.Count} CustReff berhasil diproses dan ({failedRecords.Count} CustReff gagal diproses/Nomor CustReff Tidak ditemukan)",
                        failedRecords = failedCustReffs
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Data processing");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to process data", error = ex.Message });
            }
        }

        [HttpGet("CheckCustReff")]
        [Authorize(Roles = "POST-API")]
        public async Task<IActionResult> CheckCustReff([FromQuery] string CustReff)
        {
            var sqlDataSource = _configuration.GetConnectionString("FFISDB");

            try
            {
                await using var connection = new SqlConnection(sqlDataSource);
                await connection.OpenAsync();

                var command = new SqlCommand("sp_FFIS_API_KO_ResponseReferance", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.Add(new SqlParameter("@StatementType", SqlDbType.NVarChar) { Value = "CheckShipmentID" });
                command.Parameters.Add(new SqlParameter("@custReff", SqlDbType.NVarChar) { Value = CustReff });

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var result = reader["CustReffExists"]?.ToString();

                    if (!string.IsNullOrEmpty(result))
                    {
                        var data = new ResponseDataCustReff
                        {
                            CustReff = result
                        };
                        return Ok(data);
                    }
                }

                return NotFound(new { message = $"CustReff '{CustReff}' tidak ditemukan" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching data for CustReff {CustReff}");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error fetching data",
                    error = ex.Message
                });
            }
        }


        // Inside the ResponseReferenceController class
        private void LogToFile(string status, string message, string? custReff = null, string? errorDetails = null)
        {
            try
            {
                var logMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [{status}] CustReff: {custReff ?? "-"} - {message} {errorDetails ?? ""}";
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
    }

    public class ResponseData
    {
        [Required]
        public string? CustReff { get; set; }

        public string? NoAju { get; set; }
        public string? TipeDoc { get; set; }
        public string? UserCreator { get; set; }
        public int? ItemBarang { get; set; }
        public string? KodeStatus { get; set; }
        public string? NamaStatus { get; set; }
        public string? TglStatus { get; set; }
        public string? TglRespon { get; set; }
        public string? KodeRespon { get; set; }
        public string? NamaRespon { get; set; }

        [Required]
        public string? EventDate { get; set; }

        public string? Tipe { get; set; }
    }

    public class ResponseDataCustReff
    {
        public string? CustReff { get; set; }
    }

}
