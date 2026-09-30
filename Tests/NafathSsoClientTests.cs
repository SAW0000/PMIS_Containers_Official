using System.Net;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Security;

namespace PMIS.Tests;

public class NafathSsoClientTests
{
    private static NafathSsoClient CreateClient(HttpStatusCode statusCode, string? jsonBody)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = jsonBody is null ? null! : new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://sso.example.test/") };
        var settings = Options.Create(new NafathSettings
        {
            IamSsoUrl = "https://sso.example.test/",
            GetTokenUrl = "Account/GeneraterToken?appkey=",
            UserClaimsUrl = "Account/GetUserClaim?token=",
            AppKey = "test-app-key",
            IamLoginUrl = "https://sso.example.test/Account/IamLogin"
        });

        return new NafathSsoClient(httpClient, settings);
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsToken_OnSuccessResponse()
    {
        var client = CreateClient(HttpStatusCode.OK, """{"Data":"abc123token"}""");

        var token = await client.GetTokenAsync();

        Assert.Equal("abc123token", token);
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsNull_OnNonSuccessStatusCode()
    {
        var client = CreateClient(HttpStatusCode.InternalServerError, null);

        var token = await client.GetTokenAsync();

        Assert.Null(token);
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsNull_WhenDataFieldMissing()
    {
        var client = CreateClient(HttpStatusCode.OK, "{}");

        var token = await client.GetTokenAsync();

        Assert.Null(token);
    }

    [Fact]
    public async Task GetUserClaimAsync_ReturnsClaim_OnSuccessResponse()
    {
        var client = CreateClient(HttpStatusCode.OK, """
            {"Data":{"NameIdentifier":"1234567890","ArabicName":"محمد أحمد علي","DobHijri":"1420-01-01","Gender":"M","Nationality":"سعودي"}}
            """);

        var claim = await client.GetUserClaimAsync("some-token");

        Assert.NotNull(claim);
        Assert.Equal("1234567890", claim!.NameIdentifier);
        Assert.Equal("محمد أحمد علي", claim.ArabicName);
    }

    [Fact]
    public async Task GetUserClaimAsync_ReturnsNull_OnNonSuccessStatusCode()
    {
        var client = CreateClient(HttpStatusCode.Unauthorized, null);

        var claim = await client.GetUserClaimAsync("some-token");

        Assert.Null(claim);
    }
}
