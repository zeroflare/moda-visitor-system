-- 創建資料庫（如果不存在）
CREATE DATABASE IF NOT EXISTS twdiw_visitor CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE twdiw_visitor;

-- 創建櫃檯表
CREATE TABLE IF NOT EXISTS counters (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(200) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 創建會議室表
CREATE TABLE IF NOT EXISTS meetingrooms (
    id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    counter_id VARCHAR(50) NOT NULL,
    INDEX idx_counter_id (counter_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 插入範例資料
INSERT INTO counters (id, name) VALUES
('sk-17f', '新光大樓17樓櫃檯'),
('sk-19f', '新光大樓19樓櫃檯'),
('sk-20f', '新光大樓20樓櫃檯'),
('yp-1f', '延平大樓1樓櫃檯')
ON DUPLICATE KEY UPDATE name=VALUES(name);

-- 插入會議室範例資料
INSERT INTO meetingrooms (id, name, counter_id) VALUES
('c_18895ju1j9daejhggjql9brqds95s@resource.calendar.google.com', '新光大樓17A01會議室', 'sk-17f'),
('c_18895ju1j9daejhggjql9brqds95s@resource.calendar.google.com', '延平大樓201會議室', 'yp-1f')
ON DUPLICATE KEY UPDATE name=VALUES(name), counter_id=VALUES(counter_id);

