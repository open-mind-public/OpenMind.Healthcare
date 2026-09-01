using DDD.BuildingBlocks;
using QuitSmokingApi.Domain.Entities;
using QuitSmokingApi.Domain.Events;
using QuitSmokingApi.Domain.Rules;
using QuitSmokingApi.Domain.Specifications;
using QuitSmokingApi.Domain.ValueObjects;

namespace QuitSmokingApi.Domain.Aggregates;

public class QuitJourney : AggregateRoot
{
    private const int MinutesOfLifePerCigarette = 11;
    private const int TrendWindowDays = 30;
    private const int MonthlyBreakdownMonths = 12;

    private readonly List<SmokedDay> _smokedDays = [];

    public Guid UserId { get; private set; }
    public DateTime QuitDate { get; private set; }
    public SmokingHabits SmokingHabits { get; private set; } = null!;

    /// <summary>
    /// Days the user marked as smoked ("failed" days). Part of the aggregate - only mutable
    /// through <see cref="MarkDayAsSmoked"/> and <see cref="UnmarkSmokedDay"/>.
    /// </summary>
    public IReadOnlyCollection<SmokedDay> SmokedDays => _smokedDays;

    private QuitJourney() { }

    private QuitJourney(Guid userId, DateTime quitDate, SmokingHabits smokingHabits)
    {
        UserId = userId;
        QuitDate = quitDate;
        SmokingHabits = smokingHabits;

        Emit(new JourneyStartedEvent(Id, quitDate));
    }

    public static QuitJourney Start(Guid userId, DateTime quitDate, int cigarettesPerDay, int cigarettesPerPack, decimal pricePerPack, string currency = "USD")
    {
        // Check business rules using IBusinessRule pattern
        CheckRule(new QuitDateCannotBeInFutureRule(quitDate));
        CheckRule(new CigarettesPerDayMustBePositiveRule(cigarettesPerDay));
        CheckRule(new PricePerPackMustBePositiveRule(pricePerPack));

        // Validate using Specification pattern
        var userIdSpec = QuitJourneySpecs.UserIdNotEmpty();
        if (!userIdSpec.IsSatisfiedBy(userId))
            throw new DomainException(userIdSpec.RuleDescription);

        var habits = SmokingHabits.Create(cigarettesPerDay, cigarettesPerPack, pricePerPack, currency);
        return new QuitJourney(userId, quitDate, habits);
    }

    public void Update(DateTime quitDate, int cigarettesPerDay, int cigarettesPerPack, decimal pricePerPack, string currency = "USD")
    {
        // Check business rules
        CheckRule(new QuitDateCannotBeInFutureRule(quitDate));
        CheckRule(new CigarettesPerDayMustBePositiveRule(cigarettesPerDay));
        CheckRule(new PricePerPackMustBePositiveRule(pricePerPack));

        QuitDate = quitDate;
        SmokingHabits = SmokingHabits.Create(cigarettesPerDay, cigarettesPerPack, pricePerPack, currency);

        // Days recorded before the new quit date are no longer part of the journey
        _smokedDays.RemoveAll(d => d.Date < QuitDay);

        SetUpdated();

        Emit(new JourneyUpdatedEvent(Id, quitDate));
    }

    #region Smoked days

    /// <summary>The calendar day the journey started on.</summary>
    public DateOnly QuitDay => DateOnly.FromDateTime(QuitDate);

    /// <summary>
    /// Marks a calendar day as smoked. Re-marking an already marked day amends it instead of
    /// creating a duplicate, which keeps one record per day as the aggregate invariant.
    /// </summary>
    public SmokedDay MarkDayAsSmoked(DateOnly date, int cigarettesSmoked, RelapseTrigger trigger = RelapseTrigger.Unspecified, string? note = null, DateTime? asOf = null)
    {
        CheckRule(new SmokedDayCannotBeBeforeQuitDateRule(date, QuitDay));
        CheckRule(new SmokedDayCannotBeInFutureRule(date, Today(asOf)));

        var existing = _smokedDays.FirstOrDefault(d => d.Date == date);
        if (existing is not null)
        {
            existing.Amend(cigarettesSmoked, trigger, note);
        }
        else
        {
            existing = SmokedDay.Record(Id, date, cigarettesSmoked, trigger, note);
            _smokedDays.Add(existing);
        }

        SetUpdated();
        Emit(new DayMarkedAsSmokedEvent(Id, date, cigarettesSmoked, trigger));

        return existing;
    }

