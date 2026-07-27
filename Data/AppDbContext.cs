using Microsoft.EntityFrameworkCore;
using NomadCN.Api.Models;

namespace NomadCN.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<City> Cities => Set<City>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CityLike> CityLikes => Set<CityLike>();

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
            e.Property(c => c.Description).HasColumnType("LONGTEXT");
            e.Property(c => c.DeepLiving).HasColumnType("LONGTEXT");
            e.Property(c => c.DeepCommunity).HasColumnType("LONGTEXT");
            e.Property(c => c.DeepTips).HasColumnType("LONGTEXT");
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
    }
}
