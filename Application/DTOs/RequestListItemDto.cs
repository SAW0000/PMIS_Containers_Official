namespace PMIS.Application.DTOs;

public class RequestListItemDto
{
    public string Id { get; set; } = string.Empty;

    public Guid RequestId { get; set; }
    public string Applicant { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public string TargetSystem { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public bool IsOwnRequest { get; set; }

    public List<RequestNoteDto> Notes { get; set; } = [];

    public List<List<string?>> History { get; set; } = [];
}

public class RequestNoteDto
{
    public string Author { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
