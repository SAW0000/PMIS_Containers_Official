namespace PMIS.Application.DTOs;

public class RejectionReasonOptionDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string DefaultNote { get; set; } = string.Empty;
    public string? Hint { get; set; }
}

public class RejectionReasonOptionInput
{
    public string Label { get; set; } = string.Empty;
    public string DefaultNote { get; set; } = string.Empty;
}
