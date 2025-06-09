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
        private const string LogFilePath = "C:\\LogAPIFFIS\\ApiLogKO.txt";

        public tps_online(IConfiguration configuration, ILogger<tps_online> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("GetMasterBarang")]
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
                        ATD = reader["ATD"]?.ToString()
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
        }
    }
}
