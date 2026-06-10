-- 1. Create cbm_locations table
CREATE TABLE IF NOT EXISTS cbm_locations (
    id SERIAL PRIMARY KEY,
    location_name VARCHAR(255) NOT NULL,
    address TEXT NOT NULL,
    latitude DECIMAL(10, 8),
    longitude DECIMAL(11, 8),
    location_type VARCHAR(50) DEFAULT 'General'
);

-- 2. Clear existing data to avoid duplicates if run multiple times
TRUNCATE TABLE cbm_locations RESTART IDENTITY;

-- 3. Insert seed data
INSERT INTO cbm_locations (location_name, address, latitude, longitude, location_type) VALUES
('PT Yusen Logistics Benda', 'Jalan Raya Bandara, Soewarna Business Park, Pajang, Benda, Tangerang, Banten, 19120', -6.1243701, 106.6659409, 'Kantor Internal'),
('PT Yusen Logistics Tanjung Priok', 'Pelabuhan Tanjung Priok, Jakarta Utara', -6.102718, 106.883733, 'Gudang Internal'),
('Bursa Efek Jakarta (BEJ)', 'Gedung Bursa Efek Indonesia, SCBD, Jl. Jend. Sudirman, Jakarta Selatan', -6.224856, 106.809312, 'Publik'),
('Bandara Soekarno Hatta', 'Bandara Internasional Soekarno-Hatta, Tangerang, Banten', -6.125556, 106.655833, 'Publik'),
('Kementerian Perdagangan RI', 'Jl. M.I. Ridwan Rais No.5, Gambir, Jakarta Pusat', -6.177265, 106.833890, 'Klien');

-- 4. Create stored procedure / function for searching
CREATE OR REPLACE FUNCTION fn_cbm_search_locations(p_keyword VARCHAR)
RETURNS TABLE (
    id INT,
    location_name VARCHAR,
    address TEXT,
    latitude DECIMAL,
    longitude DECIMAL,
    location_type VARCHAR
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        l.id, 
        l.location_name, 
        l.address, 
        l.latitude, 
        l.longitude, 
        l.location_type
    FROM cbm_locations l
    WHERE l.location_name ILIKE '%' || p_keyword || '%' 
       OR l.address ILIKE '%' || p_keyword || '%'
    ORDER BY 
        -- Prioritize exact match or start with
        CASE WHEN l.location_name ILIKE p_keyword || '%' THEN 1 ELSE 2 END,
        l.location_name ASC
    LIMIT 10;
END;
$$ LANGUAGE plpgsql;
