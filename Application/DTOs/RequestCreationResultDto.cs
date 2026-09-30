namespace PMIS.Application.DTOs;

public class RequestCreationResultDto
{
    public Guid RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
}
