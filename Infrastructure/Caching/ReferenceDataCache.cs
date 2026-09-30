using System.Text.Json;
using PMIS.Application.Interfaces;
using StackExchange.Redis;

namespace PMIS.Infrastructure.Caching;

public class RedisReferenceDataCache(IConnectionMultiplexer redis) : IReferenceDataCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await redis.GetDatabase().StringGetAsync(CacheKey(key));
        return value.IsNull ? default : JsonSerializer.Deserialize<T>((string)value!, JsonOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl)
        => redis.GetDatabase().StringSetAsync(CacheKey(key), JsonSerializer.Serialize(value, JsonOptions), ttl);

    public Task InvalidateAsync(string key)
        => redis.GetDatabase().KeyDeleteAsync(CacheKey(key));

    private static string CacheKey(string key) => $"pmis:refdata:{key}";
}
