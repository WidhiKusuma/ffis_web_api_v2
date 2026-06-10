-- Jalankan ini di DBeaver / pgAdmin Anda (Database: cbm_db)
-- Ini akan membuat fungsi login baru yang HANYA membutuhkan nomor telepon

CREATE OR REPLACE FUNCTION fn_cbm_login_by_phone(p_phone VARCHAR)
RETURNS TABLE (
    user_id INT,
    user_name VARCHAR,
    user_role VARCHAR,
    user_department VARCHAR
) AS $$
BEGIN
    RETURN QUERY 
    SELECT id, name, role, department 
    FROM cbm_users 
    WHERE phone_number = p_phone AND is_active = TRUE;
END;
$$ LANGUAGE plpgsql;
