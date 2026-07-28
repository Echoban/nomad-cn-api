using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NomadCN.Api.Data;
using NomadCN.Api.Models;
using NomadCN.Api.Services;

namespace NomadCN.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthService _authService;

    public CommentsController(AppDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    /// <summary>获取指定城市的评论列表（无需登录）</summary>
    [HttpGet("{cityName}")]
    public async Task<ApiResponse<List<CommentDto>>> GetByCity(string cityName, [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        if (page < 1) page = 1;
        if (size < 1 || size > 50) size = 20;

        var query = _db.CityComments
            .Where(c => c.CityName == cityName)
            .OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync();
        var comments = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                CityName = c.CityName,
                Content = c.Content,
                Rating = c.Rating,
                Username = c.Username,
                AvatarLetter = c.AvatarLetter,
                AvatarColor = c.AvatarColor,
                IsBot = c.IsBot,
                CreatedAt = c.CreatedAt,
            })
            .ToListAsync();

        return ApiResponse<List<CommentDto>>.Success(comments, $"共 {total} 条评论");
    }

    /// <summary>获取评论摘要（总数 + 平均评分）</summary>
    [HttpGet("{cityName}/summary")]
    public async Task<ApiResponse<CommentSummaryDto>> GetSummary(string cityName)
    {
        var query = _db.CityComments.Where(c => c.CityName == cityName);
        var total = await query.CountAsync();
        var avgRating = total > 0 ? await query.AverageAsync(c => c.Rating) : 0;

        var distribution = await query
            .GroupBy(c => c.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Rating, g => g.Count);

        var summary = new CommentSummaryDto
        {
            Total = total,
            AvgRating = Math.Round(avgRating, 1),
            Distribution = distribution,
        };

        return ApiResponse<CommentSummaryDto>.Success(summary);
    }

    /// <summary>发表评论（需登录）</summary>
    [Authorize]
    [HttpPost]
    public async Task<ApiResponse<CommentDto>> Create([FromBody] CreateCommentDto dto)
    {
        var user = _authService.GetUserFromClaims(User);
        if (user == null) return ApiResponse<CommentDto>.Fail("未登录");

        if (string.IsNullOrWhiteSpace(dto.Content))
            return ApiResponse<CommentDto>.Fail("评论内容不能为空");

        if (dto.Content.Length > 500)
            return ApiResponse<CommentDto>.Fail("评论内容不能超过500字");

        if (dto.Rating < 1 || dto.Rating > 5)
            return ApiResponse<CommentDto>.Fail("评分范围为1-5");

        var comment = new CityComment
        {
            CityName = dto.CityName,
            Content = dto.Content.Trim(),
            Rating = dto.Rating,
            Username = user.Username,
            AvatarLetter = user.AvatarLetter,
            AvatarColor = user.AvatarColor,
            UserId = user.Id,
            IsBot = false,
            CreatedAt = DateTime.Now,
        };

        _db.CityComments.Add(comment);
        await _db.SaveChangesAsync();

        return ApiResponse<CommentDto>.Success(new CommentDto
        {
            Id = comment.Id,
            CityName = comment.CityName,
            Content = comment.Content,
            Rating = comment.Rating,
            Username = comment.Username,
            AvatarLetter = comment.AvatarLetter,
            AvatarColor = comment.AvatarColor,
            IsBot = comment.IsBot,
            CreatedAt = comment.CreatedAt,
        });
    }

    /// <summary>删除评论（仅本人可删）</summary>
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ApiResponse<object>> Delete(int id)
    {
        var user = _authService.GetUserFromClaims(User);
        if (user == null) return ApiResponse<object>.Fail("未登录");

        var comment = await _db.CityComments.FirstOrDefaultAsync(c => c.Id == id);
        if (comment == null) return ApiResponse<object>.Fail("评论不存在");

        if (comment.UserId != user.Id)
            return ApiResponse<object>.Fail("只能删除自己的评论");

        _db.CityComments.Remove(comment);
        await _db.SaveChangesAsync();

        return ApiResponse<object>.Success(new { });
    }
}

/// <summary>评论返回 DTO</summary>
public class CommentDto
{
    public int Id { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Username { get; set; } = string.Empty;
    public string AvatarLetter { get; set; } = "U";
    public string AvatarColor { get; set; } = "#00d9a5";
    public bool IsBot { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>创建评论请求 DTO</summary>
public class CreateCommentDto
{
    public string CityName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
}

/// <summary>评论摘要 DTO</summary>
public class CommentSummaryDto
{
    public int Total { get; set; }
    public double AvgRating { get; set; }
    public Dictionary<int, int> Distribution { get; set; } = new();
}
