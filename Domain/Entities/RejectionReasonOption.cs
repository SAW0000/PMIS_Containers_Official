namespace PMIS.Domain.Entities;

public class RejectionReasonOption : Entity
{
    public string Label { get; set; } = string.Empty;

    public string DefaultNote { get; set; } = string.Empty;

    public string? Hint { get; set; }

    public int SortOrder { get; set; }

    public ICollection<RequestNote> RequestNotes { get; set; } = [];
}
