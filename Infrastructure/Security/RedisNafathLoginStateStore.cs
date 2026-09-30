using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;
using StackExchange.Redis;

namespace PMIS.Infrastructure.Security;

public class RedisNafathLoginStateStore(IConnectionMultiplexer redis) : INafathLoginStateStore
{
    private static string StateKey(string token) => $"nafath-state:{token}";

    public Task SetAsync(string token, UserRole role, TimeSpan lifetime, CancellationToken cancellationToken = default) =>
        redis.GetDatabase().StringSetAsync(StateKey(token), (int)role, lifetime);

    public async Task<UserRole?> GetAndRemoveAsync(string token, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var key = StateKey(token);
        var value = await db.StringGetAsync(key);
        if (value.IsNull)
        {
            return null;
        }

        await db.KeyDeleteAsync(key);
        return (UserRole)(int)value;
    }
}
