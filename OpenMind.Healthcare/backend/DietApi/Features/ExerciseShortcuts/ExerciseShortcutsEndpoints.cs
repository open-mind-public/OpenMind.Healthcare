using DDD.BuildingBlocks;
using DietApi.Features.ExerciseShortcuts.CreateShortcut;
using DietApi.Features.ExerciseShortcuts.DeleteShortcut;
using DietApi.Features.ExerciseShortcuts.GetShortcuts;
using DietApi.Features.ExerciseShortcuts.RenameShortcut;
using DietApi.Features.ExerciseShortcuts.ReorderShortcuts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DietApi.Features.ExerciseShortcuts;

public static class ExerciseShortcutsEndpoints
{
    public static void MapExerciseShortcutsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exercise-shortcuts")
            .WithTags("ExerciseShortcuts")
            .RequireAuthorization();

        group.MapGet("/", GetShortcuts)
            .WithName("GetExerciseShortcuts")
            .WithOpenApi();

        group.MapPost("/", CreateShortcut)
            .WithName("CreateExerciseShortcut")
            .WithOpenApi();

        // Ahead of the {id} route in the file for readability; ASP.NET Core routing prefers the
        // literal segment regardless, so "order" is never parsed as an id.
        group.MapPut("/order", ReorderShortcuts)
            .WithName("ReorderExerciseShortcuts")
            .WithOpenApi();

        group.MapPut("/{id:guid}", RenameShortcut)
            .WithName("RenameExerciseShortcut")
            .WithOpenApi();

        group.MapDelete("/{id:guid}", DeleteShortcut)
            .WithName("DeleteExerciseShortcut")
            .WithOpenApi();
    }

    private static async Task<IResult> GetShortcuts(IMediator mediator)
    {
        var shortcuts = await mediator.Send(new GetShortcutsQuery());
        return shortcuts is null ? Results.NotFound() : Results.Ok(shortcuts);
    }

    private static async Task<IResult> CreateShortcut(
        [FromBody] CreateShortcutRequest request, IMediator mediator)
    {
        try
        {
            // Null means the activity is not in the catalogue - a 404 about the activity, not
            // about the member's shortcuts.
            var shortcuts = await mediator.Send(new CreateShortcutCommand(request));

            return shortcuts is null
                ? Results.NotFound(new { message = "That activity is not in the catalogue" })
                : Results.Ok(shortcuts);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RenameShortcut(
        Guid id, [FromBody] RenameShortcutRequest request, IMediator mediator)
    {
        try
        {
            var shortcuts = await mediator.Send(new RenameShortcutCommand(id, request));
            return shortcuts is null ? Results.NotFound() : Results.Ok(shortcuts);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ReorderShortcuts(
        [FromBody] ReorderShortcutsRequest request, IMediator mediator)
    {
        try
        {
            var shortcuts = await mediator.Send(new ReorderShortcutsCommand(request));
            return shortcuts is null ? Results.NotFound() : Results.Ok(shortcuts);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteShortcut(Guid id, IMediator mediator)
    {
        try
        {
            var shortcuts = await mediator.Send(new DeleteShortcutCommand(id));
            return shortcuts is null ? Results.NotFound() : Results.Ok(shortcuts);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
