using System.ComponentModel.DataAnnotations;

namespace NomadCN.Api.Models;

/// <summary>登录请求</summary>
public class LoginDto
{
    [Required]
    public string LoginId { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
    public bool Remember { get; set; }
}

/// <summary>注册请求</summary>
public class RegisterDto
{
    [Required]
    [MinLength(2)]
    [MaxLength(20)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>认证响应</summary>
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserInfoDto User { get; set; } = new();
}

/// <summary>用户信息</summary>
public class UserInfoDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarLetter { get; set; } = "U";
    public string AvatarColor { get; set; } = "#00d9a5";
}

/// <summary>统一 API 响应</summary>
public class ApiResponse<T>
{
    public bool Ok { get; set; }
    public string Msg { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T data, string msg = "success") =>
        new() { Ok = true, Msg = msg, Data = data };

    public static ApiResponse<T> Fail(string msg) =>
        new() { Ok = false, Msg = msg };
}
