using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// The stable body details a target suggestion needs.
/// </summary>
/// <remarks>
/// Weight is deliberately absent. It changes, so it lives in the plan's weight readings where
/// there is one source of truth for "current weight" - see <c>DietPlan.CurrentWeightKg</c>.
/// </remarks>
public class BodyMetrics : ValueObject
{
    public decimal HeightCm { get; private set; }
    public int Age { get; private set; }
    public BiologicalSex Sex { get; private set; }

    // Private parameterless constructor for EF Core
    private BodyMetrics() { }

    private BodyMetrics(decimal heightCm, int age, BiologicalSex sex)
    {
        HeightCm = Math.Round(heightCm, 1);
        Age = age;
        Sex = sex;
    }

    public static BodyMetrics Create(decimal heightCm, int age, BiologicalSex sex) =>
        new(heightCm, age, sex);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return HeightCm;
        yield return Age;
        yield return Sex;
    }
}
