using System.Reflection;
using DietApi.Features.Exercise;
using DietApi.Features.ExerciseShortcuts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DietApi.Tests.Features;

/// <summary>
/// The structural guarantees, asserted so they hold for types nobody has written yet.
/// </summary>
public class ShortcutBoundaryTests
{
    [Fact]
    public void No_shortcut_shape_carries_a_figure_that_could_go_stale()
    {
        // A shortcut is an instruction to record in future. Anything cached on it - an estimate, a
        // MET, a body weight - is guaranteed to be out of date by the time it is used, from a
        // control that gives no hint of it (FR-010).
        string[] forbidden = ["Met", "Estimated", "Kilocalor", "Kcal", "WeightKg", "Calories"];

        foreach (var shape in new[] { typeof(ExerciseShortcutDto), typeof(ExerciseShortcutListResponse) })
        {
            foreach (var property in shape.GetProperties())
            {
                foreach (var word in forbidden)
                {
                    property.Name.ShouldNotContain(
                        word,
                        Case.Insensitive,
                        $"{shape.Name}.{property.Name} caches a figure on a shortcut. "
                        + "The estimate is computed when the session is recorded (FR-010).");
                }
            }
        }
    }

    [Fact]
    public void No_recorded_session_carries_the_shortcut_that_produced_it()
    {
        // A session records what happened, not which button produced it. A link back would make
        // deleting a shortcut a question about history rather than about a button.
        foreach (var property in typeof(ExerciseEntryDto).GetProperties())
        {
            property.Name.ShouldNotContain("Shortcut", Case.Insensitive);
        }

        foreach (var property in typeof(ExerciseDayDto).GetProperties())
        {
            property.Name.ShouldNotContain("Shortcut", Case.Insensitive);
        }
    }

    [Fact]
    public void The_stored_shortcut_holds_only_a_reference_and_the_two_values_a_member_would_type()
    {
        var properties = typeof(DietApi.Domain.Entities.ExerciseShortcut)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        properties.ShouldContain("ActivityTypeId");
        properties.ShouldContain("DurationMinutes");

        properties.ShouldNotContain("Met");
        properties.ShouldNotContain("ActivityName");
        properties.ShouldNotContain("EstimatedKcal");
    }

    [Fact]
    public void Every_shortcut_route_requires_a_signed_in_member()
    {
        var routes = ShortcutRoutes();

        routes.ShouldNotBeEmpty("the shortcut endpoints did not register");

        foreach (var route in routes)
        {
            route.Metadata.GetMetadata<IAuthorizeData>()
                .ShouldNotBeNull($"{route.RoutePattern.RawText} is reachable without a token");
        }
    }

    [Fact]
    public void The_reorder_route_is_not_swallowed_by_the_id_route()
    {
        // "order" is a literal segment and {id:guid} is a parameter, so routing prefers the
        // literal - but a Guid constraint is what makes that unambiguous, and it is worth pinning.
        var routes = ShortcutRoutes();

        routes.Select(r => r.RoutePattern.RawText).ShouldContain("/api/exercise-shortcuts/order");
        routes.Select(r => r.RoutePattern.RawText).ShouldContain("/api/exercise-shortcuts/{id:guid}");
    }

    private static List<RouteEndpoint> ShortcutRoutes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddAuthorization();
        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ExerciseShortcutDto).Assembly));

        var app = builder.Build();
        app.MapExerciseShortcutsEndpoints();

        return
        [
            .. ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>()
                .Where(e => e.RoutePattern.RawText?.StartsWith("/api/exercise-shortcuts") == true)
        ];
    }
}
