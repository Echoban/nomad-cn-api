-- ============================================================
-- NomadCN 数据库建表脚本
-- 数据库: MySQL 8.0+
-- ============================================================

CREATE DATABASE IF NOT EXISTS learn_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE learn_db;

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
    Tags TEXT,
    ClimateType VARCHAR(20) DEFAULT '',
    Description LONGTEXT,
    Latitude DECIMAL(9,6) DEFAULT NULL,
    Longitude DECIMAL(9,6) DEFAULT NULL,
    HousingPrice INT DEFAULT 0,
    DeepLiving LONGTEXT,
    DeepCommunity LONGTEXT,
    DeepTips LONGTEXT,
    DeepBestSeason VARCHAR(200) DEFAULT '',
    DeepCons LONGTEXT,
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

-- 地图标注表（所有用户共享可见）
CREATE TABLE IF NOT EXISTS map_annotations (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CityName VARCHAR(50) NOT NULL,
    Type VARCHAR(20) NOT NULL,
    Latitude DECIMAL(9,6) NOT NULL,
    Longitude DECIMAL(9,6) NOT NULL,
    Content TEXT,
    Color VARCHAR(20) DEFAULT '#00d9a3',
    `Path` TEXT,
    Username VARCHAR(20),
    UserId INT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX IX_map_annotations_CityName (CityName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ===== 城市评论表 =====
CREATE TABLE IF NOT EXISTS city_comments (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CityName VARCHAR(50) NOT NULL,
    Content VARCHAR(500) NOT NULL,
    Rating INT DEFAULT 5,
    Username VARCHAR(50) NOT NULL,
    AvatarLetter VARCHAR(5) DEFAULT 'U',
    AvatarColor VARCHAR(20) DEFAULT '#00d9a5',
    UserId INT NULL,
    IsBot TINYINT(1) DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX IX_city_comments_CityName (CityName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
