using DietApi.Domain.Aggregates;

namespace DietApi.Tests.TestSupport;

/// <summary>
/// Builds a <see cref="BeerDay"/> around a pinned moment, so assertions never depend on when the
/// test runs.
/// </summary>
public sealed class BeerDayBuilder
{
    private readonly DateTime _clock = DateTime.UtcNow;

    private Guid _planId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private int _daysAgo;
    private int _planStartedDaysAgo = 30;

    public static BeerDayBuilder ADay() => new();

    public DateTime Clock => _clock;
    public DateOnly Today => DateOnly.FromDateTime(_clock);
    public DateOnly Date => Today.AddDays(-_daysAgo);
    public DateOnly PlanStartDate => Today.AddDays(-_planStartedDaysAgo);
    public Guid UserId => _userId;
    public Guid PlanId => _planId;

    public BeerDayBuilder ForUser(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public BeerDayBuilder ForPlan(Guid planId)
    {
        _planId = planId;
        return this;
    }

    public BeerDayBuilder DaysAgo(int days)
    {
        _daysAgo = days;
        return this;
    }

    public BeerDayBuilder PlanStartedDaysAgo(int days)
    {
        _planStartedDaysAgo = days;
        return this;
    }

    public BeerDay Build() => BeerDay.Mark(_planId, _userId, Date, PlanStartDate, _clock);
}