    /// <summary>
    /// Removes a previously marked smoked day. Returns false when the day was never marked.
    /// </summary>
    public bool UnmarkSmokedDay(DateOnly date)
    {
        var existing = _smokedDays.FirstOrDefault(d => d.Date == date);
        if (existing is null) return false;

        _smokedDays.Remove(existing);
        SetUpdated();
        Emit(new SmokedDayRemovedEvent(Id, date));

        return true;
    }

    public bool IsSmokedDay(DateOnly date) => _smokedDays.Any(d => d.Date == date);

    public SmokedDay? GetSmokedDay(DateOnly date) => _smokedDays.FirstOrDefault(d => d.Date == date);

    /// <summary>Smoked days inside the journey window, oldest first.</summary>
    public IReadOnlyList<SmokedDay> GetSmokedDaysInJourney(DateTime? asOf = null)
    {
        var today = Today(asOf);
        return _smokedDays
            .Where(d => d.Date >= QuitDay && d.Date <= today)
            .OrderBy(d => d.Date)
            .ToList();
    }

    public IReadOnlyList<SmokedDay> GetSmokedDaysBetween(DateOnly from, DateOnly to) =>
        _smokedDays
            .Where(d => d.Date >= from && d.Date <= to)
            .OrderBy(d => d.Date)
            .ToList();

    public int GetSmokedDayCount(DateTime? asOf = null) => GetSmokedDaysInJourney(asOf).Count;

    public int GetCigarettesSmoked(DateTime? asOf = null) =>
        GetSmokedDaysInJourney(asOf).Sum(d => d.CigarettesSmoked);

    public Money GetMoneySpentOnRelapses(DateTime? asOf = null) =>
        SmokingHabits.PricePerCigarette.Multiply(GetCigarettesSmoked(asOf));

    public Duration GetLifeLostToRelapses(DateTime? asOf = null) =>
        Duration.FromMinutes(GetCigarettesSmoked(asOf) * MinutesOfLifePerCigarette);

    #endregion

    public ProgressStatistics GetStatistics(DateTime? asOf = null)
    {
        var duration = GetTimeSmokeFree(asOf);
        var daysSmokeFree = GetDaysSmokeFree(asOf);
        var currentMilestone = GetCurrentMilestone(asOf);
        var nextMilestone = GetNextMilestone(asOf);

        return new ProgressStatistics(
            daysSmokeFree: daysSmokeFree,
            hoursSmokeFree: duration.Hours,
            minutesSmokeFree: duration.TotalMinutes,
            cigarettesAvoided: GetCigarettesAvoided(asOf),
            moneySaved: GetMoneySaved(asOf),
            lifeRegained: GetLifeRegained(asOf),
            progressPercentage: GetProgressPercentage(asOf),
            currentMilestone: currentMilestone,
            nextMilestone: nextMilestone,
            daysToNextMilestone: nextMilestone?.GetDaysRemaining(daysSmokeFree) ?? 0,
            totalDaysInJourney: GetTotalDaysInJourney(asOf),
            smokedDays: GetSmokedDayCount(asOf),
            cigarettesSmoked: GetCigarettesSmoked(asOf),
            moneySpentOnRelapses: GetMoneySpentOnRelapses(asOf),
            currentStreak: GetCurrentStreak(asOf),
            longestStreak: GetLongestStreak(asOf)
        );
    }

    /// <summary>Wall-clock time elapsed since the quit date, smoked days included.</summary>
    public Duration GetTimeSinceQuit(DateTime? asOf = null)
    {
        var now = asOf ?? DateTime.UtcNow;
        var timeSpan = now - QuitDate;
        return Duration.FromMinutes(Math.Max(0, (int)timeSpan.TotalMinutes));
    }

    /// <summary>Time elapsed since the quit date with the days marked as smoked deducted.</summary>
    public Duration GetTimeSmokeFree(DateTime? asOf = null)
    {
        var elapsedMinutes = GetTimeSinceQuit(asOf).TotalMinutes;
        var smokedMinutes = GetSmokedDayCount(asOf) * 24 * 60;
        return Duration.FromMinutes(Math.Max(0, elapsedMinutes - smokedMinutes));
    }

    /// <summary>Calendar days elapsed since the quit date, smoked days included.</summary>
    public int GetTotalDaysInJourney(DateTime? asOf = null)
    {
        var now = asOf ?? DateTime.UtcNow;
        return Math.Max(0, (int)(now - QuitDate).TotalDays);
    }

    /// <summary>Days elapsed since the quit date, excluding the days marked as smoked.</summary>
    public int GetDaysSmokeFree(DateTime? asOf = null)
    {
        return Math.Max(0, GetTotalDaysInJourney(asOf) - GetSmokedDayCount(asOf));
    }

