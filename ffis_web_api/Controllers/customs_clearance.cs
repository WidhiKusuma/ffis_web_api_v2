using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace ffis_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class customs_clearance : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<customs_clearance> _logger;
        private const string LogFilePath = "C:\\Logs\\ApiLogKO.txt";

        public customs_clearance(IConfiguration configuration, ILogger<customs_clearance> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("Header")]
        [Authorize(Roles = "POST-API")]
        public async Task<IActionResult> Post([FromBody] List<ResponseDataCeisaHeader> responseDataListHeader)
        {
            if (responseDataListHeader == null || !responseDataListHeader.Any())
            {
                return BadRequest(new { message = "Payload cannot be empty." });
            }

            var sqlDataSource = _configuration.GetConnectionString("FFISDB");
            var successfullyProcessed = new List<ResponseDataCeisaHeader>();
            var failedRecords = new List<(ResponseDataCeisaHeader record, string error)>();
            var responSP = "";

            using var connection = new SqlConnection(sqlDataSource);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            LogToFile("INFO CeisaHeader", $"Received API request with {responseDataListHeader.Count} records.");

            foreach (var responseDataHeader in responseDataListHeader)
            {
                if (string.IsNullOrEmpty(responseDataHeader.NomorAju))
                {
                    failedRecords.Add((responseDataHeader, "NomorAju is required."));
                    LogToFile("ERROR CeisaHeader", $"Skipping record due to missing NomorAju.");
                    continue;
                }

                try
                {
                    LogToFile("INFO CeisaHeader", "Processing NomorAju", responseDataHeader.NomorAju);

                    using var command = new SqlCommand("sp_FFIS_API_KO_CeisaHeader", connection, transaction)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    // Parameter tambahannya bisa dibuat fungsi helper agar rapi, contoh:
                    AddSqlParameter(command, "@StatementType", SqlDbType.NVarChar, "SaveData");
                    AddSqlParameter(command, "@NomorAju", SqlDbType.NVarChar, responseDataHeader.NomorAju);
                    AddSqlParameter(command, "@KodeDokumen", SqlDbType.NVarChar, responseDataHeader.KodeDokumen);
                    AddSqlParameter(command, "@KodeKantor", SqlDbType.NVarChar, responseDataHeader.KodeKantor);
                    AddSqlParameter(command, "@KodeKantorBongkar", SqlDbType.NVarChar, responseDataHeader.KodeKantorBongkar);
                    AddSqlParameter(command, "@KodeKantorPeriksa", SqlDbType.NVarChar, responseDataHeader.KodeKantorPeriksa);
                    AddSqlParameter(command, "@KodeKantorTujuan", SqlDbType.NVarChar, responseDataHeader.KodeKantorTujuan);
                    AddSqlParameter(command, "@KodeKantorEkspor", SqlDbType.NVarChar, responseDataHeader.KodeKantorEkspor);
                    AddSqlParameter(command, "@KodeJenisImpor", SqlDbType.NVarChar, responseDataHeader.KodeJenisImpor);
                    AddSqlParameter(command, "@KodeJenisEkspor", SqlDbType.NVarChar, responseDataHeader.KodeJenisEkspor);
                    AddSqlParameter(command, "@KodeJenisTPB", SqlDbType.NVarChar, responseDataHeader.KodeJenisTPB);
                    AddSqlParameter(command, "@KodeJenisPLB", SqlDbType.NVarChar, responseDataHeader.KodeJenisPLB);
                    AddSqlParameter(command, "@KodeJenisProsedur", SqlDbType.NVarChar, responseDataHeader.KodeJenisProsedur);
                    AddSqlParameter(command, "@KodeTujuanPemasukan", SqlDbType.NVarChar, responseDataHeader.KodeTujuanPemasukan);
                    AddSqlParameter(command, "@KodeTujuanPengiriman", SqlDbType.NVarChar, responseDataHeader.KodeTujuanPengiriman);
                    AddSqlParameter(command, "@KodeTujuanTPB", SqlDbType.NVarChar, responseDataHeader.KodeTujuanTPB);
                    AddSqlParameter(command, "@KodeCaraDagang", SqlDbType.NVarChar, responseDataHeader.KodeCaraDagang);
                    AddSqlParameter(command, "@KodeCaraBayar", SqlDbType.NVarChar, responseDataHeader.KodeCaraBayar);
                    AddSqlParameter(command, "@KodeCaraBayarLainnya", SqlDbType.NVarChar, responseDataHeader.KodeCaraBayarLainnya);
                    AddSqlParameter(command, "@KodeGudangAsal", SqlDbType.NVarChar, responseDataHeader.KodeGudangAsal);
                    AddSqlParameter(command, "@KodeGudangTujuan", SqlDbType.NVarChar, responseDataHeader.KodeGudangTujuan);
                    AddSqlParameter(command, "@KodeJenisKirim", SqlDbType.NVarChar, responseDataHeader.KodeJenisKirim);
                    AddSqlParameter(command, "@KodeJenisPengiriman", SqlDbType.NVarChar, responseDataHeader.KodeJenisPengiriman);
                    AddSqlParameter(command, "@KodeKategoriEkspor", SqlDbType.NVarChar, responseDataHeader.KodeKategoriEkspor);
                    AddSqlParameter(command, "@KodeKategoriMasukFTZ", SqlDbType.NVarChar, responseDataHeader.KodeKategoriMasukFTZ);
                    AddSqlParameter(command, "@KodeKategoriKeluarFTZ", SqlDbType.NVarChar, responseDataHeader.KodeKategoriKeluarFTZ);
                    AddSqlParameter(command, "@KodeKategoriBarang", SqlDbType.NVarChar, responseDataHeader.KodeKategoriBarang);
                    AddSqlParameter(command, "@KodeLokasi", SqlDbType.NVarChar, responseDataHeader.KodeLokasi);
                    AddSqlParameter(command, "@KodeLokasiBayar", SqlDbType.NVarChar, responseDataHeader.KodeLokasiBayar);
                    AddSqlParameter(command, "@LokasiAsal", SqlDbType.NVarChar, responseDataHeader.LokasiAsal);
                    AddSqlParameter(command, "@LokasiTujuan", SqlDbType.NVarChar, responseDataHeader.LokasiTujuan);
                    AddSqlParameter(command, "@KodeDaerahAsal", SqlDbType.NVarChar, responseDataHeader.KodeDaerahAsal);
                    AddSqlParameter(command, "@KodeGudangAsal_1", SqlDbType.NVarChar, responseDataHeader.KodeGudangAsal_1);
                    AddSqlParameter(command, "@KodeGudangTujuan_1", SqlDbType.NVarChar, responseDataHeader.KodeGudangTujuan_1);
                    AddSqlParameter(command, "@KodeNegaraTujuan", SqlDbType.NVarChar, responseDataHeader.KodeNegaraTujuan);
                    AddSqlParameter(command, "@KodeTutupPU", SqlDbType.NVarChar, responseDataHeader.KodeTutupPU);
                    AddSqlParameter(command, "@NomorBC11", SqlDbType.NVarChar, responseDataHeader.NomorBC11);
                    AddSqlParameter(command, "@TanggalBC11", SqlDbType.DateTime, responseDataHeader.TanggalBC11);
                    AddSqlParameter(command, "@NomorPos", SqlDbType.NVarChar, responseDataHeader.NomorPos);
                    AddSqlParameter(command, "@NomorSubPos", SqlDbType.NVarChar, responseDataHeader.NomorSubPos);
                    AddSqlParameter(command, "@KodePelabuhanBongkar", SqlDbType.NVarChar, responseDataHeader.KodePelabuhanBongkar);
                    AddSqlParameter(command, "@KodePelabuhanMuat", SqlDbType.NVarChar, responseDataHeader.KodePelabuhanMuat);
                    AddSqlParameter(command, "@KodePelabuhanMuatAkhir", SqlDbType.NVarChar, responseDataHeader.KodePelabuhanMuatAkhir);
                    AddSqlParameter(command, "@KodePelabuhanTransit", SqlDbType.NVarChar, responseDataHeader.KodePelabuhanTransit);
                    AddSqlParameter(command, "@KodePelabuhanTujuan", SqlDbType.NVarChar, responseDataHeader.KodePelabuhanTujuan);
                    AddSqlParameter(command, "@KodePelabuhanEkspor", SqlDbType.NVarChar, responseDataHeader.KodePelabuhanEkspor);
                    AddSqlParameter(command, "@KodeTPS", SqlDbType.NVarChar, responseDataHeader.KodeTPS);
                    AddSqlParameter(command, "@TanggalBerangkat", SqlDbType.DateTime, responseDataHeader.TanggalBerangkat);
                    AddSqlParameter(command, "@TanggalEkspor", SqlDbType.DateTime, responseDataHeader.TanggalEkspor);
                    AddSqlParameter(command, "@TanggalMasuk", SqlDbType.DateTime, responseDataHeader.TanggalMasuk);
                    AddSqlParameter(command, "@TanggalMuat", SqlDbType.DateTime, responseDataHeader.TanggalMuat);
                    AddSqlParameter(command, "@TanggalTiba", SqlDbType.DateTime, responseDataHeader.TanggalTiba);
                    AddSqlParameter(command, "@TanggalPeriksa", SqlDbType.DateTime, responseDataHeader.TanggalPeriksa);
                    AddSqlParameter(command, "@TempatStuffing", SqlDbType.NVarChar, responseDataHeader.TempatStuffing);
                    AddSqlParameter(command, "@TanggalStuffing", SqlDbType.DateTime, responseDataHeader.TanggalStuffing);
                    AddSqlParameter(command, "@KodeTandaPengaman", SqlDbType.NVarChar, responseDataHeader.KodeTandaPengaman);
                    AddSqlParameter(command, "@JumlahTandaPengaman", SqlDbType.Float, responseDataHeader.JumlahTandaPengaman);
                    AddSqlParameter(command, "@FlagCurah", SqlDbType.NVarChar, responseDataHeader.FlagCurah);
                    AddSqlParameter(command, "@FlagSDA", SqlDbType.NVarChar, responseDataHeader.FlagSDA);
                    AddSqlParameter(command, "@FlagVD", SqlDbType.NVarChar, responseDataHeader.FlagVD);
                    AddSqlParameter(command, "@FlagAPBK", SqlDbType.NVarChar, responseDataHeader.FlagAPBK);
                    AddSqlParameter(command, "@FlagMigas", SqlDbType.NVarChar, responseDataHeader.FlagMigas);
                    AddSqlParameter(command, "@KodeAsuransi", SqlDbType.NVarChar, responseDataHeader.KodeAsuransi);
                    AddSqlParameter(command, "@Asuransi", SqlDbType.Float, responseDataHeader.Asuransi);
                    AddSqlParameter(command, "@NilaiBarang", SqlDbType.Float, responseDataHeader.NilaiBarang);
                    AddSqlParameter(command, "@NilaiIncoterm", SqlDbType.Float, responseDataHeader.NilaiIncoterm);
                    AddSqlParameter(command, "@NilaiMaklon", SqlDbType.Float, responseDataHeader.NilaiMaklon);
                    AddSqlParameter(command, "@Asuransi_1", SqlDbType.Float, responseDataHeader.Asuransi_1);
                    AddSqlParameter(command, "@Freight", SqlDbType.Float, responseDataHeader.Freight);
                    AddSqlParameter(command, "@FOB", SqlDbType.Float, responseDataHeader.FOB);
                    AddSqlParameter(command, "@BiayaTambahan", SqlDbType.Float, responseDataHeader.BiayaTambahan);
                    AddSqlParameter(command, "@BiayaPengurang", SqlDbType.Float, responseDataHeader.BiayaPengurang);
                    AddSqlParameter(command, "@VD", SqlDbType.Float, responseDataHeader.VD);
                    AddSqlParameter(command, "@CIF", SqlDbType.Float, responseDataHeader.CIF);
                    AddSqlParameter(command, "@HargaPenyerahan", SqlDbType.Float, responseDataHeader.HargaPenyerahan);
                    AddSqlParameter(command, "@NDPBM", SqlDbType.Float, responseDataHeader.NDPBM);
                    AddSqlParameter(command, "@TotalDanaSawit", SqlDbType.Float, responseDataHeader.TotalDanaSawit);
                    AddSqlParameter(command, "@DasarPengenaanPajak", SqlDbType.Float, responseDataHeader.DasarPengenaanPajak);
                    AddSqlParameter(command, "@NilaiJasa", SqlDbType.NVarChar, responseDataHeader.NilaiJasa);
                    AddSqlParameter(command, "@UangMuka", SqlDbType.Float, responseDataHeader.UangMuka);
                    AddSqlParameter(command, "@Bruto", SqlDbType.Float, responseDataHeader.Bruto);
                    AddSqlParameter(command, "@Netto", SqlDbType.Float, responseDataHeader.Netto);
                    AddSqlParameter(command, "@Volume", SqlDbType.Float, responseDataHeader.Volume);
                    AddSqlParameter(command, "@KotaPernyataan", SqlDbType.NVarChar, responseDataHeader.KotaPernyataan);
                    AddSqlParameter(command, "@TanggalPernyataan", SqlDbType.DateTime, responseDataHeader.TanggalPernyataan);
                    AddSqlParameter(command, "@NamaPernyataan", SqlDbType.NVarChar, responseDataHeader.NamaPernyataan);
                    AddSqlParameter(command, "@JabatanPernyataan", SqlDbType.NVarChar, responseDataHeader.JabatanPernyataan);
                    AddSqlParameter(command, "@KodeValuta", SqlDbType.NVarChar, responseDataHeader.KodeValuta);
                    AddSqlParameter(command, "@KodeIncoterm", SqlDbType.NVarChar, responseDataHeader.KodeIncoterm);
                    AddSqlParameter(command, "@KodeJasaKenaPajak", SqlDbType.NVarChar, responseDataHeader.KodeJasaKenaPajak);
                    AddSqlParameter(command, "@NomorBuktiBayar", SqlDbType.NVarChar, responseDataHeader.NomorBuktiBayar);
                    AddSqlParameter(command, "@TanggalBuktiBayar", SqlDbType.DateTime, responseDataHeader.TanggalBuktiBayar);
                    AddSqlParameter(command, "@KodeJeniasNilai", SqlDbType.NVarChar, responseDataHeader.KodeJeniasNilai);
                    AddSqlParameter(command, "@KodeKantorMuat", SqlDbType.NVarChar, responseDataHeader.KodeKantorMuat);
                    AddSqlParameter(command, "@NomorDaftar", SqlDbType.NVarChar, responseDataHeader.NomorDaftar);
                    AddSqlParameter(command, "@TanggalDaftar", SqlDbType.DateTime, responseDataHeader.TanggalDaftar);
                    AddSqlParameter(command, "@KodeAsalBarangFTC", SqlDbType.NVarChar, responseDataHeader.KodeAsalBarangFTC);
                    AddSqlParameter(command, "@KodeTujuanPengeluaran", SqlDbType.NVarChar, responseDataHeader.KodeTujuanPengeluaran);
                    AddSqlParameter(command, "@PPNPajak", SqlDbType.Float, responseDataHeader.PPNPajak);
                    AddSqlParameter(command, "@PPNBM_PAJAK", SqlDbType.NVarChar, responseDataHeader.PPNBM_PAJAK);
                    AddSqlParameter(command, "@TarifPPNPajak", SqlDbType.NVarChar, responseDataHeader.TarifPPNPajak);
                    AddSqlParameter(command, "@TarifPPNBMPajak", SqlDbType.NVarChar, responseDataHeader.TarifPPNBMPajak);
                    AddSqlParameter(command, "@BarangTidakBerwujud", SqlDbType.NVarChar, responseDataHeader.BarangTidakBerwujud);
                    AddSqlParameter(command, "@KodeJenisPengeluaran", SqlDbType.NVarChar, responseDataHeader.KodeJenisPengeluaran);
                    var responseParamHeader = new SqlParameter("@ResponseMessage", SqlDbType.NVarChar, 200);
                    responseParamHeader.Direction = ParameterDirection.Output;
                    command.Parameters.Add(responseParamHeader);
                    await command.ExecuteNonQueryAsync();
                    string responseMessage = responseParamHeader.Value.ToString();
                    successfullyProcessed.Add(responseDataHeader);
                    LogToFile("INFO CeisaHeader", $"Response Status: {responseMessage}", responseDataHeader.NomorAju);
                    responSP = responseMessage;
                }
                catch (Exception ex)
                {
                    failedRecords.Add((responseDataHeader, ex.Message));
                    LogToFile("ERROR CeisaHeader", $"Failed Insert NomorAju {responseDataHeader.NomorAju}: {ex.Message}");
                }
            }

            if (!failedRecords.Any())
            {
                transaction.Commit();
                LogToFile("INFO CeisaHeader", "Transaction committed successfully.");
                return Ok(new
                {
                    message = responSP,
                    successCount = successfullyProcessed.Count,
                    failedCount = 0
                });
            }
            else
            {
                transaction.Rollback();
                LogToFile("ERROR CeisaHeader", $"Transaction rolled back due to errors. Failed records: {failedRecords.Count}");
                return BadRequest(new
                {
                    message = "Some records failed to insert, transaction rolled back.",
                    successCount = successfullyProcessed.Count,
                    failedCount = failedRecords.Count,
                    errors = failedRecords.Select(e => new { NomorAju = e.record.NomorAju, Error = e.error })
                });
            }
        }

        [HttpPost("BahanBaku")]
        [Authorize(Roles = "POST-API")]
        public async Task<IActionResult> PostBahanBaku([FromBody] List<ResponseDataCeisaBahanBaku> responseDataListBahanBaku)
        {
            if (responseDataListBahanBaku == null || !responseDataListBahanBaku.Any())
            {
                return BadRequest(new { message = "Payload cannot be empty." });
            }

            var sqlDataSource = _configuration.GetConnectionString("FFISDB");
            var successfullyProcessed = new List<ResponseDataCeisaBahanBaku>();
            var failedRecords = new List<(ResponseDataCeisaBahanBaku record, string error)>();
            var responSP = "";

            using var connection = new SqlConnection(sqlDataSource);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            LogToFile("INFO CeisaBahanBaku", $"Received API request with {responseDataListBahanBaku.Count} records.");

            foreach (var responseDataBahanBaku in responseDataListBahanBaku)
            {
                if (string.IsNullOrEmpty(responseDataBahanBaku.NomorAju))
                {
                    failedRecords.Add((responseDataBahanBaku, "NomorAju is required."));
                    LogToFile("ERROR CeisaBahanBaku", $"Skipping record due to missing NomorAju.");
                    continue;
                }

                try
                {
                    LogToFile("INFO CeisaBahanBaku", "Processing NomorAju", responseDataBahanBaku.NomorAju);

                    using var command = new SqlCommand("sp_FFIS_API_KO_CeisaBahanBaku", connection, transaction)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    // Parameter tambahannya bisa dibuat fungsi helper agar rapi, contoh:
                    AddSqlParameter(command, "@StatementType", SqlDbType.NVarChar, "SaveData");
                    AddSqlParameter(command, "@NomorAju", SqlDbType.NVarChar, responseDataBahanBaku.NomorAju);
                    AddSqlParameter(command, "@SeriBarang", SqlDbType.NVarChar, responseDataBahanBaku.SeriBarang);
                    AddSqlParameter(command, "@SeriBahanBaku", SqlDbType.NVarChar, responseDataBahanBaku.SeriBahanBaku);
                    AddSqlParameter(command, "@KodeAsalBahanBaku", SqlDbType.NVarChar, responseDataBahanBaku.KodeAsalBahanBaku);
                    AddSqlParameter(command, "@HS", SqlDbType.NVarChar, responseDataBahanBaku.HS);
                    AddSqlParameter(command, "@KodeBarang", SqlDbType.NVarChar, responseDataBahanBaku.KodeBarang);
                    AddSqlParameter(command, "@Uraian", SqlDbType.NVarChar, responseDataBahanBaku.Uraian);
                    AddSqlParameter(command, "@Merek", SqlDbType.NVarChar, responseDataBahanBaku.Merek);
                    AddSqlParameter(command, "@Tipe", SqlDbType.NVarChar, responseDataBahanBaku.Tipe);
                    AddSqlParameter(command, "@Ukuran", SqlDbType.NVarChar, responseDataBahanBaku.Ukuran);
                    AddSqlParameter(command, "@SpesifikasiLain", SqlDbType.NVarChar, responseDataBahanBaku.SpesifikasiLain);
                    AddSqlParameter(command, "@KodeSatuan", SqlDbType.NVarChar, responseDataBahanBaku.KodeSatuan);
                    AddSqlParameter(command, "@JumlahSatuan", SqlDbType.NVarChar, responseDataBahanBaku.JumlahSatuan);
                    AddSqlParameter(command, "@KodeKemasan", SqlDbType.NVarChar, responseDataBahanBaku.KodeKemasan);
                    AddSqlParameter(command, "@JumlahKemasan", SqlDbType.NVarChar, responseDataBahanBaku.JumlahKemasan);
                    AddSqlParameter(command, "@KodeDokumenAsli", SqlDbType.NVarChar, responseDataBahanBaku.KodeDokumenAsli);
                    AddSqlParameter(command, "@KodeKantorAsal", SqlDbType.NVarChar, responseDataBahanBaku.KodeKantorAsal);
                    AddSqlParameter(command, "@NomorDaftarAsal", SqlDbType.NVarChar, responseDataBahanBaku.NomorDaftarAsal);
                    AddSqlParameter(command, "@TanggalDaftarAsal", SqlDbType.DateTime, responseDataBahanBaku.TanggalDaftarAsal);
                    AddSqlParameter(command, "@NomorAjuAsal", SqlDbType.NVarChar, responseDataBahanBaku.NomorAjuAsal);
                    AddSqlParameter(command, "@SeriBarangAsal", SqlDbType.NVarChar, responseDataBahanBaku.SeriBarangAsal);
                    AddSqlParameter(command, "@Netto", SqlDbType.NVarChar, responseDataBahanBaku.Netto);
                    AddSqlParameter(command, "@Bruto", SqlDbType.NVarChar, responseDataBahanBaku.Bruto);
                    AddSqlParameter(command, "@Volume", SqlDbType.NVarChar, responseDataBahanBaku.Volume);
                    AddSqlParameter(command, "@CIF", SqlDbType.NVarChar, responseDataBahanBaku.CIF);
                    AddSqlParameter(command, "@CIFRupiah", SqlDbType.NVarChar, responseDataBahanBaku.CIFRupiah);
                    AddSqlParameter(command, "@NDPBM", SqlDbType.NVarChar, responseDataBahanBaku.NDPBM);
                    AddSqlParameter(command, "@HargaPenyerahan", SqlDbType.NVarChar, responseDataBahanBaku.HargaPenyerahan);
                    AddSqlParameter(command, "@HargaPerolehan", SqlDbType.NVarChar, responseDataBahanBaku.HargaPerolehan);
                    AddSqlParameter(command, "@NilaiJasa", SqlDbType.NVarChar, responseDataBahanBaku.NilaiJasa);
                    AddSqlParameter(command, "@SeriIzin", SqlDbType.NVarChar, responseDataBahanBaku.SeriIzin);
                    AddSqlParameter(command, "@Valuta", SqlDbType.NVarChar, responseDataBahanBaku.Valuta);
                    AddSqlParameter(command, "@KodeBKC", SqlDbType.NVarChar, responseDataBahanBaku.KodeBKC);
                    AddSqlParameter(command, "@KodeKomoditiBKC", SqlDbType.NVarChar, responseDataBahanBaku.KodeKomoditiBKC);
                    AddSqlParameter(command, "@KodeSubKomoditiBKC", SqlDbType.NVarChar, responseDataBahanBaku.KodeSubKomoditiBKC);
                    AddSqlParameter(command, "@FlagTIS", SqlDbType.NVarChar, responseDataBahanBaku.FlagTIS);
                    AddSqlParameter(command, "@IsiPerkemasan", SqlDbType.NVarChar, responseDataBahanBaku.IsiPerkemasan);
                    AddSqlParameter(command, "@JumlahDilekatkan", SqlDbType.NVarChar, responseDataBahanBaku.JumlahDilekatkan);
                    AddSqlParameter(command, "@JumlahPitaCukai", SqlDbType.NVarChar, responseDataBahanBaku.JumlahPitaCukai);
                    AddSqlParameter(command, "@HJECukai", SqlDbType.NVarChar, responseDataBahanBaku.HJECukai);
                    AddSqlParameter(command, "@TarikCukai", SqlDbType.NVarChar, responseDataBahanBaku.TarikCukai);
                    var responseParamBahanBaku = new SqlParameter("@ResponseMessage", SqlDbType.NVarChar, 200);
                    responseParamBahanBaku.Direction = ParameterDirection.Output;
                    command.Parameters.Add(responseParamBahanBaku);
                    await command.ExecuteNonQueryAsync();
                    string responseMessage = responseParamBahanBaku.Value.ToString();
                    successfullyProcessed.Add(responseDataBahanBaku);
                    LogToFile("INFO CeisaBahanBaku", $"Response Status: {responseMessage}", responseDataBahanBaku.NomorAju);
                    responSP = responseMessage;
                }
                catch (Exception ex)
                {
                    failedRecords.Add((responseDataBahanBaku, ex.Message));
                    LogToFile("ERROR CeisaBahanBaku", $"Failed Insert NomorAju {responseDataBahanBaku.NomorAju}: {ex.Message}");
                }
            }

            if (!failedRecords.Any())
            {
                transaction.Commit();
                LogToFile("INFO CeisaBahanBaku", "Transaction committed successfully.");
                return Ok(new
                {
                    message = responSP,
                    successCount = successfullyProcessed.Count,
                    failedCount = 0
                });
            }
            else
            {
                transaction.Rollback();
                LogToFile("ERROR CeisaBahanBaku", $"Transaction rolled back due to errors. Failed records: {failedRecords.Count}");
                return BadRequest(new
                {
                    message = "Some records failed to insert, transaction rolled back.",
                    successCount = successfullyProcessed.Count,
                    failedCount = failedRecords.Count,
                    errors = failedRecords.Select(e => new { NomorAju = e.record.NomorAju, Error = e.error })
                });
            }
        }

        [HttpPost("BahanBakuDokumen")]
        [Authorize(Roles = "POST-API")]
        public async Task<IActionResult> PostBahanBakuDokumen([FromBody] List<ResponseDataCeisaBahanBakuDokumen> responseDataListBahanBakuDokumen)
        {
            if (responseDataListBahanBakuDokumen == null || !responseDataListBahanBakuDokumen.Any())
            {
                return BadRequest(new { message = "Payload cannot be empty." });
            }

            var sqlDataSource = _configuration.GetConnectionString("FFISDB");
            var successfullyProcessed = new List<ResponseDataCeisaBahanBakuDokumen>();
            var failedRecords = new List<(ResponseDataCeisaBahanBakuDokumen record, string error)>();
            var responSP = "";

            using var connection = new SqlConnection(sqlDataSource);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            LogToFile("INFO CeisaBahanBakuDokumen", $"Received API request with {responseDataListBahanBakuDokumen.Count} records.");

            foreach (var responseDataBahanBakuDokumen in responseDataListBahanBakuDokumen)
            {
                if (string.IsNullOrEmpty(responseDataBahanBakuDokumen.NomorAju))
                {
                    failedRecords.Add((responseDataBahanBakuDokumen, "NomorAju is required."));
                    LogToFile("ERROR CeisaBahanBakuDokumen", $"Skipping record due to missing NomorAju.");
                    continue;
                }

                try
                {
                    LogToFile("INFO CeisaBahanBakuDokumen", "Processing NomorAju", responseDataBahanBakuDokumen.NomorAju);

                    using var command = new SqlCommand("sp_FFIS_API_KO_CeisaBahanBakuDokumen", connection, transaction)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    // Parameter tambahannya bisa dibuat fungsi helper agar rapi, contoh:
                    AddSqlParameter(command, "@StatementType", SqlDbType.NVarChar, "SaveData");
                    AddSqlParameter(command, "@NomorAju", SqlDbType.NVarChar, responseDataBahanBakuDokumen.NomorAju);
                    AddSqlParameter(command, "@SeriBarang", SqlDbType.NVarChar, responseDataBahanBakuDokumen.SeriBarang);
                    AddSqlParameter(command, "@SeriBahanBaku", SqlDbType.NVarChar, responseDataBahanBakuDokumen.SeriBahanBaku);
                    AddSqlParameter(command, "@KodeAsalBahanBaku", SqlDbType.NVarChar, responseDataBahanBakuDokumen.KodeAsalBahanBaku);
                    AddSqlParameter(command, "@SeriDokumen", SqlDbType.NVarChar, responseDataBahanBakuDokumen.SeriDokumen);
                    AddSqlParameter(command, "@SeriIzin", SqlDbType.NVarChar, responseDataBahanBakuDokumen.SeriIzin);
                    var responseParamBahanBakuDokumen = new SqlParameter("@ResponseMessage", SqlDbType.NVarChar, 200);
                    responseParamBahanBakuDokumen.Direction = ParameterDirection.Output;
                    command.Parameters.Add(responseParamBahanBakuDokumen);
                    await command.ExecuteNonQueryAsync();
                    string responseMessage = responseParamBahanBakuDokumen.Value.ToString();
                    successfullyProcessed.Add(responseDataBahanBakuDokumen);
                    LogToFile("INFO CeisaBahanBakuDokumen", $"Response Status: {responseMessage}", responseDataBahanBakuDokumen.NomorAju);
                    responSP = responseMessage;
                }
                catch (Exception ex)
                {
                    failedRecords.Add((responseDataBahanBakuDokumen, ex.Message));
                    LogToFile("ERROR CeisaBahanBakuDokumen", $"Failed Insert NomorAju {responseDataBahanBakuDokumen.NomorAju}: {ex.Message}");
                }
            }

            if (!failedRecords.Any())
            {
                transaction.Commit();
                LogToFile("INFO CeisaBahanBakuDokumen", "Transaction committed successfully.");
                return Ok(new
                {
                    message = responSP,
                    successCount = successfullyProcessed.Count,
                    failedCount = 0
                });
            }
            else
            {
                transaction.Rollback();
                LogToFile("ERROR CeisaBahanBakuDokumen", $"Transaction rolled back due to errors. Failed records: {failedRecords.Count}");
                return BadRequest(new
                {
                    message = "Some records failed to insert, transaction rolled back.",
                    successCount = successfullyProcessed.Count,
                    failedCount = failedRecords.Count,
                    errors = failedRecords.Select(e => new { NomorAju = e.record.NomorAju, Error = e.error })
                });
            }
        }

        [HttpPost("BahanBakuTarif")]
        [Authorize(Roles = "POST-API")]
        public async Task<IActionResult> PostBahanBakuTarif([FromBody] List<ResponseDataCeisaBahanBakuTarif> responseDataListBahanBakuTarif)
        {
            if (responseDataListBahanBakuTarif == null || !responseDataListBahanBakuTarif.Any())
            {
                return BadRequest(new { message = "Payload cannot be empty." });
            }

            var sqlDataSource = _configuration.GetConnectionString("FFISDB");
            var successfullyProcessed = new List<ResponseDataCeisaBahanBakuTarif>();
            var failedRecords = new List<(ResponseDataCeisaBahanBakuTarif record, string error)>();
            var responSP = "";

            using var connection = new SqlConnection(sqlDataSource);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            LogToFile("INFO CeisaBahanBakuTarif", $"Received API request with {responseDataListBahanBakuTarif.Count} records.");

            foreach (var responseDataBahanBakuTarif in responseDataListBahanBakuTarif)
            {
                if (string.IsNullOrEmpty(responseDataBahanBakuTarif.NomorAju))
                {
                    failedRecords.Add((responseDataBahanBakuTarif, "NomorAju is required."));
                    LogToFile("ERROR CeisaBahanBakuTarif", $"Skipping record due to missing NomorAju.");
                    continue;
                }

                try
                {
                    LogToFile("INFO CeisaBahanBakuTarif", "Processing NomorAju", responseDataBahanBakuTarif.NomorAju);

                    using var command = new SqlCommand("sp_FFIS_API_KO_CeisaBahanBakuTarif", connection, transaction)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    // Parameter tambahannya bisa dibuat fungsi helper agar rapi, contoh:
                    AddSqlParameter(command, "@StatementType", SqlDbType.NVarChar, "SaveData");
                    AddSqlParameter(command, "@NomorAju", SqlDbType.VarChar, responseDataBahanBakuTarif.NomorAju);
                    AddSqlParameter(command, "@SeriBarang", SqlDbType.VarChar, responseDataBahanBakuTarif.SeriBarang);
                    AddSqlParameter(command, "@SeriBahanBaku", SqlDbType.VarChar, responseDataBahanBakuTarif.SeriBahanBaku);
                    AddSqlParameter(command, "@KodeAsalBahanBaku", SqlDbType.VarChar, responseDataBahanBakuTarif.KodeAsalBahanBaku);
                    AddSqlParameter(command, "@KodePungutan", SqlDbType.VarChar, responseDataBahanBakuTarif.KodePungutan);
                    AddSqlParameter(command, "@KodeTarif", SqlDbType.VarChar, responseDataBahanBakuTarif.KodeTarif);
                    AddSqlParameter(command, "@Tarif", SqlDbType.VarChar, responseDataBahanBakuTarif.Tarif);
                    AddSqlParameter(command, "@KodeFasilitas", SqlDbType.VarChar, responseDataBahanBakuTarif.KodeFasilitas);
                    AddSqlParameter(command, "@TarifFasilitas", SqlDbType.VarChar, responseDataBahanBakuTarif.TarifFasilitas);
                    AddSqlParameter(command, "@NilaiBayar", SqlDbType.VarChar, responseDataBahanBakuTarif.NilaiBayar);
                    AddSqlParameter(command, "@NilaiFasilitas", SqlDbType.VarChar, responseDataBahanBakuTarif.NilaiFasilitas);
                    AddSqlParameter(command, "@NilaiSudahDilunasi", SqlDbType.VarChar, responseDataBahanBakuTarif.NilaiSudahDilunasi);
                    AddSqlParameter(command, "@KodeSatuan", SqlDbType.VarChar, responseDataBahanBakuTarif.KodeSatuan);
                    AddSqlParameter(command, "@JumlahSatuan", SqlDbType.VarChar, responseDataBahanBakuTarif.JumlahSatuan);
                    AddSqlParameter(command, "@FagBMTSementara", SqlDbType.VarChar, responseDataBahanBakuTarif.FagBMTSementara);
                    AddSqlParameter(command, "@KodeKomoditiCukai", SqlDbType.VarChar, responseDataBahanBakuTarif.KodeKomoditiCukai);
                    AddSqlParameter(command, "@KodeSubKomoditiCukai", SqlDbType.VarChar, responseDataBahanBakuTarif.KodeSubKomoditiCukai);
                    AddSqlParameter(command, "@KodeSubKomoditiCukai_1", SqlDbType.VarChar, responseDataBahanBakuTarif.KodeSubKomoditiCukai_1);
                    AddSqlParameter(command, "@FlagTIS_1", SqlDbType.VarChar, responseDataBahanBakuTarif.FlagTIS_1);
                    AddSqlParameter(command, "@FlagPelekatan_1", SqlDbType.VarChar, responseDataBahanBakuTarif.FlagPelekatan_1);
                    AddSqlParameter(command, "@KodeKemasan_1", SqlDbType.VarChar, responseDataBahanBakuTarif.KodeKemasan_1);
                    AddSqlParameter(command, "@FlagTIS", SqlDbType.VarChar, responseDataBahanBakuTarif.FlagTIS);
                    AddSqlParameter(command, "@FlagPelekatan", SqlDbType.VarChar, responseDataBahanBakuTarif.FlagPelekatan);
                    AddSqlParameter(command, "@KodeKemasan", SqlDbType.VarChar, responseDataBahanBakuTarif.KodeKemasan);
                    AddSqlParameter(command, "@JumlahKemasan", SqlDbType.VarChar, responseDataBahanBakuTarif.JumlahKemasan);
                    var responseParamBahanBakuTarif = new SqlParameter("@ResponseMessage", SqlDbType.NVarChar, 200);
                    responseParamBahanBakuTarif.Direction = ParameterDirection.Output;
                    command.Parameters.Add(responseParamBahanBakuTarif);
                    await command.ExecuteNonQueryAsync();
                    string responseMessage = responseParamBahanBakuTarif.Value.ToString();
                    successfullyProcessed.Add(responseDataBahanBakuTarif);
                    LogToFile("INFO CeisaBahanBakuTarif", $"Response Status: {responseMessage}", responseDataBahanBakuTarif.NomorAju);
                    responSP = responseMessage;
                }
                catch (Exception ex)
                {
                    failedRecords.Add((responseDataBahanBakuTarif, ex.Message));
                    LogToFile("ERROR CeisaBahanBakuTarif", $"Failed Insert NomorAju {responseDataBahanBakuTarif.NomorAju}: {ex.Message}");
                }
            }

            if (!failedRecords.Any())
            {
                transaction.Commit();
                LogToFile("INFO CeisaBahanBakuTarif", "Transaction committed successfully.");
                return Ok(new
                {
                    message = responSP,
                    successCount = successfullyProcessed.Count,
                    failedCount = 0
                });
            }
            else
            {
                transaction.Rollback();
                LogToFile("ERROR CeisaBahanBakuTarif", $"Transaction rolled back due to errors. Failed records: {failedRecords.Count}");
                return BadRequest(new
                {
                    message = "Some records failed to insert, transaction rolled back.",
                    successCount = successfullyProcessed.Count,
                    failedCount = failedRecords.Count,
                    errors = failedRecords.Select(e => new { NomorAju = e.record.NomorAju, Error = e.error })
                });
            }
        }

        [HttpPost("BankDevisa")]
        [Authorize(Roles = "POST-API")]
        public async Task<IActionResult> PostBankDevisa([FromBody] List<ResponseDataCeisaBankDevisa> responseDataListBankDevisa)
        {
            if (responseDataListBankDevisa == null || !responseDataListBankDevisa.Any())
            {
                return BadRequest(new { message = "Payload cannot be empty." });
            }

            var sqlDataSource = _configuration.GetConnectionString("FFISDB");
            var successfullyProcessed = new List<ResponseDataCeisaBankDevisa>();
            var failedRecords = new List<(ResponseDataCeisaBankDevisa record, string error)>();
            var responSP = "";

            using var connection = new SqlConnection(sqlDataSource);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            LogToFile("INFO CeisaBankDevisa", $"Received API request with {responseDataListBankDevisa.Count} records.");

            foreach (var responseDataBankDevisa in responseDataListBankDevisa)
            {
                if (string.IsNullOrEmpty(responseDataBankDevisa.NomorAju))
                {
                    failedRecords.Add((responseDataBankDevisa, "NomorAju is required."));
                    LogToFile("ERROR CeisaBankDevisa", $"Skipping record due to missing NomorAju.");
                    continue;
                }

                try
                {
                    LogToFile("INFO CeisaBankDevisa", "Processing NomorAju", responseDataBankDevisa.NomorAju);

                    using var command = new SqlCommand("sp_FFIS_API_KO_CeisaBankDevisa", connection, transaction)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    // Parameter tambahannya bisa dibuat fungsi helper agar rapi, contoh:
                    AddSqlParameter(command, "@StatementType", SqlDbType.NVarChar, "SaveData");
                    AddSqlParameter(command, "@NomorAju", SqlDbType.NVarChar, responseDataBankDevisa.NomorAju);
                    AddSqlParameter(command, "@Seri", SqlDbType.NVarChar, responseDataBankDevisa.Seri);
                    AddSqlParameter(command, "@Kode", SqlDbType.NVarChar, responseDataBankDevisa.Kode);
                    AddSqlParameter(command, "@Nama", SqlDbType.NVarChar, responseDataBankDevisa.Nama);
                    var responseParamBankDevisa = new SqlParameter("@ResponseMessage", SqlDbType.NVarChar, 200);
                    responseParamBankDevisa.Direction = ParameterDirection.Output;
                    command.Parameters.Add(responseParamBankDevisa);
                    await command.ExecuteNonQueryAsync();
                    string responseMessage = responseParamBankDevisa.Value.ToString();
                    successfullyProcessed.Add(responseDataBankDevisa);
                    LogToFile("INFO CeisaBankDevisa", $"Response Status: {responseMessage}", responseDataBankDevisa.NomorAju);
                    responSP = responseMessage;
                }
                catch (Exception ex)
                {
                    failedRecords.Add((responseDataBankDevisa, ex.Message));
                    LogToFile("ERROR CeisaBankDevisa", $"Failed Insert NomorAju {responseDataBankDevisa.NomorAju}: {ex.Message}");
                }
            }

            if (!failedRecords.Any())
            {
                transaction.Commit();
                LogToFile("INFO CeisaBankDevisa", "Transaction committed successfully.");
                return Ok(new
                {
                    message = responSP,
                    successCount = successfullyProcessed.Count,
                    failedCount = 0
                });
            }
            else
            {
                transaction.Rollback();
                LogToFile("ERROR CeisaBankDevisa", $"Transaction rolled back due to errors. Failed records: {failedRecords.Count}");
                return BadRequest(new
                {
                    message = "Some records failed to insert, transaction rolled back.",
                    successCount = successfullyProcessed.Count,
                    failedCount = failedRecords.Count,
                    errors = failedRecords.Select(e => new { NomorAju = e.record.NomorAju, Error = e.error })
                });
            }
        }

        private void AddSqlParameter(SqlCommand command, string paramName, SqlDbType dbType, object value)
        {
            var param = command.Parameters.Add(paramName, dbType);
            param.Value = value ?? DBNull.Value;
        }

        // Inside the ResponseReferenceController class
        private void LogToFile(string status, string message, string? NomorAju = null, string? errorDetails = null)
        {
            try
            {
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{status}] NomorAju: {NomorAju ?? "-"} - {message} {errorDetails ?? ""}";
                System.IO.File.AppendAllText(LogFilePath, logMessage + Environment.NewLine); // Use System.IO.File
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write log to file.");
            }
        }

    }

    public class ResponseDataCeisaHeader
    {
        public string? NomorAju { get; set; }
        public string? KodeDokumen { get; set; }
        public string? KodeKantor { get; set; }
        public string? KodeKantorBongkar { get; set; }
        public string? KodeKantorPeriksa { get; set; }
        public string? KodeKantorTujuan { get; set; }
        public string? KodeKantorEkspor { get; set; }
        public string? KodeJenisImpor { get; set; }
        public string? KodeJenisEkspor { get; set; }
        public string? KodeJenisTPB { get; set; }
        public string? KodeJenisPLB { get; set; }
        public string? KodeJenisProsedur { get; set; }
        public string? KodeTujuanPemasukan { get; set; }
        public string? KodeTujuanPengiriman { get; set; }
        public string? KodeTujuanTPB { get; set; }
        public string? KodeCaraDagang { get; set; }
        public string? KodeCaraBayar { get; set; }
        public string? KodeCaraBayarLainnya { get; set; }
        public string? KodeGudangAsal { get; set; }
        public string? KodeGudangTujuan { get; set; }
        public string? KodeJenisKirim { get; set; }
        public string? KodeJenisPengiriman { get; set; }
        public string? KodeKategoriEkspor { get; set; }
        public string? KodeKategoriMasukFTZ { get; set; }
        public string? KodeKategoriKeluarFTZ { get; set; }
        public string? KodeKategoriBarang { get; set; }
        public string? KodeLokasi { get; set; }
        public string? KodeLokasiBayar { get; set; }
        public string? LokasiAsal { get; set; }
        public string? LokasiTujuan { get; set; }
        public string? KodeDaerahAsal { get; set; }
        public string? KodeGudangAsal_1 { get; set; }
        public string? KodeGudangTujuan_1 { get; set; }
        public string? KodeNegaraTujuan { get; set; }
        public string? KodeTutupPU { get; set; }
        public string? NomorBC11 { get; set; }
        public DateTime? TanggalBC11 { get; set; }
        public string? NomorPos { get; set; }
        public string? NomorSubPos { get; set; }
        public string? KodePelabuhanBongkar { get; set; }
        public string? KodePelabuhanMuat { get; set; }
        public string? KodePelabuhanMuatAkhir { get; set; }
        public string? KodePelabuhanTransit { get; set; }
        public string? KodePelabuhanTujuan { get; set; }
        public string? KodePelabuhanEkspor { get; set; }
        public string? KodeTPS { get; set; }
        public DateTime? TanggalBerangkat { get; set; }
        public DateTime? TanggalEkspor { get; set; }
        public DateTime? TanggalMasuk { get; set; }
        public DateTime? TanggalMuat { get; set; }
        public DateTime? TanggalTiba { get; set; }
        public DateTime? TanggalPeriksa { get; set; }
        public string? TempatStuffing { get; set; }
        public DateTime? TanggalStuffing { get; set; }
        public string? KodeTandaPengaman { get; set; }
        public double? JumlahTandaPengaman { get; set; }
        public string? FlagCurah { get; set; }
        public string? FlagSDA { get; set; }
        public string? FlagVD { get; set; }
        public string? FlagAPBK { get; set; }
        public string? FlagMigas { get; set; }
        public string? KodeAsuransi { get; set; }
        public double? Asuransi { get; set; }
        public double? NilaiBarang { get; set; }
        public double? NilaiIncoterm { get; set; }
        public double? NilaiMaklon { get; set; }
        public double? Asuransi_1 { get; set; }
        public double? Freight { get; set; }
        public double? FOB { get; set; }
        public double? BiayaTambahan { get; set; }
        public double? BiayaPengurang { get; set; }
        public double? VD { get; set; }
        public double? CIF { get; set; }
        public double? HargaPenyerahan { get; set; }
        public double? NDPBM { get; set; }
        public double? TotalDanaSawit { get; set; }
        public double? DasarPengenaanPajak { get; set; }
        public string? NilaiJasa { get; set; }
        public double? UangMuka { get; set; }
        public double? Bruto { get; set; }
        public double? Netto { get; set; }
        public double? Volume { get; set; }
        public string? KotaPernyataan { get; set; }
        public DateTime? TanggalPernyataan { get; set; }
        public string? NamaPernyataan { get; set; }
        public string? JabatanPernyataan { get; set; }
        public string? KodeValuta { get; set; }
        public string? KodeIncoterm { get; set; }
        public string? KodeJasaKenaPajak { get; set; }
        public string? NomorBuktiBayar { get; set; }
        public DateTime? TanggalBuktiBayar { get; set; }
        public string? KodeJeniasNilai { get; set; }
        public string? KodeKantorMuat { get; set; }
        public string? NomorDaftar { get; set; }
        public DateTime? TanggalDaftar { get; set; }
        public string? KodeAsalBarangFTC { get; set; }
        public string? KodeTujuanPengeluaran { get; set; }
        public double? PPNPajak { get; set; }
        public string? PPNBM_PAJAK { get; set; }
        public string? TarifPPNPajak { get; set; }
        public string? TarifPPNBMPajak { get; set; }
        public string? BarangTidakBerwujud { get; set; }
        public string? KodeJenisPengeluaran { get; set; }
    }

    public class ResponseDataCeisaBahanBaku
    {
        public string NomorAju { get; set; }
        public string SeriBarang { get; set; }
        public string SeriBahanBaku { get; set; }
        public string KodeAsalBahanBaku { get; set; }
        public string HS { get; set; }
        public string KodeBarang { get; set; }
        public string Uraian { get; set; }
        public string Merek { get; set; }
        public string Tipe { get; set; }
        public string Ukuran { get; set; }
        public string SpesifikasiLain { get; set; }
        public string KodeSatuan { get; set; }
        public string JumlahSatuan { get; set; }
        public string KodeKemasan { get; set; }
        public string JumlahKemasan { get; set; }
        public string KodeDokumenAsli { get; set; }
        public string KodeKantorAsal { get; set; }
        public string NomorDaftarAsal { get; set; }
        public DateTime? TanggalDaftarAsal { get; set; }
        public string NomorAjuAsal { get; set; }
        public string SeriBarangAsal { get; set; }
        public string Netto { get; set; }
        public string Bruto { get; set; }
        public string Volume { get; set; }
        public string CIF { get; set; }
        public string CIFRupiah { get; set; }
        public string NDPBM { get; set; }
        public string HargaPenyerahan { get; set; }
        public string HargaPerolehan { get; set; }
        public string NilaiJasa { get; set; }
        public string SeriIzin { get; set; }
        public string Valuta { get; set; }
        public string KodeBKC { get; set; }
        public string KodeKomoditiBKC { get; set; }
        public string KodeSubKomoditiBKC { get; set; }
        public string FlagTIS { get; set; }
        public string IsiPerkemasan { get; set; }
        public string JumlahDilekatkan { get; set; }
        public string JumlahPitaCukai { get; set; }
        public string HJECukai { get; set; }
        public string TarikCukai { get; set; }
    }

    public class ResponseDataCeisaBahanBakuDokumen
    {
        public string NomorAju { get; set; }
        public string SeriBarang { get; set; }
        public string SeriBahanBaku { get; set; }
        public string KodeAsalBahanBaku { get; set; }
        public string SeriDokumen { get; set; }
        public string SeriIzin { get; set; }
    }

    public class ResponseDataCeisaBahanBakuTarif
    {
        public string NomorAju { get; set; }
        public string SeriBarang { get; set; }
        public string SeriBahanBaku { get; set; }
        public string KodeAsalBahanBaku { get; set; }
        public string KodePungutan { get; set; }
        public string KodeTarif { get; set; }
        public string Tarif { get; set; }
        public string KodeFasilitas { get; set; }
        public string TarifFasilitas { get; set; }
        public string NilaiBayar { get; set; }
        public string NilaiFasilitas { get; set; }
        public string NilaiSudahDilunasi { get; set; }
        public string KodeSatuan { get; set; }
        public string JumlahSatuan { get; set; }
        public string FagBMTSementara { get; set; }
        public string KodeKomoditiCukai { get; set; }
        public string KodeSubKomoditiCukai { get; set; }
        public string KodeSubKomoditiCukai_1 { get; set; }
        public string FlagTIS_1 { get; set; }
        public string FlagPelekatan_1 { get; set; }
        public string KodeKemasan_1 { get; set; }
        public string FlagTIS { get; set; }
        public string FlagPelekatan { get; set; }
        public string KodeKemasan { get; set; }
        public string JumlahKemasan { get; set; }
    }

    public class ResponseDataCeisaBankDevisa
    {
        public string NomorAju { get; set; }
        public string Seri { get; set; }
        public string Kode { get; set; }
        public string Nama { get; set; }
    }

}
