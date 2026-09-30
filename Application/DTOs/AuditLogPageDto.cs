namespace PMIS.Application.DTOs;

public class AuditLogPageDto
{
    public IReadOnlyList<List<string>> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
