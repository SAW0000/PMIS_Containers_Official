namespace PMIS.Infrastructure.Reporting;

public class ReportGenerationLimiter
{
    private readonly SemaphoreSlim gate = new(Environment.ProcessorCount);

    public async Task<T> RunAsync<T>(Func<T> build, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(build, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }
}
