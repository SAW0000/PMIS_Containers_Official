using PMIS.Application.Interfaces;
using StackExchange.Redis;

namespace PMIS.Infrastructure.Security;

public class RedisLoginLockoutService(IConnectionMultiplexer redis, ISystemSettingsService systemSettingsService)
    : ILoginLockoutService
{
    private static string CounterKey(string email) => $"login-lockout:attempts:{email.Trim().ToLowerInvariant()}";

    public async Task<bool> IsLockedOutAsync(string email, CancellationToken cancellationToken = default)
    {
        var settings = await systemSettingsService.GetAsync(cancellationToken);
        var db = redis.GetDatabase();
        var attempts = (long)await db.StringGetAsync(CounterKey(email));
        return attempts >= settings.MaxLoginAttempts;
    }

    public async Task<bool> RegisterFailureAsync(string email, CancellationToken cancellationToken = default)
    {
        var settings = await systemSettingsService.GetAsync(cancellationToken);
        var db = redis.GetDatabase();
        var key = CounterKey(email);

        var attempts = await db.StringIncrementAsync(key);
        if (attempts == 1)
        {
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(settings.LoginLockoutMinutes));
        }

        return attempts == settings.MaxLoginAttempts;
    }

    public Task ResetAsync(string email, CancellationToken cancellationToken = default) =>
        redis.GetDatabase().KeyDeleteAsync(CounterKey(email));
}
