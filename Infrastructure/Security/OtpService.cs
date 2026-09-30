using System.Security.Cryptography;
using System.Text;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Enums;
using PMIS.Domain.ValueObjects;
using StackExchange.Redis;

namespace PMIS.Infrastructure.Security;

public class OtpService(IConnectionMultiplexer redis, IEmailSender emailSender, ISmsSender smsSender, ISystemSettingsService systemSettingsService)
    : IOtpService
{
    private const int MaxVerifyAttempts = 5;

    private static string HashKey(string contextId, OtpChannel channel) => $"otp:{channel}:{contextId}";

    public async Task<OtpSendResult> SendAsync(
        string contextId, OtpChannel channel, string destination, CancellationToken cancellationToken = default)
    {
        var settings = await systemSettingsService.GetAsync(cancellationToken);
        return await IssueAndDeliverAsync(contextId, channel, destination, settings, resendCount: 0, cancellationToken);
    }

    public async Task<OtpSendResult> ResendAsync(
        string contextId, OtpChannel channel, string destination, CancellationToken cancellationToken = default)
    {
        var settings = await systemSettingsService.GetAsync(cancellationToken);
        var db = redis.GetDatabase();
        var key = HashKey(contextId, channel);

        var entry = await db.HashGetAllAsync(key);
        var resendCount = HashFieldInt(entry, "resends");
        var lastSentUnixSeconds = HashFieldLong(entry, "sentAt");

        if (lastSentUnixSeconds is { } lastSent)
        {
            var elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastSent;
            if (elapsed < settings.OtpResendCooldownSeconds)
            {
                return OtpSendResult.CooldownActive;
            }
        }

        if (resendCount >= settings.OtpMaxResendCount)
        {
            return OtpSendResult.ResendLimitReached;
        }

        return await IssueAndDeliverAsync(contextId, channel, destination, settings, resendCount + 1, cancellationToken);
    }

    public async Task<OtpVerifyResult> VerifyAsync(
        string contextId, OtpChannel channel, string code, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var key = HashKey(contextId, channel);

        var entry = await db.HashGetAllAsync(key);
        if (entry.Length == 0)
        {
            return OtpVerifyResult.NoPendingCode;
        }

        var storedHash = HashFieldString(entry, "hash");
        if (storedHash is null)
        {
            return OtpVerifyResult.NoPendingCode;
        }

        if (FixedTimeEquals(Hash(code), storedHash))
        {
            await db.KeyDeleteAsync(key);
            return OtpVerifyResult.Verified;
        }

        var attempts = await db.HashIncrementAsync(key, "attempts", 1);
        if (attempts >= MaxVerifyAttempts)
        {
            await db.KeyDeleteAsync(key);
        }

        return OtpVerifyResult.CodeMismatch;
    }

    private async Task<OtpSendResult> IssueAndDeliverAsync(
        string contextId, OtpChannel channel, string destination, SystemSettingsDto settings, int resendCount,
        CancellationToken cancellationToken)
    {
        var code = GenerateCode();
        var db = redis.GetDatabase();
        var key = HashKey(contextId, channel);

        var fields = new HashEntry[]
        {
            new("hash", Hash(code)),
            new("attempts", 0),
            new("resends", resendCount),
            new("sentAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };
        await db.HashSetAsync(key, fields);
        await db.KeyExpireAsync(key, TimeSpan.FromMinutes(settings.OtpExpiryMinutes));

        var delivered = channel == OtpChannel.Email
            ? await emailSender.SendAsync(destination, OtpMessages.EmailSubject, OtpMessageTemplate.Substitute(settings.OtpEmailMessageTemplate, OtpMessageTemplate.EmailToken, code), cancellationToken)
            : await smsSender.SendAsync(destination, OtpMessageTemplate.Substitute(settings.OtpSmsMessageTemplate, OtpMessageTemplate.SmsToken, code), cancellationToken);

        return delivered ? OtpSendResult.Sent : OtpSendResult.DeliveryFailed;
    }

    private static string GenerateCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private static string? HashFieldString(HashEntry[] entry, string name) =>
        entry.FirstOrDefault(e => e.Name == name) is { Value.IsNull: false } match ? (string?)match.Value : null;

    private static int HashFieldInt(HashEntry[] entry, string name) =>
        entry.FirstOrDefault(e => e.Name == name) is { Value.IsNull: false } match ? (int)match.Value : 0;

    private static long? HashFieldLong(HashEntry[] entry, string name) =>
        entry.FirstOrDefault(e => e.Name == name) is { Value.IsNull: false } match ? (long)match.Value : null;
}

internal static class OtpMessages
{
    public const string EmailSubject = "رمز التحقق - نظام إدارة تراخيص المشاريع";
}
