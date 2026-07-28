using Microsoft.EntityFrameworkCore;
using NomadCN.Api.Models;

namespace NomadCN.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<City> Cities => Set<City>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CityLike> CityLikes => Set<CityLike>();
    public DbSet<MapAnnotation> MapAnnotations => Set<MapAnnotation>();
    public DbSet<CityComment> CityComments => Set<CityComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 城市表
        modelBuilder.Entity<City>(e =>
        {
            e.ToTable("cities");
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Name).IsUnique();
            e.Property(c => c.Latitude).HasPrecision(9, 6);
            e.Property(c => c.Longitude).HasPrecision(9, 6);
            e.Property(c => c.Tags).HasDefaultValue("[]");
        });

        // 用户表
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });

        // 城市喜欢记录表
        modelBuilder.Entity<CityLike>(e =>
        {
            e.ToTable("city_likes");
            e.HasKey(l => l.Id);
            e.HasIndex(l => new { l.UserId, l.CityId }).IsUnique();
        });

        // 地图标注表
        modelBuilder.Entity<MapAnnotation>(e =>
        {
            e.ToTable("map_annotations");
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.CityName);
            e.Property(a => a.Latitude).HasPrecision(9, 6);
            e.Property(a => a.Longitude).HasPrecision(9, 6);
        });

        // 城市评论表
        modelBuilder.Entity<CityComment>(e =>
        {
            e.ToTable("city_comments");
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.CityName);
        });
    }
}
