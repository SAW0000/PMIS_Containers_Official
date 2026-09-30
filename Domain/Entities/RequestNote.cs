using PMIS.Domain.Enums;

namespace PMIS.Domain.Entities;

public class RequestNote : Entity
{
    public Guid RequestId { get; set; }
    public Request Request { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string NoteText { get; set; } = string.Empty;
    public RequestNoteType NoteType { get; set; }

    public Guid ReasonId { get; set; }
    public RejectionReasonOption Reason { get; set; } = null!;
}
