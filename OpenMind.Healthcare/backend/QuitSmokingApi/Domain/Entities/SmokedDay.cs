using DDD.BuildingBlocks;
using QuitSmokingApi.Domain.Rules;

namespace QuitSmokingApi.Domain.Entities;

/// <summary>
/// Child entity of the QuitJourney aggregate representing a single calendar day on which
/// the user smoked (a "failed" day). Smoked days are excluded from the smoke-free totals.
/// Instances can only be created through the QuitJourney aggregate root.
/// </summary>
public class SmokedDay : Entity
{
    public Guid QuitJourneyId { get; private set; }
    public DateOnly Date { get; private set; }
    public int CigarettesSmoked { get; private set; }
    public RelapseTrigger Trigger { get; private set; }
    public string? Note { get; private set; }
    public DateTime RecordedAt { get; private set; } = DateTime.UtcNow;

    // Private constructor for EF Core
    private SmokedDay() { }

    private SmokedDay(Guid quitJourneyId, DateOnly date, int cigarettesSmoked, RelapseTrigger trigger, string? note)
    {
        QuitJourneyId = quitJourneyId;
        Date = date;
        CigarettesSmoked = cigarettesSmoked;
        Trigger = trigger;
        Note = Sanitize(note);
    }

    /// <summary>
    /// Records a smoked day. Internal so that the QuitJourney aggregate root stays the only
    /// entry point and can enforce the aggregate invariants first.
    /// </summary>
    internal static SmokedDay Record(Guid quitJourneyId, DateOnly date, int cigarettesSmoked, RelapseTrigger trigger, string? note)
    {
        CheckRule(new CigarettesSmokedMustBePositiveRule(cigarettesSmoked));
        return new SmokedDay(quitJourneyId, date, cigarettesSmoked, trigger, note);
    }

    /// <summary>
    /// Amends an already recorded smoked day (e.g. the user corrects the count or the trigger).
    /// </summary>
    internal void Amend(int cigarettesSmoked, RelapseTrigger trigger, string? note)
    {
        CheckRule(new CigarettesSmokedMustBePositiveRule(cigarettesSmoked));

        CigarettesSmoked = cigarettesSmoked;
        Trigger = trigger;
        Note = Sanitize(note);
        RecordedAt = DateTime.UtcNow;
    }

    private static void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
            throw new BusinessRuleValidationException(rule);
    }

    private static string? Sanitize(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return null;
        var trimmed = note.Trim();
        return trimmed.Length > MaxNoteLength ? trimmed[..MaxNoteLength] : trimmed;
    }

    public const int MaxNoteLength = 500;
}

/// <summary>
/// What pushed the user back to smoking on a given day. Used to power relapse analytics.
/// </summary>
public enum RelapseTrigger
{
    Bathroom = 0,
    Stress,
    Social,
    Alcohol,
    Boredom,
    AfterMeal,
    Coffee,
    Emotional,
    WorkPressure,
    Habit,
    Other
}
