using System.Globalization;
using PMIS.Application.DTOs;

namespace PMIS.Application.Services;

public static class ReportPeriodFilter
{
    public static IReadOnlyList<RequestListItemDto> Apply(IReadOnlyList<RequestListItemDto> requests, ReportPeriod period)
    {
        if (period == ReportPeriod.All)
        {
            return requests;
        }

        var reference = ReferenceDate(requests);

        return period switch
        {
            ReportPeriod.Today => requests.Where(r => ParseDate(r.Date)?.Date == reference.Date).ToList(),
            ReportPeriod.Week => FilterByWeek(requests, reference),
            ReportPeriod.Month => requests.Where(r =>
            {
                var d = ParseDate(r.Date);
                return d?.Year == reference.Year && d?.Month == reference.Month;
            }).ToList(),
            ReportPeriod.Year => requests.Where(r => ParseDate(r.Date)?.Year == reference.Year).ToList(),
            _ => requests
        };
    }

    private static List<RequestListItemDto> FilterByWeek(IReadOnlyList<RequestListItemDto> requests, DateTime reference)
    {
        var start = StartOfWeek(reference);
        var end = start.AddDays(7);
        return requests.Where(r =>
        {
            var d = ParseDate(r.Date);
            return d is not null && d >= start && d < end;
        }).ToList();
    }

    private static DateTime StartOfWeek(DateTime d)
    {
        var dayOffset = ((int)d.DayOfWeek + 6) % 7; 
        return d.Date.AddDays(-dayOffset);
    }

    private static DateTime ReferenceDate(IReadOnlyList<RequestListItemDto> requests)
    {
        var dates = requests.Select(r => ParseDate(r.Date)).Where(d => d is not null).Select(d => d!.Value).ToList();
        return dates.Count > 0 ? dates.Max() : DateTime.UtcNow;
    }

    private static DateTime? ParseDate(string? value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
