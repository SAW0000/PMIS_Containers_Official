namespace PMIS.Application.Interfaces;

public enum ScanVerdict { Clean, Infected, Error }

public record ScanResult(ScanVerdict Verdict, string? Detail = null);

public interface IVirusScanner
{
    Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default);
}