    public int GetCigarettesAvoided(DateTime? asOf = null)
    {
        var elapsedDays = GetTimeSinceQuit(asOf).TotalMinutes / (24.0 * 60.0);
        var smokeFreeDays = Math.Max(0, elapsedDays - GetSmokedDayCount(asOf));
        return (int)(smokeFreeDays * SmokingHabits.CigarettesPerDay);
    }

    public Money GetMoneySaved(DateTime? asOf = null)
    {
        var cigarettesAvoided = GetCigarettesAvoided(asOf);
        return SmokingHabits.PricePerCigarette.Multiply(cigarettesAvoided);
    }

    public Duration GetLifeRegained(DateTime? asOf = null)
    {
        var cigarettesAvoided = GetCigarettesAvoided(asOf);
        return Duration.FromMinutes(cigarettesAvoided * MinutesOfLifePerCigarette);
    }

    public Milestone GetCurrentMilestone(DateTime? asOf = null)
    {
        var days = GetDaysSmokeFree(asOf);
        return Milestone.GetMilestoneForDays(days);
    }

    public Milestone? GetNextMilestone(DateTime? asOf = null)
    {
        var days = GetDaysSmokeFree(asOf);
        return Milestone.GetNextMilestone(days);
    }

    public double GetProgressPercentage(DateTime? asOf = null)
    {
        var days = GetDaysSmokeFree(asOf);
        return Math.Min(100, (double)days / 365 * 100);
    }

    #region Streaks and analytics

    /// <summary>
    /// Consecutive smoke-free calendar days ending today. Resets to zero when today is marked as smoked.
    /// </summary>
    public int GetCurrentStreak(DateTime? asOf = null)
    {
        var today = Today(asOf);
        if (today < QuitDay) return 0;

        var smoked = SmokedDayLookup();
        var streak = 0;

        for (var day = today; day >= QuitDay; day = day.AddDays(-1))
        {
            if (smoked.Contains(day)) break;
            streak++;
        }

        return CapToJourney(streak, asOf);
    }

    /// <summary>Longest run of consecutive smoke-free calendar days in the journey so far.</summary>
    public int GetLongestStreak(DateTime? asOf = null)
    {
        var today = Today(asOf);
        if (today < QuitDay) return 0;

        var smoked = SmokedDayLookup();
        var longest = 0;
        var current = 0;

        for (var day = QuitDay; day <= today; day = day.AddDays(1))
        {
            if (smoked.Contains(day))
            {
                current = 0;
                continue;
            }

            current++;
            if (current > longest) longest = current;
        }

        return CapToJourney(longest, asOf);
    }

    /// <summary>
    /// Streaks are counted over calendar days (quit day and today both included), while the rest of
    /// the journey counts elapsed 24h days. Capping keeps a streak from reading as longer than the
    /// journey itself on the day totals shown beside it.
    /// </summary>
    private int CapToJourney(int streakInDays, DateTime? asOf) =>
        Math.Min(streakInDays, GetTotalDaysInJourney(asOf));

