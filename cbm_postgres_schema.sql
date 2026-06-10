-- ==========================================
-- CBM DATABASE SCHEMA & FUNCTIONS (POSTGRESQL)
-- ==========================================

-- 1. TABLES
CREATE TABLE IF NOT EXISTS cbm_users (
    id SERIAL PRIMARY KEY,
    phone_number VARCHAR(20) UNIQUE NOT NULL,
    name VARCHAR(100) NOT NULL,
    role VARCHAR(20) NOT NULL, -- 'Employee', 'Driver', 'Admin'
    department VARCHAR(50),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS cbm_orders (
    id SERIAL PRIMARY KEY,
    order_number VARCHAR(20) UNIQUE NOT NULL,
    user_id INT REFERENCES cbm_users(id),
    service_type VARCHAR(20) NOT NULL, -- 'CBM Car', 'CBM Send'
    status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Assigned', 'InProgress', 'Completed', 'Cancelled'
    pickup_location VARCHAR(255) NOT NULL,
    dropoff_location VARCHAR(255) NOT NULL,
    driver_id INT REFERENCES cbm_users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    assigned_at TIMESTAMP NULL,
    completed_at TIMESTAMP NULL
);


-- 2. FUNCTIONS (STORED PROCEDURES)

-- A. Login Function
CREATE OR REPLACE FUNCTION fn_cbm_login(p_phone VARCHAR, p_role VARCHAR)
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
    WHERE phone_number = p_phone AND role = p_role AND is_active = TRUE;
END;
$$ LANGUAGE plpgsql;

-- B. Create Order Function
CREATE OR REPLACE FUNCTION fn_cbm_create_order(
    p_user_id INT, 
    p_service_type VARCHAR, 
    p_pickup VARCHAR, 
    p_dropoff VARCHAR
)
RETURNS VARCHAR AS $$
DECLARE 
    v_order_number VARCHAR;
BEGIN
    -- Generate simple order number (e.g. CBM-20260609-XXXX)
    v_order_number := 'CBM-' || TO_CHAR(NOW(), 'YYYYMMDD') || '-' || LPAD(CAST(CAST(RANDOM()*1000 AS INT) AS VARCHAR), 4, '0');
    
    INSERT INTO cbm_orders (order_number, user_id, service_type, pickup_location, dropoff_location)
    VALUES (v_order_number, p_user_id, p_service_type, p_pickup, p_dropoff);
    
    RETURN v_order_number;
END;
$$ LANGUAGE plpgsql;

-- C. Get Active Orders for User
CREATE OR REPLACE FUNCTION fn_cbm_get_user_active_orders(p_user_id INT)
RETURNS TABLE (
    order_number VARCHAR,
    service_type VARCHAR,
    status VARCHAR,
    pickup_location VARCHAR,
    dropoff_location VARCHAR,
    driver_name VARCHAR,
    created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY 
    SELECT 
        o.order_number, o.service_type, o.status, o.pickup_location, o.dropoff_location,
        COALESCE(d.name, '') as driver_name,
        o.created_at
    FROM cbm_orders o
    LEFT JOIN cbm_users d ON o.driver_id = d.id
    WHERE o.user_id = p_user_id AND o.status IN ('Pending', 'Assigned', 'InProgress')
    ORDER BY o.created_at DESC;
END;
$$ LANGUAGE plpgsql;

-- D. Get Pending Orders (For Admin)
CREATE OR REPLACE FUNCTION fn_cbm_get_pending_orders()
RETURNS TABLE (
    order_id INT,
    order_number VARCHAR,
    service_type VARCHAR,
    passenger_name VARCHAR,
    pickup_location VARCHAR,
    dropoff_location VARCHAR,
    created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY 
    SELECT 
        o.id, o.order_number, o.service_type, u.name as passenger_name, 
        o.pickup_location, o.dropoff_location, o.created_at
    FROM cbm_orders o
    JOIN cbm_users u ON o.user_id = u.id
    WHERE o.status = 'Pending'
    ORDER BY o.created_at ASC;
END;
$$ LANGUAGE plpgsql;

-- E. Assign Driver
CREATE OR REPLACE FUNCTION fn_cbm_assign_driver(p_order_id INT, p_driver_id INT)
RETURNS BOOLEAN AS $$
BEGIN
    UPDATE cbm_orders
    SET status = 'Assigned', driver_id = p_driver_id, assigned_at = CURRENT_TIMESTAMP
    WHERE id = p_order_id AND status = 'Pending';
    
    IF FOUND THEN
        RETURN TRUE;
    ELSE
        RETURN FALSE;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- 3. INITIAL SEED DATA (For Testing)
INSERT INTO cbm_users (phone_number, name, role, department) 
VALUES 
('0811111', 'Andi (IT)', 'Employee', 'IT'),
('0822222', 'Budi Santoso', 'Driver', 'Operations'),
('0833333', 'Admin CBM', 'Admin', 'Dispatcher')
ON CONFLICT DO NOTHING;
