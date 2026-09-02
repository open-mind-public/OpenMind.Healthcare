using DietApi.Domain.ValueObjects;

namespace DietApi.Features.Weight;

public record WeightReadingDto(DateOnly Date, decimal WeightKg);

public record WeightTrendDto(
    IReadOnlyList<WeightReadingDto> Readings,
    decimal? StartWeightKg,
    decimal? CurrentWeightKg,
    decimal? ChangeKg,
    decimal? TargetWeightKg,
    decimal? RemainingToTargetKg,
    bool GoalReached);

public record RecordWeightRequest(decimal WeightKg);

public static class WeightMapper
{
    public static WeightTrendDto ToDto(WeightTrend trend) =>
        new([.. trend.Readings.Select(r => new WeightReadingDto(r.Date, r.WeightKg))],
            trend.StartWeightKg,
            trend.CurrentWeightKg,
            trend.ChangeKg,
            trend.TargetWeightKg,
            trend.RemainingToTargetKg,
            trend.GoalReached);
}
