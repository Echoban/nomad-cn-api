using System.ComponentModel.DataAnnotations;

namespace NomadCN.Api.Models;

/// <summary>
/// 城市评论表 - 用户对城市的评价和讨论
/// </summary>
public class CityComment
{
    public int Id { get; set; }

    /// <summary>关联城市名</summary>
    [MaxLength(50)]
    public string CityName { get; set; } = string.Empty;

    /// <summary>评论内容</summary>
    [MaxLength(500)]
    public string Content { get; set; } = string.Empty;

    /// <summary>评分 1-5</summary>
    public int Rating { get; set; } = 5;

    /// <summary>评论者用户名</summary>
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>评论者头像首字母</summary>
    [MaxLength(5)]
    public string AvatarLetter { get; set; } = "U";

    /// <summary>评论者头像颜色</summary>
    [MaxLength(20)]
    public string AvatarColor { get; set; } = "#00d9a5";

    /// <summary>评论者用户 ID（机器人评论为 NULL）</summary>
    public int? UserId { get; set; }

    /// <summary>是否为机器人评论</summary>
    public bool IsBot { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
