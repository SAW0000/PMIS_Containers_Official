namespace PMIS.Infrastructure.Storage;

public static class FileSignatureValidator
{
    private const int SignatureReadLength = 12;

    public static async Task<bool> MatchesExtensionAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        if (!content.CanSeek)
        {
            throw new ArgumentException("Signature validation requires a seekable stream.", nameof(content));
        }

        var startPosition = content.Position;
        try
        {
            var header = new byte[SignatureReadLength];
            var totalRead = 0;
            while (totalRead < header.Length)
            {
                var read = await content.ReadAsync(header.AsMemory(totalRead), cancellationToken);
                if (read == 0) break;
                totalRead += read;
            }

            return extension.ToLowerInvariant() switch
            {
                ".pdf" => StartsWith(header, totalRead, "%PDF-"u8),
                ".png" => StartsWith(header, totalRead, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
                ".jpg" or ".jpeg" => StartsWith(header, totalRead, [0xFF, 0xD8, 0xFF]),
                ".webp" => IsWebP(header, totalRead),
                _ => false
            };
        }
        finally
        {
            content.Position = startPosition;
        }
    }

    private static bool IsWebP(byte[] header, int headerLength)
        => headerLength >= 12
            && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);

    private static bool StartsWith(byte[] header, int headerLength, ReadOnlySpan<byte> signature)
        => headerLength >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature);
}
