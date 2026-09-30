using PMIS.Domain.Enums;

namespace PMIS.Domain.Entities;

public class Attachment : Entity
{
    public Guid RequestId { get; set; }
    public Request Request { get; set; } = null!;

    public AttachmentType AttachmentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
}
