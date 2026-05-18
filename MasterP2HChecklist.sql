
-- Create Master Table for P2H Checklist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MasterP2HChecklist]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MasterP2HChecklist] (
        [Oid] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [ItemCode] NVARCHAR(50) UNIQUE NOT NULL,
        [Category] NVARCHAR(100) NOT NULL,
        [Question] NVARCHAR(MAX) NOT NULL,
        [IsActive] BIT DEFAULT 1,
        [DisplayOrder] INT NOT NULL,
        [CreatedAt] DATETIME DEFAULT GETDATE()
    );
END
GO

-- Truncate existing data to avoid duplicates if re-run
TRUNCATE TABLE [dbo].[MasterP2HChecklist];
GO

-- Insert items from P2H Checklist Excel
INSERT INTO [dbo].[MasterP2HChecklist] ([ItemCode], [Category], [Question], [DisplayOrder]) VALUES
('P2H-01', 'AC/Pendingin', 'Apakah kendaraan berpendingin (AC) ?', 1),
('P2H-02', 'AC/Pendingin', 'Pendingin / AC berfungsi dengan baik?', 2),
('P2H-03', 'AC/Pendingin', 'Data suhu tersedia dan dapat di akses dengan mudah?', 3),
('P2H-04', 'AC/Pendingin', 'Pengatur suhu berfungsi dengan baik?', 4),
('P2H-05', 'AC/Pendingin', 'Penunjuk suhu berfungsi dengan baik?', 5),
('P2H-06', 'Dokumen', 'SIM masih berlaku', 6),
('P2H-07', 'Dokumen', 'STNK & Pajak Kendaraan masih berlaku', 7),
('P2H-08', 'Dokumen', 'KIR Kendaraan masih berlaku', 8),
('P2H-09', 'Personal', 'Seragam tersedia dalam kondisi baik?', 9),
('P2H-10', 'Personal', 'Sudah beristirahat dengan cukup?', 10),
('P2H-11', 'Personal', 'Kondisi kesehatan baik sebelum mengemudi?', 11),
('P2H-12', 'SPILL KIT / Tools', 'Memerlukan SPILL KIT?', 12),
('P2H-13', 'SPILL KIT / Tools', 'Pasir tersedia dalam jumlah cukup?', 13),
('P2H-14', 'SPILL KIT / Tools', 'Sepatu Karet tersedia dalam kondisi baik?', 14),
('P2H-15', 'SPILL KIT / Tools', 'Sapu Lidi tersedia dalam kondisi baik?', 15),
('P2H-16', 'SPILL KIT / Tools', 'Kantong Sampah / Trash Bag tersedia dalam kondisi baik?', 16),
('P2H-17', 'SPILL KIT / Tools', 'Sekop tersedia dalam kondisi baik?', 17),
('P2H-18', 'SPILL KIT / Tools', 'Sarung Tangan Karet tersedia dalam kondisi baik dan lengkap?', 18),
('P2H-19', 'APD', 'Helm Keselamatan tersedia dalam kondisi baik?', 19),
('P2H-20', 'APD', 'Kacamata Pelindung tersedia dalam kondisi baik?', 20),
('P2H-21', 'APD', 'Sepatu Keselamatan / Sepatu Safety tersedia dalam kondisi baik dan lengkap?', 21),
('P2H-22', 'P3K', 'Obat Antiseptik tersedia dalam kondisi baik dan tidak kadaluarsa?', 22),
('P2H-23', 'P3K', 'Kain kasa tersedia dalam kondisi baik dan steril?', 23),
('P2H-24', 'P3K', 'Kapas tersedia dalam jumlah yang cukup dan steril?', 24),
('P2H-25', 'P3K', 'Plester tersedia dalam kondisi baik?', 25),
('P2H-26', 'P3K', 'Perban tersedia dalam kondisi baik dan steril?', 26),
('P2H-27', 'APAR', 'Isi APAR dalam kondisi baik?', 27),
('P2H-28', 'APAR', 'Selang APAR dalam kondisi baik?', 28),
('P2H-29', 'APAR', 'Tekanan APAR dalam kondisi baik?', 29),
('P2H-30', 'APAR', 'Corong APAR dalam kondisi baik?', 30),
('P2H-31', 'APAR', 'Segel/Pin APAR masih terpasang dengan baik?', 31),
('P2H-32', 'APAR', 'Isi APAR masih berlaku?', 32),
('P2H-33', 'Instrumen', 'Speedometer berfungsi dengan baik?', 33),
('P2H-34', 'Instrumen', 'Odometer berfungsi dengan baik?', 34),
('P2H-35', 'Kebersihan & Eksterior', 'Bagian dalam kendaraan dalam kondisi bersih?', 35),
('P2H-36', 'Kebersihan & Eksterior', 'Bagian luar kendaraan dalam kondisi bersih?', 36),
('P2H-37', 'Kebersihan & Eksterior', 'Badan kendaraan dalam keadaan baik?', 37),
('P2H-38', 'Kebersihan & Eksterior', 'Wiper berfungsi dengan baik?', 38),
('P2H-39', 'Kebersihan & Eksterior', 'Kaca depan kendaraan dalam keadaan bersih dan baik?', 39),
('P2H-40', 'Kebersihan & Eksterior', 'Spion kendaraan dalam keadaan lengkap dan baik?', 40),
('P2H-41', 'Kontrol & Mekanis', 'Panel kontrol kendaraan berfungsi dengan baik?', 41),
('P2H-42', 'Kontrol & Mekanis', 'Sabuk pengaman 3 (tiga) titik dalam keadaan baik dan lengkap?', 42),
('P2H-43', 'Kontrol & Mekanis', 'Pedal gas berfungsi dengan baik?', 43),
('P2H-44', 'Kontrol & Mekanis', 'Klakson berfungsi dengan baik?', 44),
('P2H-45', 'Kontrol & Mekanis', 'Alat Kemudi / Stir berfungsi dengan baik?', 45),
('P2H-46', 'Kontrol & Mekanis', 'Peralatan GPS berfungsi dengan baik?', 46),
('P2H-47', 'Kontrol & Mekanis', 'Seluruh Lampu Indikator berfungsi dengan baik?', 47),
('P2H-48', 'Kontrol & Mekanis', 'ACCU / Aki kendaraan berfungsi dengan baik?', 48),
('P2H-49', 'Kontrol & Mekanis', 'Kopling kendaraan berfungsi dengan baik?', 49),
('P2H-50', 'Mesin & Rem', 'Kondisi Mesin (OLI, Radiator, dan Suara Mesin) baik tanpa ada permasalahan?', 50),
('P2H-51', 'Mesin & Rem', 'Rem / break berfungsi dengan baik?', 51),
('P2H-52', 'Lampu', 'Lampu Rem / break berfungsi dengan baik?', 52),
('P2H-53', 'Lampu', 'Lampu Rotary berfungsi dengan baik?', 53),
('P2H-54', 'Lampu', 'Lampu utama berfungsi dengan baik?', 54),
('P2H-55', 'Lampu', 'Lampu sign / riting tersedia lengkap dan berfungsi dengan baik?', 55),
('P2H-56', 'Emergency', 'Alarm mundur tersedia dan berfungsi dengan baik?', 56),
('P2H-57', 'Box & Kabin', 'Hidrolik wing / pintu samping seluruhnya berfungsi dengan baik?', 57),
('P2H-58', 'Box & Kabin', 'Kendaraan bersih dari hama?', 58),
('P2H-59', 'Box & Kabin', 'Tidak ada kebocoran pada Kabin dan Box?', 59),
('P2H-60', 'Ban', 'Kondisi ban kendaraan yang terpasang seluruhnya dalam keadaan baik?', 60),
('P2H-61', 'Ban', 'Kondisi mur roda yang terpasang lengkap, kencang dan dalam keadaan baik?', 61),
('P2H-62', 'Ban', 'Ban Cadangan tersedia dan dalam kondisi baik?', 62),
('P2H-63', 'Keamanan & Peralatan', 'Nomor telepon darurat tersedia dan mudah di lihat?', 63),
('P2H-64', 'Keamanan & Peralatan', 'Dongkrak dan kunci-kunci kendaraan lengkap dan berfungsi dengan baik?', 64),
('P2H-65', 'Keamanan & Peralatan', 'Gembok atau pengaman pintu tersedia dan berfungsi dengan baik?', 65),
('P2H-66', 'Keamanan & Peralatan', 'Uji Emisi sudah di lakukan?', 66),
('P2H-67', 'Keamanan & Peralatan', '2 buah segitiga pengaman / 4 buah kerucut tersedia lengkap dan dalam kondisi baik?', 67),
('P2H-68', 'Keamanan & Peralatan', 'Senter dalam kondisi terang dan berfungsi dengan baik?', 68),
('P2H-69', 'Keamanan & Peralatan', '2 buah pengganjal ban tersedia lengkap dan berfungsi dengan baik?', 69),
('P2H-70', 'Keamanan & Peralatan', 'Tali Pengikat Barang tersedia dan berfungsi dengan baik?', 70),
('P2H-71', 'Keluhan', 'Apakah ada keluhan terkait kondisi kendaraan maupun sarana pendukung yang tidak normal? Ceritakan secara rinci.', 71);
GO

SELECT * FROM [dbo].[MasterP2HChecklist] ORDER BY DisplayOrder;
GO
