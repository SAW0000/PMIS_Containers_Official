using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PMIS.Application.Interfaces;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Infrastructure.Security;

public class ClamAvScanner(IOptions<ClamAvSettings> options, ILogger<ClamAvScanner> logger) : IVirusScanner
{
    private static readonly TimeSpan ScanTimeout = TimeSpan.FromSeconds(30);
    private const int ChunkSize = 8192;

    private readonly ClamAvSettings _settings = options.Value;

    public async Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ScanTimeout);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, cts.Token);
            using var stream = client.GetStream();

            await stream.WriteAsync(Encoding.ASCII.GetBytes("zINSTREAM\0"), cts.Token);

            if (content.CanSeek)
            {
                content.Position = 0;
            }

            var buffer = new byte[ChunkSize];
            int bytesRead;
            while ((bytesRead = await content.ReadAsync(buffer, cts.Token)) > 0)
            {
                await WriteChunkAsync(stream, buffer.AsMemory(0, bytesRead), cts.Token);
            }

            await WriteChunkAsync(stream, ReadOnlyMemory<byte>.Empty, cts.Token);

            if (content.CanSeek)
            {
                content.Position = 0;
            }

            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            var reply = await reader.ReadLineAsync(cts.Token) ?? string.Empty;

            return ParseReply(reply);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "ClamAV scan timed out after {Timeout}", ScanTimeout);
            return new ScanResult(ScanVerdict.Error, "انتهت مهلة فحص الملف");
        }
        catch (Exception ex) when (ex is SocketException or IOException)
        {
            logger.LogError(ex, "ClamAV scan failed to connect/communicate with {Host}:{Port}", _settings.Host, _settings.Port);
            return new ScanResult(ScanVerdict.Error, "تعذر الاتصال بخدمة فحص الفيروسات");
        }
    }

    private static async Task WriteChunkAsync(NetworkStream stream, ReadOnlyMemory<byte> chunk, CancellationToken cancellationToken)
    {
        var lengthPrefix = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, chunk.Length);
        await stream.WriteAsync(lengthPrefix, cancellationToken);
        if (!chunk.IsEmpty)
        {
            await stream.WriteAsync(chunk, cancellationToken);
        }
    }

    private static ScanResult ParseReply(string reply)
    {
        var trimmed = reply.TrimEnd('\0', ' ', '\r', '\n');

        if (trimmed.EndsWith("OK", StringComparison.Ordinal))
        {
            return new ScanResult(ScanVerdict.Clean);
        }

        if (trimmed.Contains("FOUND", StringComparison.Ordinal))
        {
            return new ScanResult(ScanVerdict.Infected, trimmed);
        }

        return new ScanResult(ScanVerdict.Error, trimmed);
    }
}
