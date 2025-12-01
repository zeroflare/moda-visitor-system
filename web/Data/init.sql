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
('c_18895ju1j9daejhggjql9brqds95s@resource.calendar.google.com', '測試', 'yp-1f'),
('c_1888upnv5dcoej9plukdkt2vin5da@resource.calendar.google.com', '延平101', 'yp-1f'),
('c_1885qvghibsp8jtcjjddg1bmm8f7o@resource.calendar.google.com', '延平102', 'yp-1f'),
('c_1882hannfbj10iepj8720calf8frs@resource.calendar.google.com', '延平301', 'yp-1f'),
('c_188a4h5lk8dg6ikfmt7q9fvovhdvk@resource.calendar.google.com', '延平701', 'yp-1f'),
('c_188450ukn3a24gqem99etnfj3v456@resource.calendar.google.com', '延平801', 'yp-1f'),
('c_1884oebcgfr5ci8aioa0brs4dca0q@resource.calendar.google.com', '延平901', 'yp-1f'),
('c_1884ssao87mgijfji4p1ou9gbffmg@resource.calendar.google.com', '新光17A01', 'sk-17f'),
('c_188dv1bus7btcjf0i4arfo7ermfgc@resource.calendar.google.com', '新光17A02', 'sk-17f'),
('c_1886mb5q1nob0iqgg843p7k39bkj2@resource.calendar.google.com', '新光17B01', 'sk-17f'),
('c_18806726b3hh4gpsleruk42a2ggms@resource.calendar.google.com', '新光17B02', 'sk-17f'),
('c_1888efkm9k40ogjcnu126do50nql2@resource.calendar.google.com', '新光17B03', 'sk-17f'),
('c_1887bgji2ugl8gnjhs320ie2rnit4@resource.calendar.google.com', '新光17B04', 'sk-17f'),
('c_188070uvlik1ggekm3lrapr1m1oh8@resource.calendar.google.com', '新光20A01', 'sk-20f')
ON DUPLICATE KEY UPDATE name=VALUES(name), counter_id=VALUES(counter_id);

-- 創建會議表
CREATE TABLE IF NOT EXISTS meetings (
    id VARCHAR(255) PRIMARY KEY,
    meetingname VARCHAR(200),
    inviter_email VARCHAR(255) NOT NULL,
    inviter_name VARCHAR(200),
    inviter_dept VARCHAR(200),
    inviter_title VARCHAR(200),
    start_at DATETIME NOT NULL,
    end_at DATETIME NOT NULL,
    meetingroom_id VARCHAR(255) NOT NULL,
    INDEX idx_meetingroom_id (meetingroom_id),
    INDEX idx_inviter_email (inviter_email),
    INDEX idx_start_at (start_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 創建訪客表
CREATE TABLE IF NOT EXISTS visitors (
    visitor_email VARCHAR(255) NOT NULL,
    visitor_name VARCHAR(200),
    visitor_phone VARCHAR(50),
    visitor_dept VARCHAR(200),
    checkin_at DATETIME,
    checkout_at DATETIME,
    created_at DATETIME NOT NULL,
    meeting_id VARCHAR(255) NOT NULL,
    PRIMARY KEY (meeting_id, visitor_email),
    INDEX idx_meeting_id (meeting_id),
    INDEX idx_visitor_email (visitor_email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 創建員工表
CREATE TABLE IF NOT EXISTS employees (
    id BIGINT PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    name VARCHAR(200) NOT NULL,
    dept VARCHAR(200),
    costcenter VARCHAR(200),
    title VARCHAR(200),
    UNIQUE KEY uk_email (email),
    INDEX idx_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 創建簽到簽出表
CREATE TABLE IF NOT EXISTS check_logs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    created_at DATETIME NOT NULL,
    type VARCHAR(50) NOT NULL,
    visitor_email VARCHAR(255) NOT NULL,
    visitor_name VARCHAR(200),
    visitor_phone VARCHAR(50),
    visitor_dept VARCHAR(200),
    meeting_id VARCHAR(255) NOT NULL,
    INDEX idx_created_at (created_at),
    INDEX idx_visitor_email (visitor_email),
    INDEX idx_meeting_id (meeting_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 創建 webhook 表
CREATE TABLE IF NOT EXISTS notify_webhooks (
    dept VARCHAR(200) PRIMARY KEY,
    type VARCHAR(50) NOT NULL,
    webhook VARCHAR(500) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 創建使用者表
CREATE TABLE IF NOT EXISTS users (
    email VARCHAR(255) PRIMARY KEY,
    username VARCHAR(200) NOT NULL,
    role VARCHAR(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 創建 secrets 表
CREATE TABLE IF NOT EXISTS secrets (
    id VARCHAR(255) PRIMARY KEY,
    value TEXT NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

