using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;

namespace PMIS.Application.Services;

public class NotificationPreferenceService(IUnitOfWork unitOfWork) : INotificationPreferenceService
{
    public async Task<IReadOnlyList<NotificationPreferenceDto>> GetForUserAsync(
        Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        var catalog = NotificationPreferenceKeys.ForRole(role);
        if (catalog.Count == 0)
        {
            return [];
        }

        var stored = await unitOfWork.NotificationPreferences
            .FindAsync(p => p.UserId == userId, cancellationToken);
        var storedByKey = stored.ToDictionary(p => p.PreferenceKey, p => p.IsEnabled);

        return [.. catalog.Select(key => new NotificationPreferenceDto
        {
            Key = key,
            IsEnabled = !storedByKey.TryGetValue(key, out var isEnabled) || isEnabled
        })];
    }

    public async Task<string?> UpdateAsync(
        Guid userId, UserRole role, IReadOnlyList<NotificationPreferenceDto> preferences,
        CancellationToken cancellationToken = default)
    {
        if (preferences.Count == 0)
        {
            return null;
        }

        var catalog = NotificationPreferenceKeys.ForRole(role);
        if (preferences.Any(p => !catalog.Contains(p.Key)))
        {
            return "إعداد إشعارات غير معروف";
        }

        if (preferences.Select(p => p.Key).Distinct().Count() != preferences.Count)
        {
            return "تكرار في إعدادات الإشعارات";
        }

        var stored = await unitOfWork.NotificationPreferences
            .FindAsync(p => p.UserId == userId, cancellationToken);
        var storedByKey = stored.ToDictionary(p => p.PreferenceKey);

        foreach (var preference in preferences)
        {
            if (storedByKey.TryGetValue(preference.Key, out var existing))
            {
                if (existing.IsEnabled == preference.IsEnabled)
                {
                    continue;
                }

                existing.IsEnabled = preference.IsEnabled;
            }
            else
            {
                await unitOfWork.NotificationPreferences.AddAsync(
                    new NotificationPreference
                    {
                        UserId = userId,
                        PreferenceKey = preference.Key,
                        IsEnabled = preference.IsEnabled
                    },
                    cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<bool> IsEnabledAsync(
        Guid userId, string preferenceKey, CancellationToken cancellationToken = default)
    {
        var stored = await unitOfWork.NotificationPreferences
            .FindAsync(p => p.UserId == userId && p.PreferenceKey == preferenceKey, cancellationToken);

        return stored.Count == 0 || stored[0].IsEnabled;
    }
}
