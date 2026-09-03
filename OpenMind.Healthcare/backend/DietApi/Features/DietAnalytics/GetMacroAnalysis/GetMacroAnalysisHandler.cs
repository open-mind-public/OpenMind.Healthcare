using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietAnalytics.GetMacroAnalysis;

/// <summary>
/// A member's macronutrients over a period, against the targets that were in force.
/// </summary>
/// <remarks>
/// Reads only the per-day rows: the targets come from each day's own stored snapshot, and the
/// grams are summed in the domain rather than in SQL (ADR 0002). Returns null only when the member
/// has no plan.
/// </remarks>
public record GetMacroAnalysisQuery(PeriodPreset Preset) : IRequest<MacroAnalysisResponse?>;

public class GetMacroAnalysisHandler(
    IDietPlanRepository planRepository,
    IDietAnalyticsRepository analytics,
    AnalysisPeriodResolver resolver,
    MacronutrientAnalyser analyser,
    IUserService userService) : IRequestHandler<GetMacroAnalysisQuery, MacroAnalysisResponse?>
{
    public async Task<MacroAnalysisResponse?> Handle(
        GetMacroAnalysisQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var period = resolver.Resolve(request.Preset, plan.StartDate);
        var days = await analytics.GetDayRowsAsync(userId, period.From, period.To, cancellationToken);
        period = period.WithLoggedDays(days.Count);

        var comparison = analyser.Analyse(days);

        return new MacroAnalysisResponse(
            DietAnalyticsMapper.ToDto(period),
            comparison.AveragedOverDays,
            comparison.HasTargets,
            new MacroAmountsDto(comparison.ProteinG, comparison.CarbsG, comparison.FatG),

            // Null rather than a substituted plan target. A client must present the split alone
            // when the member set no macronutrient targets (FR-012).
            comparison.HasTargets
                ? new MacroAmountsDto(
                    comparison.TargetProteinG ?? 0m,
                    comparison.TargetCarbsG ?? 0m,
                    comparison.TargetFatG ?? 0m)
                : null,
            new MacroSharesDto(comparison.ProteinShare, comparison.CarbsShare, comparison.FatShare));
    }
}
