-- Migration: Create visitor_profiles table
-- Description: Create a new table to store visitor profile information with email as primary key

CREATE TABLE IF NOT EXISTS visitor_profiles (
    email VARCHAR(255) PRIMARY KEY,
    name VARCHAR(200) NULL,
    company VARCHAR(200) NULL,
    phone VARCHAR(50) NULL,
    cid VARCHAR(50) NULL,
    created_at DATETIME NULL,
    updated_at DATETIME NULL,
    expires_at DATETIME NULL,
    INDEX idx_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

