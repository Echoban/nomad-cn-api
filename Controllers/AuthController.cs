using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NomadCN.Api.Models;
using NomadCN.Api.Services;

namespace NomadCN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>注册</summary>
    [HttpPost("register")]
    public async Task<ApiResponse<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponse<AuthResponseDto>.Fail("输入数据不合法");

        var (ok, msg, data) = await _authService.RegisterAsync(dto);
        if (!ok) return ApiResponse<AuthResponseDto>.Fail(msg);
        return ApiResponse<AuthResponseDto>.Success(data!, msg);
    }

    /// <summary>登录</summary>
    [HttpPost("login")]
    public async Task<ApiResponse<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponse<AuthResponseDto>.Fail("输入数据不合法");

        var (ok, msg, data) = await _authService.LoginAsync(dto);
        if (!ok) return ApiResponse<AuthResponseDto>.Fail(msg);
        return ApiResponse<AuthResponseDto>.Success(data!, msg);
    }

    /// <summary>获取当前用户（需 JWT）</summary>
    [Authorize]
    [HttpGet("me")]
    public ApiResponse<UserInfoDto> Me()
    {
        var user = _authService.GetUserFromClaims(User);
        if (user == null) return ApiResponse<UserInfoDto>.Fail("未登录");
        return ApiResponse<UserInfoDto>.Success(user);
    }
}
