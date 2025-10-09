using Azure.Core;
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ffis_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class tps_online : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<tps_online> _logger;
        private const string LogFilePath = "C:\\LogAPIFFIS\\ApiLogTPS.txt";

        public tps_online(IConfiguration configuration, ILogger<tps_online> logger)
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

        [HttpGet("GetMasterBarang")]
        [Authorize(Roles = "POST-API")]
        public async Task<IActionResult> GetMasterBarangByMAWB([FromQuery] string MAWB_or_HAWB)
        {
            var sqlDataSource = _configuration.GetConnectionString("FFISDB");

            try
            {
                await using var connection = new SqlConnection(sqlDataSource);
                await connection.OpenAsync();

                var command = new SqlCommand("sp_FFIS_API_TPS_Online", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.Add(new SqlParameter("@StatementType", SqlDbType.NVarChar) { Value = "GetMasterBarang" });
                command.Parameters.Add(new SqlParameter("@MAWB_or_HAWB", SqlDbType.NVarChar) { Value = MAWB_or_HAWB });

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var data = new ResponseDataMasterBarang
                    {
                        MasterAWB = reader["MasterAWB"]?.ToString(),
                        HouseAWB = reader["HouseAWB"]?.ToString(),
                        CarrierCode = reader["CarrierCode"]?.ToString(),
                        MAWBIssuedDate = reader["MAWBIssuedDate"]?.ToString(),
                        FlightVoyage = reader["FlightVoyage"]?.ToString(),
                        Jumlah = reader["Jumlah"] != DBNull.Value ? Convert.ToDecimal(reader["Jumlah"]) : 0m,
                        Bruto = reader["Bruto"] != DBNull.Value ? Convert.ToDecimal(reader["Bruto"]) : 0m,
                        ChargeBruto = reader["ChargeBruto"] != DBNull.Value ? Convert.ToDecimal(reader["ChargeBruto"]) : 0m,
                        ConsolATA = reader["ConsolATA"]?.ToString(),
                        FirstLoad = reader["FirstLoad"]?.ToString(),
                        LastDisch = reader["LastDisch"]?.ToString(),
                        BC11No = reader["BC11No"]?.ToString(),
                        POS = reader["POS"]?.ToString(),
                        ETA = reader["ETA"]?.ToString(),
                        ETD = reader["ETD"]?.ToString(),
                        BC11Date = reader["BC11Date"]?.ToString(),
                        ATA = reader["ATA"]?.ToString(),
                        ATD = reader["ATD"]?.ToString()
                    };
                    return Ok(data);
                }

                return NotFound(new { message = "MAWB/HAWB not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching data for MAWB_or_HAWB {MAWB_or_HAWB}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching data", error = ex.Message });
            }
        }

        [HttpGet("GetBarangBongkarKapalpesawat")]
        [Authorize(Roles = "POST-API")]
        public async Task<IActionResult> GetBarangBongkarKapalpesawatByMAWB([FromQuery] string MAWB)
        {
            var sqlDataSource = _configuration.GetConnectionString("FFISDB");

            try
            {
                await using var connection = new SqlConnection(sqlDataSource);
                await connection.OpenAsync();

                var command = new SqlCommand("sp_FFIS_API_TPS_Online", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.Add(new SqlParameter("@StatementType", SqlDbType.NVarChar) { Value = "GetBarang_Bongkar_Kapal_pesawat" });
                command.Parameters.Add(new SqlParameter("@MasterAWB", SqlDbType.NVarChar) { Value = MAWB });

                await using var reader = await command.ExecuteReaderAsync();

                var result = new List<ResponseDataBongkarKapalpesawat>();

                while (await reader.ReadAsync())
                {
                    var data = new ResponseDataBongkarKapalpesawat
                    {
                        MasterAWB = reader["MasterAWB"]?.ToString(),
                        Jumlah = reader["Jumlah"] != DBNull.Value ? Convert.ToDecimal(reader["Jumlah"]) : 0m,
                        Bruto = reader["Bruto"] != DBNull.Value ? Convert.ToDecimal(reader["Bruto"]) : 0m,
                        ChargeBruto = reader["ChargeBruto"] != DBNull.Value ? Convert.ToDecimal(reader["ChargeBruto"]) : 0m
                    };
                    result.Add(data);
                }
                if (result.Any())
                    return Ok(result);

                return NotFound(new { message = "MAWB not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching data for MAWB {MAWB}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching data", error = ex.Message });
            }
        }

        [HttpGet("GetBarangAsalPLPOBImport")]
        [Authorize(Roles = "POST-API")]
        public async Task<IActionResult> GetBarangAsalPLPOBImportByMAWB([FromQuery] string MAWB)
        {
            var sqlDataSource = _configuration.GetConnectionString("FFISDB");

            try
            {
                await using var connection = new SqlConnection(sqlDataSource);
                await connection.OpenAsync();

                var command = new SqlCommand("sp_FFIS_API_TPS_Online", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.Add(new SqlParameter("@StatementType", SqlDbType.NVarChar) { Value = "GetBarang_Asal_PLP_OB_Import" });
                command.Parameters.Add(new SqlParameter("@MasterAWB", SqlDbType.NVarChar) { Value = MAWB });

                await using var reader = await command.ExecuteReaderAsync();
                var result = new List<ResponseDataBarangAsalPLPOBImport>();

                while (await reader.ReadAsync())
                {
                    var data = new ResponseDataBarangAsalPLPOBImport
                    {
                        MasterAWB = reader["MasterAWB"]?.ToString(),
                        Komoditi = reader["Komoditi"]?.ToString(),
                        HouseBill = reader["HouseBill"]?.ToString(),
                        Jumlah = reader["Jumlah"] != DBNull.Value ? Convert.ToDecimal(reader["Jumlah"]) : 0m,
                        Bruto = reader["Bruto"] != DBNull.Value ? Convert.ToDecimal(reader["Bruto"]) : 0m,
                        ChargeBruto = reader["ChargeBruto"] != DBNull.Value ? Convert.ToDecimal(reader["ChargeBruto"]) : 0m,
                        ConsigneeCode = reader["ConsigneeCode"]?.ToString(),
                        ConsigneeName = reader["ConsigneeName"]?.ToString(),
                        POS = reader["POS"]?.ToString(),
                        SubPos = reader["SubPos"]?.ToString(),
                        FirstLoad = reader["FirstLoad"]?.ToString(),
                        LastDisch = reader["LastDisch"]?.ToString(),
                        BC11Date = reader["BC11Date"]?.ToString(),
                        ETA = reader["ETA"]?.ToString(),
                        ATA = reader["ATA"]?.ToString(),
                        ATD = reader["ATD"]?.ToString(),
                        HouseBLDate = reader["HouseBLDate"]?.ToString()
                    };

                    result.Add(data);
                }

                if (result.Any())
                    return Ok(result);

                return NotFound(new { message = "MAWB not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching data for MAWB {MAWB}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching data", error = ex.Message });
            }
        }

        [HttpPost("SavePLPData")]
        [Authorize(Roles = "POST-API")]
        public async Task<IActionResult> SavePLPData([FromBody] List<ResponseDataPLP> dataList)
        {
            if (dataList == null || !dataList.Any())
            {
                return BadRequest(new { message = "Payload list is empty or null." });
            }

            var connStr = _configuration.GetConnectionString("FFISDB");
            var resultList = new List<object>();

            foreach (var data in dataList)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.MasterAWB))
                {
                    LogToFile("ERROR", "Payload or MasterAWB is missing", data?.MasterAWB);
                    resultList.Add(new
                    {
                        MasterAWB = data?.MasterAWB,
                        status = "failed",
                        message = "Payload or MasterAWB is missing."
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(data.HouseAWB))
                {
                    LogToFile("ERROR", "HouseRef is missing or empty", data.MasterAWB);
                    resultList.Add(new
                    {
                        MasterAWB = data.MasterAWB,
                        status = "failed",
                        message = "HouseRef is required."
                    });
                    continue;
                }

                var cleanedMasterAWB = data.MasterAWB.Replace("-", "").Trim();

                try
                {
                    await using var conn = new SqlConnection(connStr);
                    await conn.OpenAsync();

                    using var cmd = new SqlCommand("sp_FFIS_API_TPS_Online_PLP", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@Flag", "SaveHeader");
                    cmd.Parameters.AddWithValue("@User", "API-TPS");
                    cmd.Parameters.AddWithValue("@MAWBNo", cleanedMasterAWB ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@HouseRef", data.HouseAWB ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PLPNo", data.PLPNo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Jumlah", data.Jumlah ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Weight", data.Weight ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BC11No", data.BC11No ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@TPSAsal", data.TPSAsal ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@NoBatalPLP", data.NoBatalPLP ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@POS", data.POS ?? (object)DBNull.Value);

                    var PLPDate = string.IsNullOrWhiteSpace(data.PLPDate) ? (DateTime?)null : ParseDateTimeIfValid(data.PLPDate, "PLPDate", cleanedMasterAWB);
                    var TglBC11 = string.IsNullOrWhiteSpace(data.TglBC11) ? (DateTime?)null : ParseDateTimeIfValid(data.TglBC11, "TglBC11", cleanedMasterAWB);
                    var TglBatalPLP = string.IsNullOrWhiteSpace(data.TglBatalPLP) ? (DateTime?)null : ParseDateTimeIfValid(data.TglBatalPLP, "TglBatalPLP", cleanedMasterAWB);
                    var TPSGateInDate = string.IsNullOrWhiteSpace(data.TPSGateInDate) ? (DateTime?)null : ParseDateTimeIfValid(data.TPSGateInDate, "TPSGateInDate", cleanedMasterAWB);
                    var TPSGateOutDate = string.IsNullOrWhiteSpace(data.TPSGateOutDate) ? (DateTime?)null : ParseDateTimeIfValid(data.TPSGateOutDate, "TPSGateOutDate", cleanedMasterAWB);

                    if (data.PLPDate != null && PLPDate == null ||
                        data.TglBC11 != null && TglBC11 == null ||
                        data.TglBatalPLP != null && TglBatalPLP == null ||
                        data.TPSGateInDate != null && TPSGateInDate == null ||
                        data.TPSGateOutDate != null && TPSGateOutDate == null)
                    {
                        LogToFile("ERROR", "Invalid date format in one or more fields.", cleanedMasterAWB);
                        resultList.Add(new
                        {
                            MasterAWB = cleanedMasterAWB,
                            status = "failed",
                            message = "Invalid date format in one or more fields."
                        });
                        continue;
                    }

                    cmd.Parameters.Add(new SqlParameter("@PLPDate", SqlDbType.DateTime) { Value = PLPDate ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@TglBC11", SqlDbType.DateTime) { Value = TglBC11 ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@TglBatalPLP", SqlDbType.DateTime) { Value = TglBatalPLP ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@TPSGateInDate", SqlDbType.DateTime) { Value = TPSGateInDate ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@TPSGateOutDate", SqlDbType.DateTime) { Value = TPSGateOutDate ?? (object)DBNull.Value });

                    var outputParam = new SqlParameter("@FlagInwardID_Output", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    await cmd.ExecuteNonQueryAsync();

                    int flag = (int)(outputParam.Value ?? 0);
                    string resultMsg = flag switch
                    {
                        1 => "PLP created successfully",
                        2 => "PLP updated successfully",
                        _ => "No operation performed"
                    };

                    if (flag == 0)
                    {
                        LogToFile("ERROR", "Stored procedure did not perform insert or update", cleanedMasterAWB);
                        resultList.Add(new
                        {
                            MasterAWB = cleanedMasterAWB,
                            status = "failed",
                            message = "No operation performed by the database."
                        });
                        continue;
                    }

                    LogToFile("SUCCESS", resultMsg, cleanedMasterAWB);

                    resultList.Add(new
                    {
                        MasterAWB = cleanedMasterAWB,
                        status = "success",
                        message = resultMsg,
                        code = flag
                    });
                }
                catch (Exception ex)
                {
                    LogToFile("ERROR", "Exception during SavePLPData", cleanedMasterAWB, ex.ToString());
                    _logger.LogError(ex, "Error in SavePLPData for MAWB {mawb}", cleanedMasterAWB);

                    resultList.Add(new
                    {
                        MasterAWB = cleanedMasterAWB,
                        status = "failed",
                        message = "Internal error during PLP data save",
                        error = ex.Message
                    });
                }
            }

            // Final summary
            int successCount = resultList.Count(r => r.ToString().Contains("status = success"));
            int failedCount = resultList.Count(r => r.ToString().Contains("status = failed"));
            LogToFile("INFO", $"SavePLPData Summary: Total input = {dataList.Count}, Success = {successCount}, Failed = {failedCount}");

            return Ok(resultList);
        }


        private void LogToFile(string status, string message, string? mawb = null, string? errorDetails = null)
        {
            try
            {
                var logTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                var statusPadded = status?.PadRight(7) ?? "INFO   ";
                var mawbDisplay = string.IsNullOrWhiteSpace(mawb) ? "-" : mawb;

                var logMessage = $"{logTime} [{statusPadded}] MAWB: {mawbDisplay} - {message}";
                if (!string.IsNullOrWhiteSpace(errorDetails))
                {
                    logMessage += $" | Details: {errorDetails}";
                }

                System.IO.File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write log to file.");
            }
        }

        private DateTime? ParseDateTimeIfValid(string? dateTimeString, string paramName, string? mawb = null)
        {
            var formats = new[] { "yyyyMMddHHmmss", "yyyyMMdd" };

            foreach (var format in formats)
            {
                if (!string.IsNullOrWhiteSpace(dateTimeString) &&
                    dateTimeString.Length == format.Length &&
                    DateTime.TryParseExact(dateTimeString, format, null, DateTimeStyles.None, out var result))
                {
                    return result;
                }
            }

            var warning = $"Invalid date format for [{paramName}]: value = '{dateTimeString}'";
            _logger.LogWarning("{Message} | MAWB: {MAWB}", warning, mawb);
            LogToFile("ERROR", warning, mawb);
            return null;
        }

        public class ResponseDataMasterBarang
        {
            public string? MasterAWB { get; set; }
            public string? HouseAWB { get; set; }
            public string? CarrierCode { get; set; }
            public string? MAWBIssuedDate { get; set; }
            public string? FlightVoyage { get; set; }
            public decimal? Jumlah { get; set; }
            public decimal? Bruto { get; set; }
            public decimal? ChargeBruto { get; set; }
            public string? ConsolATA { get; set; }
            public string? FirstLoad { get; set; }
            public string? LastDisch { get; set; }
            public string? BC11No { get; set; }
            public string? BC11Date { get; set; }
            public string? POS { get; set; }
            public string? ETA { get; set; }
            public string? ETD { get; set; }
            public string? ATA { get; set; }
            public string? ATD { get; set; }
        }

        public class ResponseDataBongkarKapalpesawat
        {
            public string? MasterAWB { get; set; }
            public decimal? Jumlah { get; set; }
            public decimal? Bruto { get; set; }
            public decimal? ChargeBruto { get; set; }
        }

        public class ResponseDataBarangAsalPLPOBImport
        {
            public string? MasterAWB { get; set; }
            public string? Komoditi { get; set; }
            public string? HouseBill { get; set; }
            public decimal? Jumlah { get; set; }
            public decimal? Bruto { get; set; }
            public decimal? ChargeBruto { get; set; }
            public string? ConsigneeCode { get; set; }
            public string? ConsigneeName { get; set; }
            public string? POS { get; set; }
            public string? SubPos { get; set; }
            public string? FirstLoad { get; set; }
            public string? LastDisch { get; set; }
            public string? BC11Date { get; set; }
            public string? ETA { get; set; }
            public string? ATA { get; set; }
            public string? ATD { get; set; }
            public string? HouseBLDate { get; set; }
        }

        public class ResponseDataPLP
        {
            public string MasterAWB { get; set; }
            public string? HouseAWB { get; set; }
            public string? PLPNo { get; set; }
            public string? PLPDate { get; set; }
            public int? Jumlah { get; set; }
            public float? Weight { get; set; }
            public string? BC11No { get; set; }
            public string? TglBC11 { get; set; }
            public string? TPSAsal { get; set; }
            public string? NoBatalPLP { get; set; }
            public string? TglBatalPLP { get; set; }
            public string? POS { get; set; }
            public string? TPSGateInDate { get; set; }
            public string? TPSGateOutDate { get; set; }
        }
    }
}
