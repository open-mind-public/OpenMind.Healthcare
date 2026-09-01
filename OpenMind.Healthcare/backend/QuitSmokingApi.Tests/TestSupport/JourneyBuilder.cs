using QuitSmokingApi.Domain.Aggregates;
using QuitSmokingApi.Domain.Entities;

namespace QuitSmokingApi.Tests.TestSupport;

/// <summary>
/// Builds a <see cref="QuitJourney"/> for a test around a single pinned moment.
/// Tests pass <see cref="Clock"/> back in as the "as of" argument so results never depend on
/// how long the test itself takes, or on when in the day it runs.
/// </summary>
public sealed class JourneyBuilder
{
    private readonly DateTime _clock = DateTime.UtcNow;

    private Guid _userId = Guid.NewGuid();
    private int _quitDaysAgo = 70;
    private TimeSpan _quitOffset = TimeSpan.Zero;
    private int _cigarettesPerDay = 20;
    private int _cigarettesPerPack = 20;
    private decimal _pricePerPack = 10m;
    private string _currency = "USD";

    private readonly List<(DateOnly Date, int Cigarettes, RelapseTrigger Trigger, string? Note)> _smokedDays = [];

    public static JourneyBuilder AJourney() => new();

    /// <summary>The moment the journey is measured at. Pass this as "as of" when asserting.</summary>
    public DateTime Clock => _clock;

    public DateOnly Today => DateOnly.FromDateTime(_clock);

    public DateOnly DaysAgo(int days) => Today.AddDays(-days);

    public Guid UserId => _userId;

    public JourneyBuilder ForUser(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public JourneyBuilder StartedDaysAgo(int days)
    {
        _quitDaysAgo = days;
        return this;
    }

    /// <summary>
    /// Nudges the quit instant off the exact day boundary - use to exercise part-way-through-a-day
    /// behaviour (a positive offset makes the journey that much shorter).
    /// </summary>
    public JourneyBuilder ShiftedBy(TimeSpan offset)
    {
        _quitOffset = offset;
        return this;
    }

    public JourneyBuilder Smoking(int cigarettesPerDay, int cigarettesPerPack = 20, decimal pricePerPack = 10m, string currency = "USD")
    {
        _cigarettesPerDay = cigarettesPerDay;
        _cigarettesPerPack = cigarettesPerPack;
        _pricePerPack = pricePerPack;
        _currency = currency;
        return this;
    }

    public JourneyBuilder SmokedDaysAgo(int daysAgo, int cigarettes = 5, RelapseTrigger trigger = RelapseTrigger.Bathroom, string? note = null)
    {
        _smokedDays.Add((DaysAgo(daysAgo), cigarettes, trigger, note));
        return this;
    }

    public JourneyBuilder SmokedOn(DateOnly date, int cigarettes = 5, RelapseTrigger trigger = RelapseTrigger.Bathroom, string? note = null)
    {
        _smokedDays.Add((date, cigarettes, trigger, note));
        return this;
    }

    public QuitJourney Build()
    {
        // Exactly N days before the clock, so N whole days have elapsed and the quit day lands on
        // Today.AddDays(-N) - both readable in assertions and independent of the time of day.
        var quitDate = _clock.AddDays(-_quitDaysAgo).Add(_quitOffset);

        var journey = QuitJourney.Start(
            _userId,
            quitDate,
            _cigarettesPerDay,
            _cigarettesPerPack,
            _pricePerPack,
            _currency);

        foreach (var day in _smokedDays)
        {
            journey.MarkDayAsSmoked(day.Date, day.Cigarettes, day.Trigger, day.Note, _clock);
        }

        return journey;
    }
}
