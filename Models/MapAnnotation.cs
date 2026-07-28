using System.ComponentModel.DataAnnotations;

namespace NomadCN.Api.Models;

/// <summary>
/// 地图标注表 - 用户在城市地图上创建的标注（标记、文字、涂鸦）
/// 所有用户共享可见
/// </summary>
public class MapAnnotation
{
    public int Id { get; set; }

    /// <summary>关联城市名</summary>
    [MaxLength(50)]
    public string CityName { get; set; } = string.Empty;

    /// <summary>标注类型：marker / text / freehand</summary>
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    /// <summary>纬度（marker/text 为单个点，freehand 为第一个点）</summary>
    public decimal Latitude { get; set; }

    /// <summary>经度</summary>
    public decimal Longitude { get; set; }

    /// <summary>标注内容（text 类型的文字内容，marker 为弹窗文字）</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>颜色（十六进制，如 #00d9a3）</summary>
    [MaxLength(20)]
    public string Color { get; set; } = "#00d9a3";

    /// <summary>涂鸦路径（JSON 数组，仅 freehand 类型有值）</summary>
    public string Path { get; set; } = "[]";

    /// <summary>创建者用户名</summary>
    [MaxLength(20)]
    public string Username { get; set; } = string.Empty;

    /// <summary>创建者用户 ID</summary>
    public int? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
