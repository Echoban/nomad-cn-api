# 游民中国 · NomadCN — 后端 API

中国数字游民城市指南后端服务，提供城市数据、用户认证、喜欢功能等 API。

## 技术栈

- .NET 8 / ASP.NET Core
- Entity Framework Core + MySQL (Pomelo 驱动)
- Redis 缓存
- JWT 认证
- BCrypt 密码加密
- Swagger API 文档

## 快速开始

### 1. 环境准备

- .NET 8 SDK
- MySQL 8.0+
- Redis

### 2. 数据库初始化

```bash
# 创建数据库和用户
mysql -u root -e "
  CREATE DATABASE nomadcn CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
  CREATE USER 'nomadcn'@'localhost' IDENTIFIED BY 'nomadcn2026';
  GRANT ALL PRIVILEGES ON nomadcn.* TO 'nomadcn'@'localhost';
  FLUSH PRIVILEGES;
"

# 执行建表脚本
mysql -u nomadcn -pnomadcn2026 nomadcn < database/schema.sql

# 生成种子数据（需安装依赖）
pip install bcrypt mysql-connector-python pypinyin
python3 database/seed_data.py
```

### 3. 配置

编辑 `appsettings.json`：

```json
{
  "ConnectionStrings": {
    "MySQL": "Server=127.0.0.1;Port=3306;Database=nomadcn;User=nomadcn;Password=nomadcn2026;",
    "Redis": "127.0.0.1:6379"
  }
}
```

### 4. 启动

```bash
dotnet run --urls "http://localhost:5118"
```

API 文档: `http://localhost:5118/swagger`

## 项目结构

```
nomad-cn-api/
├── Controllers/
│   ├── AuthController.cs      # 认证控制器
│   ├── CitiesController.cs    # 城市数据控制器
│   └── LikesController.cs     # 喜欢功能控制器
├── Data/
│   └── AppDbContext.cs         # EF Core 数据库上下文
├── Models/
│   ├── City.cs                 # 城市模型
│   ├── CityLike.cs             # 喜欢记录模型
│   ├── User.cs                 # 用户模型
│   └── Dtos.cs                 # 数据传输对象
├── Services/
│   ├── AuthService.cs          # 认证服务（JWT）
│   ├── CityService.cs          # 城市服务（Redis 缓存）
│   └── RedisCacheService.cs    # Redis 缓存服务
├── database/
│   ├── schema.sql              # 数据库建表脚本
│   └── seed_data.py            # 种子数据生成脚本
├── Program.cs                  # 应用入口
└── appsettings.json            # 配置文件
```

## API 接口

| 接口 | 方法 | 认证 | 说明 |
|------|------|------|------|
| `/api/cities` | GET | - | 获取所有城市（Redis 缓存 1h） |
| `/api/cities/{name}` | GET | - | 按名称获取城市详情 |
| `/api/cities/search/{keyword}` | GET | - | 搜索城市 |
| `/api/auth/register` | POST | - | 用户注册 |
| `/api/auth/login` | POST | - | 用户登录 |
| `/api/auth/me` | GET | Bearer | 获取当前用户信息 |
| `/api/likes/toggle/{cityName}` | POST | Bearer | 切换喜欢状态 |
| `/api/likes/my` | GET | Bearer | 获取已喜欢城市列表 |

## 测试用户

种子脚本会创建以下测试用户：

| 用户名 | 密码 | 邮箱 |
|--------|------|------|
| webuser | web123456 | web@test.com |
| apiuser | api123456 | api@test.com |
