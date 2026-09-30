namespace PMIS.Application.Interfaces;

public interface ISmsSender
{
    Task<bool> SendAsync(string mobileNumber, string message, CancellationToken cancellationToken = default);
}
