using DietApi.Features.DietAchievements.CheckNewDietAchievements;
using DietApi.Features.DietAchievements.GetDietAchievements;
using MediatR;

namespace DietApi.Features.DietAchievements;

public static class DietAchievementsEndpoints
{
    public static void MapDietAchievementsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/diet-achievements")
            .WithTags("DietAchievements")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllDietAchievements")
            .WithOpenApi();

        group.MapGet("/unlocked", GetUnlocked)
            .WithName("GetUnlockedDietAchievements")
            .WithOpenApi();

        group.MapPost("/check", Check)
            .WithName("CheckNewDietAchievements")
            .WithOpenApi();
    }

    private static async Task<IResult> GetAll(IMediator mediator)
    {
        var result = await mediator.Send(new GetDietAchievementsQuery());
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetUnlocked(IMediator mediator)
    {
        var result = await mediator.Send(new GetDietAchievementsQuery(UnlockedOnly: true));
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> Check(IMediator mediator)
    {
        var result = await mediator.Send(new CheckNewDietAchievementsCommand());
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
}
