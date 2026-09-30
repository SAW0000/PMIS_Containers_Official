using PMIS.Application.Interfaces;
using StackExchange.Redis;

namespace PMIS.Infrastructure.Security;

public class RedisNafathPendingProfileStore(IConnectionMultiplexer redis) : INafathPendingProfileStore
{
    private static string StateKey(string token) => $"nafath-pending-profile:{token}";

    public Task SetAsync(string token, Guid userId, TimeSpan lifetime, CancellationToken cancellationToken = default) =>
        redis.GetDatabase().StringSetAsync(StateKey(token), userId.ToString(), lifetime);

    public async Task<Guid?> GetAndRemoveAsync(string token, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var key = StateKey(token);
        var value = await db.StringGetAsync(key);
        if (value.IsNull)
        {
            return null;
        }

        await db.KeyDeleteAsync(key);
        return Guid.Parse(value.ToString());
    }
}
