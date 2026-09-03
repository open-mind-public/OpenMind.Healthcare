using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Observations.Rules;

/// <summary>
/// One food accounting for a large share of everything logged.
/// </summary>
/// <remarks>
/// Reviewed against FR-019: names the food and its share. It does not say the food is bad, or that
/// eating one thing often is a mistake — for many members it is porridge, and that is fine.
/// </remarks>
public class SingleFoodDominanceRule : IObservationRule
{
    public const decimal Threshold = 15m;

    public ObservationFamily Family => ObservationFamily.Composition;

    public int MinimumLoggedDays => ObservationThresholds.MinimumLoggedDays;

    public string ThresholdDescription => $"one food at or above {Threshold}% of the period's energy";

    public Observation? Evaluate(AnalyticsFigures figures)
    {
        var top = figures.TopFoods.FirstOrDefault();

        if (top is null || top.ShareOfTotal < Threshold)
            return null;

        return Observation.Create(
            Family,
            $"{top.FoodName} accounted for {top.ShareOfTotal:0.#}% of everything you logged, across {top.TimesLogged} entries.",
            $"{top.ShareOfTotal:0.#}%",
            ObservationThresholds.Strength(top.ShareOfTotal, Threshold, ceiling: 40m),
            figures.Period.LoggedDays);
    }
}

/// <summary>
/// One meal carrying most of the day.
/// </summary>
/// <remarks>
/// Reviewed against FR-019: reports the share. It does not recommend redistributing meals.
/// </remarks>
public class MealSkewRule : IObservationRule
{
    public const decimal Threshold = 45m;

    public ObservationFamily Family => ObservationFamily.Composition;

    public int MinimumLoggedDays => ObservationThresholds.MinimumLoggedDays;

    public string ThresholdDescription => $"one meal at or above {Threshold}% of the period's energy";

    public Observation? Evaluate(AnalyticsFigures figures)
    {
        var largest = figures.Meals.Shares.OrderByDescending(s => s.ShareOfTotal).FirstOrDefault();

        if (largest is null || largest.ShareOfTotal < Threshold)
            return null;

        return Observation.Create(
            Family,
            $"{largest.Meal} was {largest.ShareOfTotal:0.#}% of what you logged.",
            $"{largest.ShareOfTotal:0.#}%",
            ObservationThresholds.Strength(largest.ShareOfTotal, Threshold, ceiling: 75m),
            figures.Period.LoggedDays);
    }
}

/// <summary>
/// Little of the period's energy coming from fruit and vegetables.
/// </summary>
/// <remarks>
/// Reviewed against FR-019, and the most delicate of the seven. It reports a share of
/// <em>energy</em> and says so, because fruit and vegetables are low in energy by nature and a
/// member eating plenty of them will still see a small percentage here. It does not tell anyone to
/// eat more vegetables, and it does not call a diet unhealthy — that is dietary advice, which this
/// release does not give.
/// </remarks>
public class LowPlantShareRule : IObservationRule
{
    public const decimal Threshold = 10m;

    public ObservationFamily Family => ObservationFamily.Composition;

    public int MinimumLoggedDays => ObservationThresholds.MinimumLoggedDays;

    public string ThresholdDescription => $"fruit and vegetables below {Threshold}% of the period's energy";

    public Observation? Evaluate(AnalyticsFigures figures)
    {
        if (figures.Intake.TotalKilocalories <= 0)
            return null;

        var share = figures.Categories.PlantShare;

        if (share >= Threshold)
            return null;

        return Observation.Create(
            Family,
            $"Fruit and vegetables came to {share:0.#}% of the energy you logged.",
            $"{share:0.#}%",

            // Inverted: the further below the threshold, the stronger. Zero is the floor.
            ObservationThresholds.Strength(Threshold - share, 0m, ceiling: Threshold),
            figures.Period.LoggedDays);
    }
}
