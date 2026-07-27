-- ============================================================
-- NomadCN 数据库建表脚本
-- 数据库: MySQL 8.0+
-- ============================================================

CREATE DATABASE IF NOT EXISTS nomadcn CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE nomadcn;

-- 创建用户（如需）
-- CREATE USER IF NOT EXISTS 'nomadcn'@'%' IDENTIFIED BY 'nomadcn2026';
-- GRANT ALL PRIVILEGES ON nomadcn.* TO 'nomadcn'@'%';
-- FLUSH PRIVILEGES;

-- ===== 城市表 =====
CREATE TABLE IF NOT EXISTS cities (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL,
    Region VARCHAR(20) NOT NULL,
    Tier VARCHAR(20) NOT NULL,
    Flag VARCHAR(10) DEFAULT '🇨🇳',
    Cost INT DEFAULT 0,
    Rent INT DEFAULT 0,
    Food INT DEFAULT 0,
    Transport INT DEFAULT 0,
    Internet INT DEFAULT 0,
    Broadband VARCHAR(50) DEFAULT '',
    Mobile VARCHAR(50) DEFAULT '',
    Climate VARCHAR(20) DEFAULT '',
    TempAvg INT DEFAULT 0,
    AirQuality INT DEFAULT 0,
    AqiLevel VARCHAR(10) DEFAULT '',
    Safety INT DEFAULT 0,
    Healthcare INT DEFAULT 0,
    Walkability INT DEFAULT 0,
    Nightlife INT DEFAULT 0,
    Coffee INT DEFAULT 0,
    Coworking INT DEFAULT 0,
    Tags TEXT DEFAULT '[]',
    ClimateType VARCHAR(20) DEFAULT '',
    Description LONGTEXT,
    Latitude DECIMAL(9,6) DEFAULT NULL,
    Longitude DECIMAL(9,6) DEFAULT NULL,
    HousingPrice INT DEFAULT 0,
    DeepLiving LONGTEXT,
    DeepCommunity LONGTEXT,
    DeepTips LONGTEXT,
    DeepBestSeason VARCHAR(200) DEFAULT '',
    Score INT DEFAULT 0,
    Likes INT DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE INDEX IX_cities_Name (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ===== 用户表 =====
CREATE TABLE IF NOT EXISTS users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    AvatarLetter VARCHAR(5) DEFAULT '',
    AvatarColor VARCHAR(20) DEFAULT '',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE INDEX IX_users_Username (Username),
    UNIQUE INDEX IX_users_Email (Email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ===== 城市喜欢记录表 =====
CREATE TABLE IF NOT EXISTS city_likes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    CityId INT NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE INDEX IX_city_likes_UserId_CityId (UserId, CityId),
    CONSTRAINT FK_CityLikes_User FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CityLikes_City FOREIGN KEY (CityId) REFERENCES cities(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
