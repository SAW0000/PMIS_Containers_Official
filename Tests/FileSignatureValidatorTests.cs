using PMIS.Infrastructure.Storage;

namespace PMIS.Tests;

public class FileSignatureValidatorTests
{
    private static MemoryStream StreamOf(byte[] bytes) => new(bytes);

    [Fact]
    public async Task MatchesExtensionAsync_ReturnsTrue_ForRealPdfHeader()
    {
        using var stream = StreamOf("%PDF-1.4\n1 0 obj<<>>endobj"u8.ToArray());

        Assert.True(await FileSignatureValidator.MatchesExtensionAsync(stream, ".pdf"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_ReturnsTrue_ForRealPngHeader()
    {
        using var stream = StreamOf([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00]);

        Assert.True(await FileSignatureValidator.MatchesExtensionAsync(stream, ".png"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_ReturnsTrue_ForRealJpegHeader()
    {
        using var stream = StreamOf([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10]);

        Assert.True(await FileSignatureValidator.MatchesExtensionAsync(stream, ".jpg"));
        stream.Position = 0;
        Assert.True(await FileSignatureValidator.MatchesExtensionAsync(stream, ".jpeg"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_ReturnsTrue_ForRealWebPHeader()
    {
        using var stream = StreamOf([
            0x52, 0x49, 0x46, 0x46, 0x24, 0x8A, 0x01, 0x00,
            0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38]);

        Assert.True(await FileSignatureValidator.MatchesExtensionAsync(stream, ".webp"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_ReturnsFalse_ForRiffContainerThatIsNotWebP()
    {
        using var stream = StreamOf([
            0x52, 0x49, 0x46, 0x46, 0x24, 0x8A, 0x01, 0x00,
            0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74]);

        Assert.False(await FileSignatureValidator.MatchesExtensionAsync(stream, ".webp"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_ReturnsFalse_WhenWebPHeaderIsTruncated()
    {
        using var stream = StreamOf([0x52, 0x49, 0x46, 0x46, 0x24, 0x8A, 0x01, 0x00, 0x57, 0x45]);

        Assert.False(await FileSignatureValidator.MatchesExtensionAsync(stream, ".webp"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_ReturnsFalse_WhenExecutableIsRenamedToJpg()
    {
        using var stream = StreamOf([0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00]);

        Assert.False(await FileSignatureValidator.MatchesExtensionAsync(stream, ".jpg"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_ReturnsFalse_WhenPlainTextRenamedToPdf()
    {
        using var stream = StreamOf("this is just plain text, not a real PDF at all"u8.ToArray());

        Assert.False(await FileSignatureValidator.MatchesExtensionAsync(stream, ".pdf"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_ReturnsFalse_WhenExtensionIsUnknown()
    {
        using var stream = StreamOf("%PDF-1.4"u8.ToArray());

        Assert.False(await FileSignatureValidator.MatchesExtensionAsync(stream, ".exe"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_ReturnsFalse_WhenFileIsShorterThanSignature()
    {
        using var stream = StreamOf([0x89, 0x50]);

        Assert.False(await FileSignatureValidator.MatchesExtensionAsync(stream, ".png"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_RestoresStreamPosition_AfterCheck()
    {
        using var stream = StreamOf("%PDF-1.4\nrest of file"u8.ToArray());
        stream.Position = 3;

        await FileSignatureValidator.MatchesExtensionAsync(stream, ".pdf");

        Assert.Equal(3, stream.Position);
    }

    [Fact]
    public async Task MatchesExtensionAsync_IsCaseInsensitiveForExtension()
    {
        using var stream = StreamOf("%PDF-1.4"u8.ToArray());

        Assert.True(await FileSignatureValidator.MatchesExtensionAsync(stream, ".PDF"));
    }
}
