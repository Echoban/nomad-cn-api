namespace NomadCN.Api.Models;

/// <summary>
/// 城市喜欢记录表 - 用户对城市的喜欢关系
/// </summary>
public class CityLike
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
