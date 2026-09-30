using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PMIS.Application.Interfaces;
using PMIS.Infrastructure.Persistence;
using PMIS.Infrastructure.Security;

namespace PMIS.Tests;

public class ClamAvScannerTests
{
    private const string EicarSignature =
        "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

    private static ClamAvScanner CreateScanner() =>
        new(Options.Create(new ClamAvSettings { Host = "127.0.0.1", Port = 3310 }), NullLogger<ClamAvScanner>.Instance);

    [Fact]
    public async Task ScanAsync_ReturnsClean_ForOrdinaryFile()
    {
        var scanner = CreateScanner();
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4\nordinary clean content\n%%EOF"));

        var result = await scanner.ScanAsync(stream);

        Assert.Equal(ScanVerdict.Clean, result.Verdict);
    }

    [Fact]
    public async Task ScanAsync_ReturnsInfected_ForPureEicarSignature()
    {
        var scanner = CreateScanner();
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(EicarSignature));

        var result = await scanner.ScanAsync(stream);

        Assert.Equal(ScanVerdict.Infected, result.Verdict);
        Assert.Contains("Eicar", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScanAsync_ReturnsError_WhenClamdUnreachable()
    {
        var scanner = new ClamAvScanner(
            Options.Create(new ClamAvSettings { Host = "127.0.0.1", Port = 39999 }),
            NullLogger<ClamAvScanner>.Instance);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("irrelevant content"));

        var result = await scanner.ScanAsync(stream);

        Assert.Equal(ScanVerdict.Error, result.Verdict);
    }
}
