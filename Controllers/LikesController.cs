using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NomadCN.Api.Data;
using NomadCN.Api.Models;
using NomadCN.Api.Services;

namespace NomadCN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LikesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthService _authService;
    private readonly ICacheService _cache;

    public LikesController(AppDbContext db, IAuthService authService, ICacheService cache)
    {
        _db = db;
        _authService = authService;
        _cache = cache;
    }

    /// <summary>切换喜欢状态（需登录）</summary>
    [Authorize]
    [HttpPost("toggle/{cityName}")]
    public async Task<ApiResponse<LikeToggleResponse>> Toggle(string cityName)
    {
        var user = _authService.GetUserFromClaims(User);
        if (user == null) return ApiResponse<LikeToggleResponse>.Fail("未登录");

        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Name == cityName);
        if (city == null) return ApiResponse<LikeToggleResponse>.Fail("城市不存在");

        var existing = await _db.CityLikes
            .FirstOrDefaultAsync(l => l.UserId == user.Id && l.CityId == city.Id);

        bool liked;
        if (existing != null)
        {
            // 取消喜欢
            _db.CityLikes.Remove(existing);
            city.Likes = Math.Max(0, city.Likes - 1);
            liked = false;
        }
        else
        {
            // 添加喜欢
            _db.CityLikes.Add(new CityLike { UserId = user.Id, CityId = city.Id });
            city.Likes += 1;
            liked = true;
        }

        await _db.SaveChangesAsync();

        // 清除缓存
        await _cache.RemoveAsync("cities:all");
        await _cache.RemoveAsync($"city:{cityName}");

        return ApiResponse<LikeToggleResponse>.Success(new LikeToggleResponse
        {
            Liked = liked,
            Likes = city.Likes
        });
    }

    /// <summary>获取当前用户喜欢的城市名列表（需登录）</summary>
    [Authorize]
    [HttpGet("my")]
    public async Task<ApiResponse<List<string>>> MyLikes()
    {
        var user = _authService.GetUserFromClaims(User);
        if (user == null) return ApiResponse<List<string>>.Fail("未登录");

        var likedCityNames = await (
            from cl in _db.CityLikes
            join c in _db.Cities on cl.CityId equals c.Id
            where cl.UserId == user.Id
            select c.Name
        ).ToListAsync();

        return ApiResponse<List<string>>.Success(likedCityNames);
    }
}

/// <summary>喜欢切换响应</summary>
public class LikeToggleResponse
{
    public bool Liked { get; set; }
    public int Likes { get; set; }
}
