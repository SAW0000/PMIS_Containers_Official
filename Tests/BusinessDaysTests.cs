using PMIS.Application.Services;

namespace PMIS.Tests;

public class BusinessDaysTests
{
    [Fact]
    public void Between_SameDay_IsZero()
        => Assert.Equal(0, BusinessDays.Between(new DateTime(2026, 9, 1, 8, 0, 0), new DateTime(2026, 9, 1, 23, 0, 0)));

    [Fact]
    public void Between_ReversedRange_IsZero()
        => Assert.Equal(0, BusinessDays.Between(new DateTime(2026, 9, 1), new DateTime(2026, 8, 20)));

    [Fact]
    public void Between_ConsecutiveWorkingDays_CountsOne()
        => Assert.Equal(1, BusinessDays.Between(new DateTime(2026, 8, 31), new DateTime(2026, 9, 1)));

    [Fact]
    public void Between_SkipsFridayAndSaturday()
    {
        Assert.Equal(1, BusinessDays.Between(new DateTime(2026, 8, 27), new DateTime(2026, 8, 30)));
    }

    [Fact]
    public void Between_FullWeek_CountsFiveWorkingDays()
        => Assert.Equal(5, BusinessDays.Between(new DateTime(2026, 8, 25), new DateTime(2026, 9, 1)));

    [Fact]
    public void CutoffFor_WalksBackOverTheWeekend()
    {
        Assert.Equal(new DateTime(2026, 8, 27), BusinessDays.CutoffFor(new DateTime(2026, 9, 1, 15, 30, 0), 3));
    }

    [Fact]
    public void CutoffFor_ZeroDays_IsToday()
        => Assert.Equal(new DateTime(2026, 9, 1), BusinessDays.CutoffFor(new DateTime(2026, 9, 1, 15, 30, 0), 0));

    [Fact]
    public void CutoffFor_AgreesWithBetween_AtTheBoundary()
    {
        var now = new DateTime(2026, 9, 1, 12, 0, 0);
        var cutoff = BusinessDays.CutoffFor(now, 3);

        Assert.Equal(3, BusinessDays.Between(cutoff, now));
        Assert.True(BusinessDays.Between(cutoff.AddDays(-1), now) > 3);
    }
}
