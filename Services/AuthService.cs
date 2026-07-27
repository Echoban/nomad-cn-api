using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NomadCN.Api.Data;
using NomadCN.Api.Models;

namespace NomadCN.Api.Services;

public interface IAuthService
{
    Task<(bool ok, string msg, AuthResponseDto? data)> RegisterAsync(RegisterDto dto);
    Task<(bool ok, string msg, AuthResponseDto? data)> LoginAsync(LoginDto dto);
    UserInfoDto? GetUserFromClaims(ClaimsPrincipal principal);
}

/// <summary>
/// 认证服务 - JWT + BCrypt 密码哈希
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    private static readonly string[] AvatarColors =
        { "#00d9a5", "#3b82f6", "#f59e0b", "#ef4444", "#8b5cf6", "#ec4899", "#06b6d4", "#84cc16" };

    public AuthService(AppDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<(bool ok, string msg, AuthResponseDto? data)> RegisterAsync(RegisterDto dto)
    {
        // 检查用户名是否已存在
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
            return (false, "该用户名已被注册", null);

        // 检查邮箱是否已存在
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLower()))
            return (false, "该邮箱已被注册", null);

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11),
            AvatarLetter = dto.Username[..1].ToUpper(),
            AvatarColor = AvatarColors[dto.Username[0] % AvatarColors.Length],
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        return (true, "注册成功", new AuthResponseDto
        {
            Token = token,
            User = ToUserInfo(user)
        });
    }

    public async Task<(bool ok, string msg, AuthResponseDto? data)> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username == dto.LoginId || u.Email == dto.LoginId.ToLower());

        if (user == null)
            return (false, "用户不存在", null);

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return (false, "密码错误", null);

        var token = GenerateJwtToken(user);
        return (true, "登录成功", new AuthResponseDto
        {
            Token = token,
            User = ToUserInfo(user)
        });
    }

    public UserInfoDto? GetUserFromClaims(ClaimsPrincipal principal)
    {
        var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return null;

        return new UserInfoDto
        {
            Id = userId,
            Username = principal.FindFirst(ClaimTypes.Name)?.Value ?? "",
            Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? "",
            AvatarLetter = principal.FindFirst("avatar_letter")?.Value ?? "U",
            AvatarColor = principal.FindFirst("avatar_color")?.Value ?? "#00d9a5",
        };
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("avatar_letter", user.AvatarLetter),
            new Claim("avatar_color", user.AvatarColor),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(int.Parse(_config["Jwt:ExpireMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserInfoDto ToUserInfo(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        AvatarLetter = user.AvatarLetter,
        AvatarColor = user.AvatarColor,
    };
}
