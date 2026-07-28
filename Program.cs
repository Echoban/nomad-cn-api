using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using NomadCN.Api.Data;
using NomadCN.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ===== 数据库 =====
// 优先使用 MySQL（ConnectionStrings:MySQL），其次 PostgreSQL（环境变量 DATABASE_URL），最后回退到 SQLite（零配置部署）
var mysqlConn = builder.Configuration.GetConnectionString("MySQL");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var usePostgres = !string.IsNullOrWhiteSpace(databaseUrl)
    || !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("PostgreSQL"));

if (!string.IsNullOrWhiteSpace(mysqlConn))
{
    // MySQL 模式：使用 Pomelo 驱动连接 MySQL
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseMySql(mysqlConn, serverVersion));
    Console.WriteLine("Using MySQL database");
}
else if (usePostgres)
{
    var rawConn = databaseUrl
        ?? builder.Configuration.GetConnectionString("PostgreSQL")
        ?? builder.Configuration.GetConnectionString("MySQL");
    var postgresConn = ConvertPostgresUrl(rawConn);
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(postgresConn));
    Console.WriteLine("Using PostgreSQL database");
}
else
{
    // SQLite 零配置模式：数据文件存在 /data 目录（容器持久化）或当前目录
    var sqlitePath = "/data/nomadcn.db";
    var sqliteDir = Path.GetDirectoryName(sqlitePath);
    if (!string.IsNullOrEmpty(sqliteDir) && !Directory.Exists(sqliteDir))
    {
        try { Directory.CreateDirectory(sqliteDir); }
        catch { sqlitePath = "nomadcn.db"; } // 回退到当前目录
    }
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlite($"Data Source={sqlitePath}"));
    Console.WriteLine($"Using SQLite database: {sqlitePath}");
}

// 将 postgres://user:pass@host:port/db 转换为 Npgsql 标准格式
static string ConvertPostgresUrl(string? conn)
{
    if (string.IsNullOrWhiteSpace(conn)) return conn!;
    if (!conn.StartsWith("postgres://") && !conn.StartsWith("postgresql://")) return conn;
    try
    {
        var uri = new Uri(conn);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var db = uri.AbsolutePath.TrimStart('/');
        var userInfo = uri.UserInfo.Split(':');
        var user = Uri.UnescapeDataString(userInfo[0]);
        var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
    }
    catch { return conn; }
}

// ===== Redis 缓存（可选） =====
// 检查环境变量 REDIS_URL，如果不存在则使用 InMemoryCacheService
var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
var useRedis = !string.IsNullOrWhiteSpace(redisUrl);
if (useRedis)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisUrl!));
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}
else
{
    builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
}

// ===== 业务服务 =====
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ===== JWT 认证 =====
// 优先从环境变量读取 JWT_KEY / JWT_ISSUER / JWT_AUDIENCE，回退到 appsettings.json
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? builder.Configuration["Jwt:Key"]!;
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["Jwt:Audience"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,
        };
    });

// ===== CORS =====
// 优先从环境变量 CORS_ORIGINS（逗号分隔）读取，回退到 appsettings.json
// 当环境变量 CORS_ALLOW_ALL=true 时使用 AllowAnyOrigin
var allowAllCors = string.Equals(
    Environment.GetEnvironmentVariable("CORS_ALLOW_ALL"), "true",
    StringComparison.OrdinalIgnoreCase);

if (allowAllCors)
{
    builder.Services.AddCors(opt =>
        opt.AddPolicy("NomadCN", p => p
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()));
}
else
{
    var corsOriginsEnv = Environment.GetEnvironmentVariable("CORS_ORIGINS");
    string[] allowedOrigins;
    if (!string.IsNullOrWhiteSpace(corsOriginsEnv))
    {
        allowedOrigins = corsOriginsEnv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
    else
    {
        allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:8765" };
    }

    builder.Services.AddCors(opt =>
        opt.AddPolicy("NomadCN", p => p
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));
}

// ===== MVC + Swagger =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NomadCN API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ===== 端口：监听环境变量 PORT，默认 5000 =====
// IIS inprocess 模式下端口由 IIS 管理，app.Urls 为只读集合，需捕获异常
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
try { app.Urls.Add($"http://*:{port}"); } catch { /* IIS inprocess 模式下端口由 IIS 管理 */ }

// ===== 自动建表 + 数据迁移 + 数据播种 =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DataSeeder.SeedAsync(db);
}

// ===== Swagger：生产环境也启用 =====
app.UseSwagger();
app.UseSwaggerUI();

// ===== 静态文件 + CORS + 认证 =====
app.UseStaticFiles();
app.UseCors("NomadCN");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ===== 前端路由 fallback =====
app.MapFallbackToFile("index.html");

app.Run();
