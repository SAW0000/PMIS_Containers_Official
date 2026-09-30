using System.Net;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Security;

namespace PMIS.Tests;

public class LmsEmailSenderTests
{
    private static (LmsEmailSender Sender, Mock<HttpMessageHandler> Handler) CreateSender(HttpStatusCode statusCode)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://lms.example.test/") };
        var settings = Options.Create(new EmailSmsSettings
        {
            LmsBaseUrl = "https://lms.example.test/",
            SendMailPath = "smsJson/MailServices.svc/SendMail",
            MailWsKey = "test-mail-key",
            MailUsername = "test-user",
            MailPassword = "test-pass",
            MailFrom = "noreply@example.test"
        });

        return (new LmsEmailSender(httpClient, settings), handlerMock);
    }

    [Fact]
    public async Task SendAsync_ReturnsTrue_OnSuccessResponse()
    {
        var (sender, _) = CreateSender(HttpStatusCode.OK);

        var result = await sender.SendAsync("user@example.test", "subject", "body");

        Assert.True(result);
    }

    [Fact]
    public async Task SendAsync_ReturnsFalse_OnNonSuccessStatusCode()
    {
        var (sender, _) = CreateSender(HttpStatusCode.InternalServerError);

        var result = await sender.SendAsync("user@example.test", "subject", "body");

        Assert.False(result);
    }

    [Fact]
    public async Task SendAsync_ReturnsFalse_OnUnauthorized()
    {
        var (sender, _) = CreateSender(HttpStatusCode.Unauthorized);

        var result = await sender.SendAsync("user@example.test", "subject", "body");

        Assert.False(result);
    }
}
