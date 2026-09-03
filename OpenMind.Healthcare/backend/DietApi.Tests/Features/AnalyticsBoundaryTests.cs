using System.Reflection;
using DietApi.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DietApi.Features.DietAnalytics;

namespace DietApi.Tests.Features;

/// <summary>
/// The guarantee carried forward from exercise logging, restated where it is most tempting to
/// break.
/// </summary>
/// <remarks>
/// An analytics feature is the single most natural place in this application for someone to
/// helpfully add a "net calories" column — intake and exercise are both to hand, and subtracting
/// one from the other looks like an obvious improvement. It is not. The whole exercise feature was
/// shaped around a member's calorie target never moving because they exercised, and a figure here
/// combining the two would undo that in the one screen a member studies most closely.
/// <para>
/// Asserted structurally rather than by review, so it holds for types nobody has written yet.
/// </para>
/// </remarks>
public class AnalyticsBoundaryTests
{
    /// <summary>Every response and nested shape this feature can put on the wire.</summary>
    public static TheoryData<Type> AnalyticsShapes()
    {
        var data = new TheoryData<Type>();

        foreach (var type in typeof(IntakeAnalysisResponse).Assembly
            .GetTypes()
            .Where(t => t.Namespace == typeof(IntakeAnalysisResponse).Namespace)
            .Where(t => t.IsClass && !t.IsAbstract)
            .OrderBy(t => t.Name))
        {
            data.Add(type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AnalyticsShapes))]
    public void No_analytics_shape_carries_a_figure_combining_exercise_with_intake(Type shape)
    {
        // Words that would only appear on a field that had merged the two.
        string[] forbidden = ["net", "available", "burned", "burnt", "exercise", "activity", "deficit", "surplus"];

        foreach (var property in shape.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            foreach (var word in forbidden)
            {
                property.Name.ShouldNotContain(
                    word,
                    Case.Insensitive,
                    $"{shape.Name}.{property.Name} reads like a figure combining exercise with intake. "
                    + "Recorded exercise is never calories available to eat (FR-023).");
            }
        }
    }

    [Theory]
    [MemberData(nameof(AnalyticsShapes))]
    public void No_analytics_shape_offers_a_spendable_allowance(Type shape)
    {
        // "Remaining" is the vocabulary of a budget. Analytics reports what happened; the day view
        // is where a member sees what is left, against a target that exercise does not move.
        foreach (var property in shape.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            property.Name.ShouldNotContain("Remaining", Case.Insensitive);
        }
    }

    [Fact]
    public void The_figures_a_rule_can_see_contain_no_exercise_data()
    {
        // If an observation could see exercise, one could be written that combined it with intake.
        foreach (var property in typeof(AnalyticsFigures).GetProperties())
        {
            property.Name.ShouldNotContain("Exercise", Case.Insensitive);
            property.PropertyType.Name.ShouldNotContain("Exercise", Case.Insensitive);
        }
    }

    [Fact]
    public void Every_analytics_route_is_a_get()
    {
        // Viewing analytics cannot change anything, so there is no verb here that could (FR-024).
        //
        // Asserted against the routes actually registered rather than by counting handler methods.
        // A count is a proxy that churns every time a read is added, and it would not have noticed
        // a POST slipped in beside four GETs.
        var app = MappedApp();

        var routes = AnalyticsRoutes(app);

        routes.ShouldNotBeEmpty("the analytics endpoints did not register");

        foreach (var route in routes)
        {
            var verbs = route.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];

            verbs.ShouldBe(["GET"], $"{route.RoutePattern.RawText} accepts something other than GET");
        }
    }

    [Fact]
    public void Every_analytics_route_requires_a_signed_in_member()
    {
        var app = MappedApp();

        var routes = AnalyticsRoutes(app);

        routes.ShouldNotBeEmpty();

        foreach (var route in routes)
        {
            route.Metadata.GetMetadata<IAuthorizeData>()
                .ShouldNotBeNull($"{route.RoutePattern.RawText} is reachable without a token");
        }
    }

    [Fact]
    public void Every_average_travels_with_its_denominator()
    {
        // The two must not be separable by a client, which is why they share an object (FR-003).
        var summary = typeof(IntakeSummaryDto).GetProperties().Select(p => p.Name).ToList();

        summary.ShouldContain(nameof(IntakeSummaryDto.AverageDailyKilocalories));
        summary.ShouldContain(nameof(IntakeSummaryDto.AveragedOverDays));
        summary.ShouldContain(nameof(IntakeSummaryDto.AveragedOver));

        typeof(MacroAnalysisResponse).GetProperties().Select(p => p.Name)
            .ShouldContain(nameof(MacroAnalysisResponse.AveragedOverDays));
    }

    [Fact]
    public void Every_observation_travels_with_its_figure_and_its_evidence()
    {
        var observation = typeof(ObservationDto).GetProperties().Select(p => p.Name).ToList();

        observation.ShouldContain(nameof(ObservationDto.Figure));
        observation.ShouldContain(nameof(ObservationDto.BasedOnDays));
    }

    /// <summary>
    /// The analytics routes as actually registered, without starting a host.
    /// </summary>
    private static List<RouteEndpoint> AnalyticsRoutes(WebApplication app) =>
        [.. ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("/api/diet-analytics") == true)];

    /// <summary>
    /// A bare app with the analytics endpoints mapped, so the registered routes can be inspected
    /// without starting a host.
    /// </summary>
    /// <remarks>
    /// MediatR is registered the way production does. Without it, minimal-API parameter binding
    /// infers the <c>IMediator</c> argument as a request body and refuses to build a GET.
    /// </remarks>
    private static WebApplication MappedApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddAuthorization();
        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DietAnalyticsEndpoints).Assembly));

        var app = builder.Build();
        app.MapDietAnalyticsEndpoints();
        return app;
    }
}
