namespace PMIS.Application.DTOs;

public class TargetSystemDto
{
    public Guid Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
}

public class TargetSystemAdminDto
{
    public Guid Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class TargetSystemInput
{
    public string SystemName { get; set; } = string.Empty;
}

public class SetActiveInput
{
    public required bool IsActive { get; set; }
}
