using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Infrastructure.Security;

public class LmsEmailSender(HttpClient httpClient, IOptions<EmailSmsSettings> options) : IEmailSender
{
    private readonly EmailSmsSettings _settings = options.Value;

    public async Task<bool> SendAsync(
        string toAddress, string subject, string body, CancellationToken cancellationToken = default)
    {
        var request = new SendMailRequest
        {
            MailTo = [toAddress],
            MailSubject = subject,
            MailBody = body,
            UName = _settings.MailUsername,
            UPass = _settings.MailPassword,
            MailFrom = _settings.MailFrom,
            DisplaySybject = subject,
            WSKey = _settings.MailWsKey
        };

        var response = await httpClient.PostAsJsonAsync(_settings.SendMailPath, request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
