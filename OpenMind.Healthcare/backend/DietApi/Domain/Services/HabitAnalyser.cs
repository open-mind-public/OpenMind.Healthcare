using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Services;

/// <summary>
/// Turns a member's beer days, exercise days and logged days over a period into the figures the
/// analytics view shows: how often each habit happens, and how eating on beer days compares with
/// every other day.
/// </summary>
/// <remarks>
/// <para>
/// Pure - no clock, no repository. <c>today</c> and <c>planStart</c> arrive as parameters, so the
/// whole comparison is testable across arbitrary dates (Principle IV).
/// </para>
/// <para>
/// The not-logged days are derived here, not queried: an in-plan day in the period with no logged
/// row is a not-logged day, and it belongs in the comparison because a beer night the member did
/// not log is still part of the pattern.
/// </para>
/// <para>
/// Beer and exercise dates are intersected with the in-plan window before counting, so a stray date
/// from an older plan cannot inflate a figure - the same clamp the exercise range applies.
/// </para>
/// </remarks>
public class HabitAnalyser
{
    public HabitAnalysis Analyse(
        AnalysisPeriod period,
        DateOnly planStart,
        DateOnly today,
        IReadOnlyList<DayIntakeRow> loggedDays,
        IReadOnlySet<DateOnly> beerDates,
        IReadOnlySet<DateOnly> exerciseDates)
    {
        var inPlan = new List<DateOnly>();
        for (var date = period.From; date <= period.To; date = date.AddDays(1))
        {
            if (date >= planStart && date <= today)
                inPlan.Add(date);
        }

        if (inPlan.Count == 0)
            return HabitAnalysis.Empty;

        var inPlanSet = inPlan.ToHashSet();

        var stateByDate = loggedDays
            .Where(row => inPlanSet.Contains(row.Date))
            .ToDictionary(
                row => row.Date,
                row => DayAssessment.For(row.Date, row.Calories, row.TargetCalories, hasEntries: true).State);

        var beerInPlan = beerDates.Where(inPlanSet.Contains).ToHashSet();
        var exerciseInPlan = exerciseDates.Count(inPlanSet.Contains);

        var onBeer = Split(inPlan.Where(beerInPlan.Contains), stateByDate);
        var onOther = Split(inPlan.Where(d => !beerInPlan.Contains(d)), stateByDate);

        return new HabitAnalysis(
            InPlanDays: inPlan.Count,
            BeerDays: beerInPlan.Count,
            BeerDaysPerWeek: PerWeek(beerInPlan.Count, inPlan.Count),
            ExerciseDays: exerciseInPlan,
            ExerciseDaysPerWeek: PerWeek(exerciseInPlan, inPlan.Count),
            OnBeerDays: onBeer,
            OnNonBeerDays: onOther);
    }

    private static EatingOutcome Split(IEnumerable<DateOnly> dates, IReadOnlyDictionary<DateOnly, DayState> stateByDate)
    {
        int onTarget = 0, overTarget = 0, notLogged = 0;

        foreach (var date in dates)
        {
            var state = stateByDate.GetValueOrDefault(date, DayState.NotLogged);
            switch (state)
            {
                case DayState.OnTarget: onTarget++; break;
                case DayState.OverTarget: overTarget++; break;
                default: notLogged++; break;
            }
        }

        return EatingOutcome.From(onTarget, overTarget, notLogged);
    }

    private static decimal PerWeek(int count, int inPlanDays) =>
        inPlanDays == 0 ? 0m : Math.Round(count / (inPlanDays / 7m), 1);
}
