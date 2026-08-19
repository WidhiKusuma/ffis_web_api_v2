-- ============================================================
-- Migration: filter Divisi untuk pencarian cms_ylid (vendor_cost_db)
-- Tambah parameter p_division ke 4 fungsi search yang dipakai mobile.
--
-- Kompatibel-mundur: parameter p_division ditaruh di akhir dengan
-- DEFAULT '', sehingga pemanggilan lama (web app) tetap jalan.
-- p_division = '' atau 'ALL'  -> tanpa filter divisi.
--
-- Jalankan terhadap database vendor_cost_db.
-- ============================================================

-- Hapus signature lama supaya tidak ambigu dengan overload baru.
DROP FUNCTION IF EXISTS sp_cost_get_distinct_pickups();
DROP FUNCTION IF EXISTS sp_cost_get_distinct_destinations(TEXT);
DROP FUNCTION IF EXISTS sp_cost_search_truck_types(TEXT, TEXT, TEXT);
DROP FUNCTION IF EXISTS sp_cost_search_prices(TEXT, TEXT, TEXT, TEXT);

-- 5.9  Get distinct pickup locations (opsional filter divisi)
CREATE OR REPLACE FUNCTION sp_cost_get_distinct_pickups(p_division TEXT DEFAULT '')
RETURNS TABLE (pickup TEXT)
LANGUAGE sql STABLE AS $$
    SELECT DISTINCT mc.pickup
    FROM   master_cost mc
    WHERE  (p_division = '' OR p_division = 'ALL' OR UPPER(mc.division) = UPPER(p_division))
    ORDER  BY mc.pickup;
$$;

-- 5.10 Get distinct destinations (opsional filter pickup + divisi)
CREATE OR REPLACE FUNCTION sp_cost_get_distinct_destinations(
    p_pickup   TEXT DEFAULT '',
    p_division TEXT DEFAULT ''
)
RETURNS TABLE (destination TEXT)
LANGUAGE sql STABLE AS $$
    SELECT DISTINCT mc.destination
    FROM   master_cost mc
    WHERE  (p_pickup = '' OR UPPER(mc.pickup) = UPPER(p_pickup))
      AND  (p_division = '' OR p_division = 'ALL' OR UPPER(mc.division) = UPPER(p_division))
    ORDER  BY mc.destination;
$$;

-- 5.11 Truck type columns for a route (opsional filter divisi)
CREATE OR REPLACE FUNCTION sp_cost_search_truck_types(
    p_pickup       TEXT,
    p_destination  TEXT,
    p_vehicle_type TEXT,
    p_division     TEXT DEFAULT ''
)
RETURNS TABLE (
    name       TEXT,
    sort_order INTEGER
)
LANGUAGE sql STABLE AS $$
    SELECT DISTINCT tt.name, tt.sort_order
    FROM   truck_types tt
    JOIN   cost_prices cp ON cp.truck_type_id = tt.id
    JOIN   master_cost mc ON mc.id            = cp.cost_id
    WHERE  UPPER(mc.pickup)      = UPPER(p_pickup)
      AND  UPPER(mc.destination) = UPPER(p_destination)
      AND  tt.vehicle_type       = p_vehicle_type
      AND  (p_division = '' OR p_division = 'ALL' OR UPPER(mc.division) = UPPER(p_division))
    ORDER  BY tt.sort_order, tt.name;
$$;

-- 5.12 Vendor x truck-type price rows (opsional filter divisi)
CREATE OR REPLACE FUNCTION sp_cost_search_prices(
    p_pickup       TEXT,
    p_destination  TEXT,
    p_vehicle_type TEXT,
    p_jenis_usaha  TEXT DEFAULT '',
    p_division     TEXT DEFAULT ''
)
RETURNS TABLE (
    vendor_id   INTEGER,
    vendor_name TEXT,
    jenis_usaha TEXT,
    truck_type  TEXT,
    sort_order  INTEGER,
    price       NUMERIC
)
LANGUAGE sql STABLE AS $$
    SELECT v.id, v.name, v.jenis_usaha,
           tt.name, tt.sort_order, cp.price
    FROM   master_cost mc
    JOIN   vendors     v  ON v.id  = mc.vendor_id
    JOIN   cost_prices cp ON cp.cost_id      = mc.id
    JOIN   truck_types tt ON tt.id = cp.truck_type_id
    WHERE  UPPER(mc.pickup)      = UPPER(p_pickup)
      AND  UPPER(mc.destination) = UPPER(p_destination)
      AND  tt.vehicle_type       = p_vehicle_type
      AND  (p_jenis_usaha = '' OR v.jenis_usaha LIKE '%' || p_jenis_usaha || '%')
      AND  (p_division = '' OR p_division = 'ALL' OR UPPER(mc.division) = UPPER(p_division))
    ORDER  BY v.name, tt.sort_order;
$$;

-- ============================================================
-- END migration
-- ============================================================
