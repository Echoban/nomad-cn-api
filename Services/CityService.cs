using Microsoft.EntityFrameworkCore;
using NomadCN.Api.Data;
using NomadCN.Api.Models;

namespace NomadCN.Api.Services;

public interface ICityService
{
    Task<List<City>> GetAllAsync();
    Task<City?> GetByNameAsync(string name);
    Task<List<City>> SearchAsync(string keyword);
}

/// <summary>
/// 城市服务 - 带 Redis 缓存
/// 缓存策略：全量列表缓存 1 小时，单城市缓存 30 分钟
/// </summary>
public class CityService : ICityService
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogger<CityService> _logger;

    private const string CacheKeyAll = "cities:all";
    private const string CacheKeyPrefix = "city:";
    private static readonly TimeSpan CacheTtlAll = TimeSpan.FromHours(1);
    private static readonly TimeSpan CacheTtlOne = TimeSpan.FromMinutes(30);

    public CityService(AppDbContext db, ICacheService cache, ILogger<CityService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<City>> GetAllAsync()
    {
        // 1. 先查 Redis
        var cached = await _cache.GetAsync<List<City>>(CacheKeyAll);
        if (cached != null && cached.Count > 0)
        {
            _logger.LogInformation("城市列表命中缓存");
            return cached;
        }

        // 2. 未命中，查 MySQL
        var cities = await _db.Cities.OrderBy(c => c.Id).ToListAsync();
        if (cities.Count > 0)
        {
            await _cache.SetAsync(CacheKeyAll, cities, CacheTtlAll);
        }
        return cities;
    }

    public async Task<City?> GetByNameAsync(string name)
    {
        var key = $"{CacheKeyPrefix}{name}";
        var cached = await _cache.GetAsync<City>(key);
        if (cached != null) return cached;

        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Name == name);
        if (city != null)
        {
            await _cache.SetAsync(key, city, CacheTtlOne);
        }
        return city;
    }

    public async Task<List<City>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetAllAsync();

        var cities = await _db.Cities
            .Where(c => c.Name.Contains(keyword) || c.Region.Contains(keyword) || c.Tags.Contains(keyword))
            .OrderBy(c => c.Id)
            .ToListAsync();
        return cities;
    }
}
