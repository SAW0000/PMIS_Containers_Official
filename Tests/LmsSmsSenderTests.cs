using System.Net;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Security;

namespace PMIS.Tests;

public class LmsSmsSenderTests
{
    private static LmsSmsSender CreateSender(HttpStatusCode statusCode)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://lms.example.test/") };
        var settings = Options.Create(new EmailSmsSettings
        {
            LmsBaseUrl = "https://lms.example.test/",
            SendSmsPath = "smsJson/SmsServices.svc/SendSMSRest",
            SmsWsKey = "test-sms-key",
            AppName = "PMIS"
        });

        return new LmsSmsSender(httpClient, settings);
    }

    [Fact]
    public async Task SendAsync_ReturnsTrue_OnSuccessResponse()
    {
        var sender = CreateSender(HttpStatusCode.OK);

        var result = await sender.SendAsync("0500000000", "message");

        Assert.True(result);
    }

    [Fact]
    public async Task SendAsync_ReturnsFalse_OnNonSuccessStatusCode()
    {
        var sender = CreateSender(HttpStatusCode.BadGateway);

        var result = await sender.SendAsync("0500000000", "message");

        Assert.False(result);
    }
}
