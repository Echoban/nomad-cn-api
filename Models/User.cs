using System.ComponentModel.DataAnnotations;

namespace NomadCN.Api.Models;

/// <summary>
/// 用户表
/// </summary>
public class User
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(5)]
    public string AvatarLetter { get; set; } = "U";

    [MaxLength(20)]
    public string AvatarColor { get; set; } = "#00d9a5";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
