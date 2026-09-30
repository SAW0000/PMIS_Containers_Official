using PMIS.Application.DTOs;

namespace PMIS.Application.Interfaces;

public interface IAccountSettingsService
{
    Task<(AccountContactVerificationResultDto? Result, string? Error)> SendContactVerificationAsync(
        Guid userId, AccountContactVerificationRequestDto dto, bool isResend, CancellationToken cancellationToken = default);

    Task<(AccountInfoUpdateResultDto? Result, string? Error)> UpdateAccountInfoAsync(
        Guid userId, AccountInfoUpdateDto dto, ScannedPhoto? photo, string ipAddress,
        CancellationToken cancellationToken = default);
}
