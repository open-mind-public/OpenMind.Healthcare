using DietApi.Domain.Aggregates;
using DietApi.Domain.ValueObjects;

namespace DietApi.Features.DietGuidance;

public record EatingTipDto(Guid Id, string Title, string Description, string Icon, TipCategory Category);

/// <summary>
/// A message that reflects where the member actually is. <c>Tone</c> lets the client style it
/// without re-deriving the reason.
/// </summary>
public record DailyEncouragementDto(string Message, int CurrentStreakDays, string Tone);

public static class DietGuidanceMapper
{
    public static EatingTipDto ToDto(EatingTip tip) =>
        new(tip.Id, tip.Title, tip.Description, tip.Icon, tip.Category);
}
