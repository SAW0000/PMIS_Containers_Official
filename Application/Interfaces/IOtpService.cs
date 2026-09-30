using PMIS.Application.DTOs;
using PMIS.Domain.Enums;

namespace PMIS.Application.Interfaces;

public interface IOtpService
{
    Task<OtpSendResult> SendAsync(
        string contextId, OtpChannel channel, string destination, CancellationToken cancellationToken = default);

    Task<OtpSendResult> ResendAsync(
        string contextId, OtpChannel channel, string destination, CancellationToken cancellationToken = default);

    Task<OtpVerifyResult> VerifyAsync(
        string contextId, OtpChannel channel, string code, CancellationToken cancellationToken = default);
}
