using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NomadCN.Api.Data;
using NomadCN.Api.Models;
using NomadCN.Api.Services;

namespace NomadCN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnnotationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthService _authService;

    public AnnotationsController(AppDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    /// <summary>获取指定城市的所有标注（所有用户共享可见，无需登录）</summary>
    [HttpGet("{cityName}")]
    public async Task<ApiResponse<List<AnnotationDto>>> GetByCity(string cityName)
    {
        var annotations = await _db.MapAnnotations
            .Where(a => a.CityName == cityName)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AnnotationDto
            {
                Id = a.Id,
                Type = a.Type,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                Content = a.Content,
                Color = a.Color,
                Path = a.Path,
                Username = a.Username,
                CreatedAt = a.CreatedAt,
            })
            .ToListAsync();

        return ApiResponse<List<AnnotationDto>>.Success(annotations);
    }

    /// <summary>创建标注（需登录）</summary>
    [Authorize]
    [HttpPost]
    public async Task<ApiResponse<AnnotationDto>> Create([FromBody] CreateAnnotationDto dto)
    {
        var user = _authService.GetUserFromClaims(User);
        if (user == null) return ApiResponse<AnnotationDto>.Fail("未登录");

        var annotation = new MapAnnotation
        {
            CityName = dto.CityName,
            Type = dto.Type,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Content = dto.Content ?? "",
            Color = dto.Color ?? "#00d9a3",
            Path = dto.Path ?? "[]",
            Username = user.Username,
            UserId = user.Id,
            CreatedAt = DateTime.Now,
        };

        _db.MapAnnotations.Add(annotation);
        await _db.SaveChangesAsync();

        return ApiResponse<AnnotationDto>.Success(new AnnotationDto
        {
            Id = annotation.Id,
            Type = annotation.Type,
            Latitude = annotation.Latitude,
            Longitude = annotation.Longitude,
            Content = annotation.Content,
            Color = annotation.Color,
            Path = annotation.Path,
            Username = annotation.Username,
            CreatedAt = annotation.CreatedAt,
        });
    }

    /// <summary>删除标注（仅创建者可删除）</summary>
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ApiResponse<object>> Delete(int id)
    {
        var user = _authService.GetUserFromClaims(User);
        if (user == null) return ApiResponse<object>.Fail("未登录");

        var annotation = await _db.MapAnnotations.FirstOrDefaultAsync(a => a.Id == id);
        if (annotation == null) return ApiResponse<object>.Fail("标注不存在");

        if (annotation.UserId != user.Id)
            return ApiResponse<object>.Fail("只能删除自己的标注");

        _db.MapAnnotations.Remove(annotation);
        await _db.SaveChangesAsync();

        return ApiResponse<object>.Success(new { });
    }

    /// <summary>清空指定城市自己的所有标注（需登录）</summary>
    [Authorize]
    [HttpDelete("clear/{cityName}")]
    public async Task<ApiResponse<object>> ClearMine(string cityName)
    {
        var user = _authService.GetUserFromClaims(User);
        if (user == null) return ApiResponse<object>.Fail("未登录");

        var mine = await _db.MapAnnotations
            .Where(a => a.CityName == cityName && a.UserId == user.Id)
            .ToListAsync();

        _db.MapAnnotations.RemoveRange(mine);
        await _db.SaveChangesAsync();

        return ApiResponse<object>.Success(new { removed = mine.Count });
    }
}

/// <summary>标注返回 DTO</summary>
public class AnnotationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Color { get; set; } = "#00d9a3";
    public string Path { get; set; } = "[]";
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>创建标注请求 DTO</summary>
public class CreateAnnotationDto
{
    public string CityName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Content { get; set; }
    public string? Color { get; set; }
    public string? Path { get; set; }
}
