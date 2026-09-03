namespace DietApi.Domain.Services;

/// <summary>
/// Turns parts of a total into percentages that still add up once they are rounded.
/// </summary>
/// <remarks>
/// Rounding each share independently is the obvious approach and it produces breakdowns that sum
/// to 99.9 or 100.1, which a member reading a list of percentages will notice. The largest
/// remainder method gives the leftover tenth to whichever part was rounded down hardest, so the
/// displayed figures sum to exactly 100 without any single one being wrong by more than a tenth.
/// </remarks>
public static class PercentageShares
{
    /// <summary>
    /// Shares of <paramref name="total"/>, to one decimal place, summing to exactly 100 whenever
    /// the total is positive. An empty or zero total yields all zeros.
    /// </summary>
    public static IReadOnlyList<decimal> Of(IReadOnlyList<int> parts, int total)
    {
        if (parts.Count == 0)
            return [];

        if (total <= 0)
            return [.. parts.Select(_ => 0m)];

        // Work in tenths of a percent as integers, so the remainder comparison is exact.
        var exact = parts.Select(p => p * 1000m / total).ToList();
        var floors = exact.Select(v => (int)Math.Floor(v)).ToList();

        var shortfall = 1000 - floors.Sum();

        // Hand the missing tenths to the parts with the largest fractional remainders. Ties go to
        // the earlier part, so the result depends only on the figures and not on sort stability.
        var order = Enumerable.Range(0, parts.Count)
            .OrderByDescending(i => exact[i] - floors[i])
            .ThenBy(i => i)
            .ToList();

        for (var i = 0; i < shortfall; i++)
        {
            floors[order[i % order.Count]]++;
        }

        return [.. floors.Select(f => f / 10m)];
    }
}
