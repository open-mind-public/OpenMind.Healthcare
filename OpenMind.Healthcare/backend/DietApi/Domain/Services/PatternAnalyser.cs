using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Services;

/// <summary>
/// When a member eats: across the week, and across the day.
/// </summary>
/// <remarks>
/// The day-of-week half is straightforward — a logged day's date is a calendar day with no
/// timezone. The time-of-day half is the interesting one, and <see cref="RotateToLocal"/> explains
/// why the buckets arrive in quarter-hours rather than hours.
/// </remarks>
public class PatternAnalyser
{
    /// <summary>Quarter-hour buckets in a day. Every real-world UTC offset is a multiple of one.</summary>
    public const int BucketsPerDay = 96;

    private const int BucketsPerHour = 4;
    private const int MinutesPerBucket = 15;

    public WeekdayDistribution ByWeekday(IReadOnlyList<DayIntakeRow> days)
    {
        var byWeekday = days
            .GroupBy(d => d.Date.DayOfWeek)
            .ToDictionary(
                g => g.Key,
                g => (Average: (int)Math.Round((decimal)g.Sum(d => d.Calories) / g.Count(), MidpointRounding.AwayFromZero),
                      Count: g.Count()));

        return WeekdayDistribution.Create(
        [
            .. Enum.GetValues<DayOfWeek>().Select(day =>
                byWeekday.TryGetValue(day, out var found)
                    ? new WeekdayShare(day, found.Average, found.Count)

                    // A weekday with nothing logged reads as zero over zero days, not as missing.
                    : new WeekdayShare(day, 0, 0))
        ]);
    }

    public TimeOfDayDistribution ByHour(IReadOnlyList<QuarterHourRow> rows, int utcOffsetMinutes)
    {
        var local = RotateToLocal(rows, utcOffsetMinutes);
        var total = local.Sum();

        var hourly = new int[24];
        for (var bucket = 0; bucket < BucketsPerDay; bucket++)
        {
            hourly[bucket / BucketsPerHour] += local[bucket];
        }

        var shares = PercentageShares.Of(hourly, total);

        return TimeOfDayDistribution.Create(
            [.. hourly.Select((kcal, hour) => new HourShare(hour, kcal, shares[hour]))],
            utcOffsetMinutes);
    }

    /// <summary>
    /// Moves the 96 UTC quarter-hour buckets into the caller's local day.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is why the read model groups by quarter-hour rather than by hour. Rotating a 24-bucket
    /// hourly histogram only works for whole-hour offsets, and several hundred million people live
    /// at +05:30 (India), +05:45 (Nepal) or +09:30 (parts of Australia). At quarter-hour resolution
    /// every real-world offset is a whole number of buckets, so the rotation is exact rather than
    /// nearly right.
    /// </para>
    /// <para>
    /// Energy is conserved: rotation moves buckets, it never creates or discards one.
    /// </para>
    /// </remarks>
    private static int[] RotateToLocal(IReadOnlyList<QuarterHourRow> rows, int utcOffsetMinutes)
    {
        var utc = new int[BucketsPerDay];

        foreach (var row in rows)
        {
            var bucket = (row.Hour * BucketsPerHour) + row.Quarter;

            if (bucket is >= 0 and < BucketsPerDay)
                utc[bucket] += row.Kilocalories;
        }

        // Round to the nearest bucket so an offset that is not a multiple of fifteen minutes -
        // which no country currently uses, but which a caller could still send - lands somewhere
        // sensible rather than being silently truncated.
        var shift = (int)Math.Round(utcOffsetMinutes / (decimal)MinutesPerBucket, MidpointRounding.AwayFromZero);

        var local = new int[BucketsPerDay];
        for (var bucket = 0; bucket < BucketsPerDay; bucket++)
        {
            // C# keeps the sign of the dividend, so a negative offset needs bringing back into range.
            var target = ((bucket + shift) % BucketsPerDay + BucketsPerDay) % BucketsPerDay;
            local[target] = utc[bucket];
        }

        return local;
    }
}
