using PMIS.Application.DTOs;
using PMIS.Domain.ValueObjects;

namespace PMIS.Tests;

public class OtpResultMessagesTests
{
    [Fact]
    public void ForSendFailure_ReturnsNull_WhenSent()
    {
        Assert.Null(OtpResultMessages.ForSendFailure(OtpSendResult.Sent));
    }

    [Fact]
    public void ForSendFailure_ReturnsInfrastructureProblem_WhenDeliveryFailed()
    {
        Assert.Equal(OtpResultMessages.InfrastructureProblem, OtpResultMessages.ForSendFailure(OtpSendResult.DeliveryFailed));
    }

    [Fact]
    public void ForSendFailure_ReturnsDistinctMessage_WhenCooldownActive()
    {
        var message = OtpResultMessages.ForSendFailure(OtpSendResult.CooldownActive);

        Assert.NotNull(message);
        Assert.NotEqual(OtpResultMessages.InfrastructureProblem, message);
        Assert.NotEqual(OtpResultMessages.WrongCode, message);
    }

    [Fact]
    public void ForSendFailure_ReturnsDistinctMessage_WhenResendLimitReached()
    {
        var message = OtpResultMessages.ForSendFailure(OtpSendResult.ResendLimitReached);

        Assert.NotNull(message);
        Assert.NotEqual(OtpResultMessages.InfrastructureProblem, message);
        Assert.NotEqual(OtpResultMessages.WrongCode, message);
    }

    [Fact]
    public void ForVerifyFailure_ReturnsNull_WhenVerified()
    {
        Assert.Null(OtpResultMessages.ForVerifyFailure(OtpVerifyResult.Verified));
    }

    [Fact]
    public void ForVerifyFailure_ReturnsWrongCode_OnCodeMismatch()
    {
        Assert.Equal(OtpResultMessages.WrongCode, OtpResultMessages.ForVerifyFailure(OtpVerifyResult.CodeMismatch));
    }

    [Fact]
    public void ForVerifyFailure_ReturnsInfrastructureProblem_WhenNoPendingCode()
    {
        Assert.Equal(OtpResultMessages.InfrastructureProblem, OtpResultMessages.ForVerifyFailure(OtpVerifyResult.NoPendingCode));
    }
}
