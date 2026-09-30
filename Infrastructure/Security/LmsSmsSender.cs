using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Infrastructure.Security;

public class LmsSmsSender(HttpClient httpClient, IOptions<EmailSmsSettings> options) : ISmsSender
{
    private readonly EmailSmsSettings _settings = options.Value;

    public async Task<bool> SendAsync(string mobileNumber, string message, CancellationToken cancellationToken = default)
    {
        var request = new SendSmsRequest
        {
            MobileNumbers = [mobileNumber],
            sms_msg = message,
            AppName = _settings.AppName,
            WsKey = _settings.SmsWsKey
        };

        var response = await httpClient.PostAsJsonAsync(_settings.SendSmsPath, request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
