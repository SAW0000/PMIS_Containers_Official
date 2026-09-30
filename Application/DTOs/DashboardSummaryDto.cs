namespace PMIS.Application.DTOs;

public class DashboardSummaryDto
{
    public DashboardStatsDto Stats { get; set; } = new();
    public PagedListDto<ActionItemDto> ActionItems { get; set; } = new();
    public PagedListDto<LicenseItemDto> Licenses { get; set; } = new();
    public PagedListDto<ActivityEventDto> Activity { get; set; } = new();
    public StatusBarsDto StatusBars { get; set; } = new();
    public DonutsDto Donuts { get; set; } = new();
}

public class DashboardStatsDto
{
    public int Total { get; set; }
    public int Submitted { get; set; }
    public int Activated { get; set; }

    public int NeedsAction { get; set; }
}

public class ActionItemDto
{
    public string Id { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public string Project { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public string? LatestNote { get; set; }
    public bool IsDraft { get; set; }
}

public class LicenseItemDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime ActivationDate { get; set; }
    public DateTime ExpiryDate { get; set; }

    public int DaysLeft { get; set; }
}

public class ActivityEventDto
{
    public string Label { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string RequestId { get; set; } = string.Empty;
}

public class StatusBarsDto
{
    public IReadOnlyList<StatusCountDto> Counts { get; set; } = [];
}

public class StatusCountDto
{
    public string StatusKey { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DonutsDto
{
    public int ActivatedPct { get; set; }
    public int AcceptedPct { get; set; }
    public int ReturnedPct { get; set; }
}
