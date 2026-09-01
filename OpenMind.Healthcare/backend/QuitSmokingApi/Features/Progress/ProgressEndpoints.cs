using DDD.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuitSmokingApi.Domain.Aggregates;
using QuitSmokingApi.Features.Progress.CreateOrUpdateProgress;
using QuitSmokingApi.Features.Progress.GetHealthMilestones;
using QuitSmokingApi.Features.Progress.GetProgress;
using QuitSmokingApi.Features.Progress.GetStats;

namespace QuitSmokingApi.Features.Progress;

public static class ProgressEndpoints
{
    public static void MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/progress")
            .WithTags("Progress")
            .RequireAuthorization();

        group.MapGet("/", GetProgress)
            .WithName("GetProgress")
            .WithOpenApi();

        group.MapPost("/", CreateOrUpdateProgress)
            .WithName("CreateOrUpdateProgress")
            .WithOpenApi();

        group.MapGet("/stats", GetStats)
            .WithName("GetProgressStats")
            .WithOpenApi();

        group.MapGet("/health-milestones", GetHealthMilestones)
            .WithName("GetHealthMilestones")
            .WithOpenApi();
    }

    private static async Task<IResult> GetProgress(IMediator mediator)
    {
        var journey = await mediator.Send(new GetProgressQuery());
        if (journey is null) return Results.NotFound();
        
        // Map domain model to DTO
        var dto = new QuitJourneyDto(
            Id: journey.Id,
            QuitDate: journey.QuitDate,
            CigarettesPerDay: journey.SmokingHabits.CigarettesPerDay,
            CigarettesPerPack: journey.SmokingHabits.CigarettesPerPack,
            PricePerPack: journey.SmokingHabits.PricePerPack.Amount,
            Currency: journey.SmokingHabits.PricePerPack.Currency,
            CreatedAt: journey.CreatedAt,
            UpdatedAt: journey.UpdatedAt
        );
        return Results.Ok(dto);
    }

    private static async Task<IResult> CreateOrUpdateProgress(
        [FromBody] CreateOrUpdateProgressCommand command,
        IMediator mediator)
    {
        QuitJourney journey;
        try
        {
            journey = await mediator.Send(command);
        }
        catch (DomainException ex)
        {
            // e.g. a quit date in the future, or non-positive habits
            return Results.BadRequest(new { message = ex.Message });
        }

        // Map domain model to DTO
        var dto = new QuitJourneyDto(
            Id: journey.Id,
            QuitDate: journey.QuitDate,
            CigarettesPerDay: journey.SmokingHabits.CigarettesPerDay,
            CigarettesPerPack: journey.SmokingHabits.CigarettesPerPack,
            PricePerPack: journey.SmokingHabits.PricePerPack.Amount,
            Currency: journey.SmokingHabits.PricePerPack.Currency,
            CreatedAt: journey.CreatedAt,
            UpdatedAt: journey.UpdatedAt
        );
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetStats(IMediator mediator)
    {
        var stats = await mediator.Send(new GetStatsQuery());
        
        // Map domain model to DTO
        var dto = new ProgressStatsDto(
            DaysSmokeFree: stats.DaysSmokeFree,
            HoursSmokeFree: stats.HoursSmokeFree,
            MinutesSmokeFree: stats.MinutesSmokeFree,
            CigarettesNotSmoked: stats.CigarettesAvoided,
            MoneySaved: stats.MoneySaved.Amount,
            LifeRegainedMinutes: stats.LifeRegained.TotalMinutes,
            LifeRegainedFormatted: stats.LifeRegained.ToFriendlyString(),
            ProgressPercentage: stats.ProgressPercentage,
            CurrentMilestone: stats.CurrentMilestone.Name,
            NextMilestone: stats.NextMilestone?.Name,
            DaysToNextMilestone: stats.DaysToNextMilestone,
            TotalDaysInJourney: stats.TotalDaysInJourney,
            SmokedDays: stats.SmokedDays,
            CigarettesSmoked: stats.CigarettesSmoked,
            MoneySpentOnRelapses: stats.MoneySpentOnRelapses.Amount,
            CurrentStreak: stats.CurrentStreak,
            LongestStreak: stats.LongestStreak,
            SmokeFreeRate: stats.SmokeFreeRate
        );
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetHealthMilestones(IMediator mediator)
    {
        var milestones = await mediator.Send(new GetHealthMilestonesQuery());
        return Results.Ok(milestones);
    }
}
