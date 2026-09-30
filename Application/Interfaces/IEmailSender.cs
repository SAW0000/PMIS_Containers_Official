namespace PMIS.Application.Interfaces;

public interface IEmailSender
{
    Task<bool> SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default);
}
