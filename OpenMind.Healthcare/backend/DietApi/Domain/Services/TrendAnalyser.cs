using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Services;

/// <summary>
/// Lays a member's logged days out across every calendar day of a period.
/// </summary>
/// <remarks>
/// The read model returns only days that were logged. A line chart needs every day, so that the
/// ones with nothing on them can be drawn as gaps rather than silently closed up — a chart that
/// omits unlogged days entirely compresses time and makes a fortnight of neglect look like a
/// continuous run.
/// </remarks>
public class TrendAnalyser
{
    public IntakeTrend Build(AnalysisPeriod period, IReadOnlyList<DayIntakeRow> days)
    {
        if (period.TotalDays <= 0)
            return IntakeTrend.Empty();

        var byDate = days.ToDictionary(d => d.Date);
        var points = new List<DailyIntakePoint>(period.TotalDays);

        // The target in force, carried across days the member did not log. Their target existed on
        // those days; not logging does not suspend it. Intake is never carried forward this way -
        // that would be inventing food.
        var target = days.Count == 0 ? 0 : days[0].TargetCalories;
        decimal? targetProtein = days.Count == 0 ? null : days[0].TargetProteinG;
        decimal? targetCarbs = days.Count == 0 ? null : days[0].TargetCarbsG;
        decimal? targetFat = days.Count == 0 ? null : days[0].TargetFatG;

        for (var date = period.From; date <= period.To; date = date.AddDays(1))
        {
            if (byDate.TryGetValue(date, out var row))
            {
                target = row.TargetCalories;
                targetProtein = row.TargetProteinG;
                targetCarbs = row.TargetCarbsG;
                targetFat = row.TargetFatG;

                points.Add(new DailyIntakePoint(
                    date, Logged: true, row.Calories, row.TargetCalories,
                    row.ProteinG, row.CarbsG, row.FatG,
                    row.TargetProteinG, row.TargetCarbsG, row.TargetFatG));
            }
            else
            {
                // Zeros here are placeholders behind a false Logged flag, never figures to plot.
                points.Add(new DailyIntakePoint(
                    date, Logged: false, 0, target, 0m, 0m, 0m,
                    targetProtein, targetCarbs, targetFat));
            }
        }

        return IntakeTrend.Create(points);
    }
}
