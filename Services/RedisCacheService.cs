using System.Text.Json;
using StackExchange.Redis;

namespace NomadCN.Api.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
}

/// <summary>
/// Redis 缓存服务 - 使用 StackExchange.Redis
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    private readonly IConnectionMultiplexer _mux;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public RedisCacheService(IConnectionMultiplexer mux, ILogger<RedisCacheService> logger)
    {
        _mux = mux;
        _db = mux.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (!value.HasValue) return default;
            return JsonSerializer.Deserialize<T>(value!, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis GET 失败: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOpts);
            await _db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(30));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SET 失败: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try { await _db.KeyDeleteAsync(key); }
        catch (Exception ex) { _logger.LogError(ex, "Redis DEL 失败: {Key}", key); }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var server = _mux.GetServer(_mux.GetEndPoints().First());
            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                await _db.KeyDeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis 按模式删除失败: {Pattern}", pattern);
        }
    }
}
