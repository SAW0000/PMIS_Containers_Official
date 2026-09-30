namespace PMIS.Application.Services;

public static class BusinessDays
{
    private static bool IsWeekend(DateTime date)
        => date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;

    public static int Between(DateTime from, DateTime to)
    {
        var start = from.Date;
        var end = to.Date;
        if (end <= start)
        {
            return 0;
        }

        var days = 0;
        for (var day = start.AddDays(1); day <= end; day = day.AddDays(1))
        {
            if (!IsWeekend(day))
            {
                days++;
            }
        }

        return days;
    }

    public static DateTime CutoffFor(DateTime asOf, int businessDays)
    {
        var cutoff = asOf.Date;
        var remaining = businessDays;

        while (remaining > 0)
        {
            cutoff = cutoff.AddDays(-1);
            if (!IsWeekend(cutoff))
            {
                remaining--;
            }
        }

        return cutoff;
    }
}
