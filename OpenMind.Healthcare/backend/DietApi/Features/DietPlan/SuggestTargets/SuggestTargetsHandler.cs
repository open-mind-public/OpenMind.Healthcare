using DietApi.Domain.Services;
using MediatR;

namespace DietApi.Features.DietPlan.SuggestTargets;

/// <summary>
/// Calculates a suggested target and persists nothing. Used during setup before a plan exists,
/// and again whenever body details change.
/// </summary>
public record SuggestTargetsQuery(SuggestTargetsRequest Request) : IRequest<TargetSuggestionDto>;

public class SuggestTargetsHandler(TargetSuggestionService suggestionService)
    : IRequestHandler<SuggestTargetsQuery, TargetSuggestionDto>
{
    public Task<TargetSuggestionDto> Handle(SuggestTargetsQuery request, CancellationToken cancellationToken)
    {
        var r = request.Request;

        var suggestion = suggestionService.Suggest(
            DietPlanMapper.ToDomain(r.BodyMetrics),
            r.CurrentWeightKg,
            r.ActivityLevel,
            r.Goal);

        return Task.FromResult(DietPlanMapper.ToDto(suggestion));
    }
}
