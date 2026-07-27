using System.Collections.Concurrent;
using System.Text.Json;

namespace NomadCN.Api.Services;

/// <summary>
/// 内存缓存服务 - 使用 ConcurrentDictionary 存储，支持过期时间
/// 当未配置 Redis 时作为 ICacheService 的默认实现
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ILogger<InMemoryCacheService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public InMemoryCacheService(ILogger<InMemoryCacheService> logger)
    {
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.Expiry == null || entry.Expiry > DateTimeOffset.UtcNow)
                {
                    var value = JsonSerializer.Deserialize<T>(entry.Value!, JsonOpts);
                    return Task.FromResult(value);
                }
                // 已过期，移除
                _cache.TryRemove(key, out _);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InMemory GET 失败: {Key}", key);
        }
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOpts);
            var expiryOffset = expiry.HasValue
                ? DateTimeOffset.UtcNow.Add(expiry.Value)
                : DateTimeOffset.UtcNow.AddMinutes(30);
            _cache[key] = new CacheEntry(json, expiryOffset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InMemory SET 失败: {Key}", key);
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            // 将通配符模式转换为正则表达式
            // Redis Keys 模式支持 * 与 ?，这里做简单处理
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            var regex = new System.Text.RegularExpressions.Regex(regexPattern,
                System.Text.RegularExpressions.RegexOptions.Compiled);

            var keysToRemove = _cache.Keys.Where(k => regex.IsMatch(k)).ToList();
            foreach (var k in keysToRemove)
            {
                _cache.TryRemove(k, out _);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InMemory 按模式删除失败: {Pattern}", pattern);
        }
        return Task.CompletedTask;
    }

    private sealed class CacheEntry
    {
        public string Value { get; }
        public DateTimeOffset? Expiry { get; }

        public CacheEntry(string value, DateTimeOffset? expiry)
        {
            Value = value;
            Expiry = expiry;
        }
    }
}
