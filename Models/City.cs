using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NomadCN.Api.Models;

/// <summary>
/// 城市主数据表 - 合并了城市基础信息、坐标、房价、深度描述
/// </summary>
public class City
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Region { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Tier { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Flag { get; set; } = string.Empty;

    // 生活成本
    public int Cost { get; set; }
    public int Rent { get; set; }
    public int Food { get; set; }
    public int Transport { get; set; }

    // 网络
    public int Internet { get; set; }
    [MaxLength(50)]
    public string Broadband { get; set; } = string.Empty;
    [MaxLength(50)]
    public string Mobile { get; set; } = string.Empty;

    // 气候
    [MaxLength(20)]
    public string Climate { get; set; } = string.Empty;
    public int TempAvg { get; set; }
    public int AirQuality { get; set; }
    [MaxLength(10)]
    public string AqiLevel { get; set; } = string.Empty;

    // 评分维度
    public int Safety { get; set; }
    public int Healthcare { get; set; }
    public int Walkability { get; set; }
    public int Nightlife { get; set; }
    public int Coffee { get; set; }
    public int Coworking { get; set; }

    // 标签（JSON 数组）
    public string Tags { get; set; } = "[]";

    [MaxLength(20)]
    public string ClimateType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // 坐标
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    // 房价（元/㎡）
    public int HousingPrice { get; set; }

    // 深度描述
    public string DeepLiving { get; set; } = string.Empty;
    public string DeepCommunity { get; set; } = string.Empty;
    public string DeepTips { get; set; } = string.Empty;
    [MaxLength(200)]
    public string DeepBestSeason { get; set; } = string.Empty;

    // 城市缺点深度解读
    public string DeepCons { get; set; } = string.Empty;

    // 综合评分（运行时计算或预存）
    public int Score { get; set; }

    // 喜欢数
    public int Likes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
