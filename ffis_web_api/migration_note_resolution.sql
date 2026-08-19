-- Migration: penyelesaian catatan persetujuan (audit trail)
-- Menambahkan kolom pelacakan tindak lanjut driver + verifikasi admin transport
-- pada setiap catatan approve/reject di P2HApprovalLog.
--
-- SUPERSEDED: granularitas per-approval ini digantikan oleh
-- migration_note_resolution_detail.sql (per-item temuan di P2HDetail).
-- Kolom di bawah ini TIDAK dipakai lagi oleh kode aplikasi. Aman dibiarkan
-- apa adanya di DB yang sudah menjalankan migrasi ini (tidak perlu di-drop).

ALTER TABLE P2HApprovalLog ADD
    ResolutionStatus NVARCHAR(30) NOT NULL DEFAULT 'Open',
    DriverResolvedBy NVARCHAR(100) NULL,
    DriverResolvedTime DATETIME NULL,
    DriverResolutionNote NVARCHAR(500) NULL,
    AdminVerifiedBy NVARCHAR(100) NULL,
    AdminVerifiedTime DATETIME NULL;
