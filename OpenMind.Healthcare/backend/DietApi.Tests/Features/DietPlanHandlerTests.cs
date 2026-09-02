using DDD.BuildingBlocks;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietPlan;
using DietApi.Features.DietPlan.CreateDietPlan;
using DietApi.Features.DietPlan.GetDietPlan;
using DietApi.Features.DietPlan.SetTargets;
using DietApi.Features.DietPlan.SuggestTargets;
using DietApi.Features.DietPlan.UpdateDietPlan;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the plan use cases: the success path and the unauthenticated path for each
/// handler that resolves a member, plus the paths a member can actually reach by accident.
/// </summary>
public class DietPlanHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private readonly TargetSuggestionService _suggestions = new();

    // --- Suggest targets --------------------------------------------------
    // This handler resolves no member - a suggestion is a pure calculation, and the endpoint
    // group's RequireAuthorization is what keeps it behind a sign-in.

    [Fact]
    public async Task Suggesting_targets_returns_the_calculation_and_its_disclaimer()
    {
        var handler = new SuggestTargetsHandler(_suggestions);

        var result = await handler.Handle(
            new SuggestTargetsQuery(new SuggestTargetsRequest(
                GoalType.LoseWeight,
                new BodyMetricsDto(178m, 34, BiologicalSex.Male),
                84.6m,
                ActivityLevel.ModeratelyActive)),
            CancellationToken.None);

        result.SuggestedTargets.Calories.ShouldBe(2281);
        result.RestingEnergyKcal.ShouldBe(1794);
        result.Disclaimer.ShouldBe(TargetSuggestion.Disclaimer);
    }

    // --- Create -----------------------------------------------------------

    [Fact]
    public async Task Creating_a_plan_persists_it_for_the_signed_in_member()
    {
        var userId = Guid.NewGuid();
        var repository = FakeDietPlanRepository.Empty();
        var handler = new CreateDietPlanHandler(repository, SignedInUser.WithId(userId));

        var result = await handler.Handle(new CreateDietPlanCommand(Request()), CancellationToken.None);

        repository.SaveCount.ShouldBe(1);
        repository.StoredFor(userId).ShouldNotBeNull();
        result.Plan.Targets.Calories.ShouldBe(2100);
        result.Plan.CurrentWeightKg.ShouldBe(84.6m);
        result.BelowSafeFloorWarning.ShouldBeNull();
    }

    [Fact]
    public async Task Creating_a_plan_without_a_signed_in_member_is_refused()
    {
        var handler = new CreateDietPlanHandler(FakeDietPlanRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new CreateDietPlanCommand(Request()), CancellationToken.None));
    }

    [Fact]
    public async Task A_member_cannot_create_a_second_plan()
    {
        var builder = DietPlanBuilder.APlan();
        var plan = builder.Build();
        var handler = new CreateDietPlanHandler(
            FakeDietPlanRepository.Containing(plan), SignedInUser.WithId(builder.UserId));

        await Should.ThrowAsync<DomainException>(
            handler.Handle(new CreateDietPlanCommand(Request()), CancellationToken.None));
    }

    [Fact]
    public async Task A_target_below_the_floor_saves_and_warns_rather_than_being_blocked()
    {
        var repository = FakeDietPlanRepository.Empty();
        var handler = new CreateDietPlanHandler(repository, SignedInUser.WithId(Guid.NewGuid()));

        var result = await handler.Handle(
            new CreateDietPlanCommand(Request(calories: 900, source: TargetSource.MemberSet)),
            CancellationToken.None);

        // Saved, not refused.
        repository.SaveCount.ShouldBe(1);
        result.Plan.Targets.Calories.ShouldBe(900);
        result.BelowSafeFloorWarning.ShouldNotBeNull();
        result.BelowSafeFloorWarning.ShouldContain("1500");
    }

    [Fact]
    public async Task A_suggested_target_never_carries_a_floor_warning()
    {
        var handler = new CreateDietPlanHandler(FakeDietPlanRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        var result = await handler.Handle(
            new CreateDietPlanCommand(Request(calories: 1400, source: TargetSource.Suggested)),
            CancellationToken.None);

        result.BelowSafeFloorWarning.ShouldBeNull();
    }

    // --- Get --------------------------------------------------------------

    [Fact]
    public async Task Fetching_a_plan_returns_the_signed_in_member_s_own()
    {
        var builder = DietPlanBuilder.APlan();
        var plan = builder.Build();
        var handler = new GetDietPlanHandler(
            FakeDietPlanRepository.Containing(plan), SignedInUser.WithId(builder.UserId));

        var result = await handler.Handle(new GetDietPlanQuery(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(plan.Id);
    }

    [Fact]
    public async Task Fetching_a_plan_that_does_not_exist_returns_null_so_the_endpoint_can_answer_404()
    {
        var handler = new GetDietPlanHandler(FakeDietPlanRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetDietPlanQuery(), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Another_member_s_plan_is_not_visible()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var handler = new GetDietPlanHandler(
            FakeDietPlanRepository.Containing(plan), SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetDietPlanQuery(), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Fetching_a_plan_without_a_signed_in_member_is_refused()
    {
        var handler = new GetDietPlanHandler(FakeDietPlanRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetDietPlanQuery(), CancellationToken.None));
    }

    // --- Update -----------------------------------------------------------

    [Fact]
    public async Task Updating_a_plan_offers_a_refreshed_suggestion_and_leaves_the_target_alone()
    {
        var builder = DietPlanBuilder.APlan().WithTargets(2100, TargetSource.MemberSet);
        var plan = builder.Build();
        var handler = new UpdateDietPlanHandler(
            FakeDietPlanRepository.Containing(plan), _suggestions, SignedInUser.WithId(builder.UserId));

        var result = await handler.Handle(
            new UpdateDietPlanCommand(new UpdateDietPlanRequest(
                GoalType.Maintain,
                Today.AddDays(-10),
                new BodyMetricsDto(180m, 35, BiologicalSex.Male),
                ActivityLevel.VeryActive,
                80m)),
            CancellationToken.None);

        result.TargetsUnchanged.ShouldBeTrue();
        result.Plan.Targets.Calories.ShouldBe(2100);
        result.Plan.TargetSource.ShouldBe(TargetSource.MemberSet);
        result.Plan.ActivityLevel.ShouldBe(ActivityLevel.VeryActive);

        // The refreshed suggestion reflects the new details without being applied.
        result.RefreshedSuggestion.SuggestedTargets.Calories.ShouldNotBe(2100);
    }

    [Fact]
    public async Task Updating_a_plan_that_does_not_exist_is_refused()
    {
        var handler = new UpdateDietPlanHandler(
            FakeDietPlanRepository.Empty(), _suggestions, SignedInUser.WithId(Guid.NewGuid()));

        await Should.ThrowAsync<DomainException>(
            handler.Handle(new UpdateDietPlanCommand(new UpdateDietPlanRequest(
                GoalType.Maintain, Today, new BodyMetricsDto(178m, 34, BiologicalSex.Male),
                ActivityLevel.Sedentary, null)), CancellationToken.None));
    }

    [Fact]
    public async Task Updating_a_plan_without_a_signed_in_member_is_refused()
    {
        var handler = new UpdateDietPlanHandler(
            FakeDietPlanRepository.Empty(), _suggestions, SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new UpdateDietPlanCommand(new UpdateDietPlanRequest(
                GoalType.Maintain, Today, new BodyMetricsDto(178m, 34, BiologicalSex.Male),
                ActivityLevel.Sedentary, null)), CancellationToken.None));
    }

    // --- Set targets ------------------------------------------------------

    [Fact]
    public async Task Setting_targets_records_the_new_value_and_its_source()
    {
        var builder = DietPlanBuilder.APlan().WithTargets(2100);
        var plan = builder.Build();
        var repository = FakeDietPlanRepository.Containing(plan);
        var handler = new SetDietTargetsHandler(repository, SignedInUser.WithId(builder.UserId));

        var result = await handler.Handle(
            new SetDietTargetsCommand(new SetTargetsRequest(
                new NutritionTargetsDto(1800, null, null, null), TargetSource.MemberSet)),
            CancellationToken.None);

        repository.SaveCount.ShouldBe(1);
        result.Plan.Targets.Calories.ShouldBe(1800);
        result.Plan.TargetSource.ShouldBe(TargetSource.MemberSet);
    }

    [Fact]
    public async Task Setting_targets_without_a_signed_in_member_is_refused()
    {
        var handler = new SetDietTargetsHandler(FakeDietPlanRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new SetDietTargetsCommand(new SetTargetsRequest(
                new NutritionTargetsDto(1800, null, null, null), TargetSource.MemberSet)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Setting_targets_before_a_plan_exists_is_refused()
    {
        var handler = new SetDietTargetsHandler(FakeDietPlanRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        await Should.ThrowAsync<DomainException>(
            handler.Handle(new SetDietTargetsCommand(new SetTargetsRequest(
                new NutritionTargetsDto(1800, null, null, null), TargetSource.MemberSet)),
                CancellationToken.None));
    }

    private static CreateDietPlanRequest Request(
        int calories = 2100,
        TargetSource source = TargetSource.Suggested) =>
        new(GoalType.LoseWeight,
            Today.AddDays(-30),
            new BodyMetricsDto(178m, 34, BiologicalSex.Male),
            ActivityLevel.ModeratelyActive,
            84.6m,
            78m,
            new NutritionTargetsDto(calories, null, null, null),
            source);
}
