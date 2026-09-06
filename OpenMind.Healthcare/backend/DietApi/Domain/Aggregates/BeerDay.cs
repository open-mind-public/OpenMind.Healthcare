using DDD.BuildingBlocks;
using DietApi.Domain.Events;
using DietApi.Domain.Rules;

namespace DietApi.Domain.Aggregates;

/// <summary>
/// One calendar date a member has marked as a day they drank beer.
/// </summary>
/// <remarks>
/// <para>
/// Its own aggregate root, and deliberately <em>not</em> a flag on <c>LoggedDay</c> - the same
/// reasoning that made <see cref="ExerciseDay"/> a sibling of a logged day rather than a part of it.
/// A logged day is created by the first food entry and destroyed when the last is removed, so a
/// beer marker living there would vanish when a member cleared that day's food, and a beer day with
/// no food logged would have nowhere to live. FR-004 also says a beer day carries no calories and
/// does not move the eating verdict - keeping it out of <c>LoggedDay</c> makes that structural
/// (research.md R-001).
/// </para>
/// <para>
/// It carries nothing but the date. There is no total to keep and no child to accumulate, so -
/// unlike the other per-day aggregates - it needs no concurrency token: it either exists or it does
/// not, and both marking and unmarking are idempotent (research.md R-002).
/// </para>
/// </remarks>
public class BeerDay : AggregateRoot
{
    public Guid DietPlanId { get; private set; }

    /// <summary>Denormalised so every query filters by owner without crossing an aggregate.</summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The calendar day. Fixed when the day is created - a beer day is moved by being unmarked and
    /// re-marked, never edited.
    /// </summary>
    public DateOnly Date { get; private set; }

    private BeerDay() { }

    /// <summary>
    /// Marks a date as a beer day. The caller checks first whether one already exists; marking is
    /// only reached for a date that is not yet a beer day (FR-017).
    /// </summary>
    public static BeerDay Mark(
        Guid dietPlanId,
        Guid userId,
        DateOnly date,
        DateOnly planStartDate,
        DateTime? asOf = null)
    {
        var today = DateOnly.FromDateTime(asOf ?? DateTime.UtcNow);

        CheckRule(new BeerDateCannotBeInFutureRule(date, today));
        CheckRule(new BeerDateCannotPrecedePlanStartRule(date, planStartDate));

        var day = new BeerDay
        {
            DietPlanId = dietPlanId,
            UserId = userId,
            Date = date
        };

        day.Emit(new BeerDayMarkedEvent(day.Id, date));
        return day;
    }
}
