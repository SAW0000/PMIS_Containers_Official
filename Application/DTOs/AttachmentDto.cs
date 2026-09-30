namespace PMIS.Application.DTOs;

public class AttachmentDto
{
    public Guid Id { get; set; }
    public string AttachmentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
}