    /// <summary>
    /// Builds the full analytics snapshot for the days the user marked as smoked.
    /// </summary>
    public RelapseAnalytics GetRelapseAnalytics(DateTime? asOf = null)
    {
        var currency = SmokingHabits.PricePerPack.Currency;
        var today = Today(asOf);
        if (today < QuitDay) return RelapseAnalytics.Empty(currency);

        var smokedDays = GetSmokedDaysInJourney(asOf);
        var smokedByDate = smokedDays.ToDictionary(d => d.Date);

        var weekdayTotals = new int[7];
        var weekdaySmoked = new int[7];
        var monthlyTotals = new Dictionary<(int Year, int Month), int>();
        var monthlySmoked = new Dictionary<(int Year, int Month), int>();
        var monthlyCigarettes = new Dictionary<(int Year, int Month), int>();

        for (var day = QuitDay; day <= today; day = day.AddDays(1))
        {
            var weekdayIndex = (int)day.DayOfWeek;
            var monthKey = (day.Year, day.Month);

            weekdayTotals[weekdayIndex]++;
            monthlyTotals[monthKey] = monthlyTotals.GetValueOrDefault(monthKey) + 1;

            if (!smokedByDate.TryGetValue(day, out var smokedDay)) continue;

            weekdaySmoked[weekdayIndex]++;
            monthlySmoked[monthKey] = monthlySmoked.GetValueOrDefault(monthKey) + 1;
            monthlyCigarettes[monthKey] = monthlyCigarettes.GetValueOrDefault(monthKey) + smokedDay.CigarettesSmoked;
        }

        var triggerBreakdown = smokedDays
            .GroupBy(d => d.Trigger)
            .Select(g => new TriggerStat(g.Key, g.Count(), g.Sum(d => d.CigarettesSmoked), smokedDays.Count))
            .OrderByDescending(t => t.Days)
            .ThenBy(t => t.Trigger)
            .ToList();

        var weekdayBreakdown = Enumerable.Range(0, 7)
            .Select(i => new WeekdayStat((DayOfWeek)i, weekdaySmoked[i], weekdayTotals[i]))
            .ToList();

        var monthlyBreakdown = monthlyTotals.Keys
            .OrderByDescending(k => k.Year).ThenByDescending(k => k.Month)
            .Take(MonthlyBreakdownMonths)
            .OrderBy(k => k.Year).ThenBy(k => k.Month)
            .Select(k => new MonthlyStat(
                k.Year,
                k.Month,
                monthlySmoked.GetValueOrDefault(k),
                monthlyTotals[k],
                monthlyCigarettes.GetValueOrDefault(k)))
            .ToList();

        var lastRelapse = smokedDays.Count > 0 ? smokedDays[^1].Date : (DateOnly?)null;
        var firstRelapse = smokedDays.Count > 0 ? smokedDays[0].Date : (DateOnly?)null;

        var windowStart = today.AddDays(-(TrendWindowDays - 1));
        var previousWindowStart = today.AddDays(-(TrendWindowDays * 2 - 1));
        var relapsesLast30 = smokedDays.Count(d => d.Date >= windowStart);
        var relapsesPrevious30 = smokedDays.Count(d => d.Date >= previousWindowStart && d.Date < windowStart);

        var totalDays = GetTotalDaysInJourney(asOf);

        return new RelapseAnalytics(
            totalDaysInJourney: totalDays,
            smokeFreeDays: GetDaysSmokeFree(asOf),
            smokedDays: smokedDays.Count,
            totalCigarettesSmoked: smokedDays.Sum(d => d.CigarettesSmoked),
            moneySpentOnRelapses: GetMoneySpentOnRelapses(asOf),
            moneySaved: GetMoneySaved(asOf),
            lifeLostToRelapses: GetLifeLostToRelapses(asOf),
            currentStreak: GetCurrentStreak(asOf),
            longestStreak: GetLongestStreak(asOf),
            lastRelapseDate: lastRelapse,
            firstRelapseDate: firstRelapse,
            daysSinceLastRelapse: lastRelapse.HasValue ? today.DayNumber - lastRelapse.Value.DayNumber : 0,
            averageCigarettesPerRelapseDay: smokedDays.Count == 0 ? 0 : (double)smokedDays.Sum(d => d.CigarettesSmoked) / smokedDays.Count,
            averageDaysBetweenRelapses: smokedDays.Count == 0 ? 0 : (double)(totalDays + 1) / smokedDays.Count,
            relapsesLast30Days: relapsesLast30,
            relapsesPrevious30Days: relapsesPrevious30,
            trend: DetermineTrend(relapsesLast30, relapsesPrevious30, totalDays),
            mostCommonTrigger: triggerBreakdown.Count > 0 ? triggerBreakdown[0].Trigger : null,
            riskiestWeekday: DetermineRiskiestWeekday(weekdayBreakdown),
            triggerBreakdown: triggerBreakdown,
            weekdayBreakdown: weekdayBreakdown,
            monthlyBreakdown: monthlyBreakdown);
    }

    private static RelapseTrend DetermineTrend(int last30, int previous30, int totalDays)
    {
        // Without a full previous window there is nothing meaningful to compare against
        if (totalDays < TrendWindowDays * 2) return RelapseTrend.NotEnoughData;
        if (last30 == previous30) return RelapseTrend.Stable;
        return last30 < previous30 ? RelapseTrend.Improving : RelapseTrend.Worsening;
    }

    private static DayOfWeek? DetermineRiskiestWeekday(IReadOnlyList<WeekdayStat> weekdays)
    {
        var riskiest = weekdays
            .Where(w => w.SmokedDays > 0)
            .OrderByDescending(w => w.RelapseRate)
            .ThenByDescending(w => w.SmokedDays)
            .FirstOrDefault();

        return riskiest?.Weekday;
    }

    private HashSet<DateOnly> SmokedDayLookup() => _smokedDays.Select(d => d.Date).ToHashSet();

    private static DateOnly Today(DateTime? asOf = null) => DateOnly.FromDateTime(asOf ?? DateTime.UtcNow);

    #endregion
}
