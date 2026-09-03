using DDD.BuildingBlocks;
using ActivityTypeAggregate = DietApi.Domain.Aggregates.ActivityType;
using DietPlanAggregate = DietApi.Domain.Aggregates.DietPlan;
using DietApi.Features.ExerciseShortcuts;
using DietApi.Features.ExerciseShortcuts.CreateShortcut;
using DietApi.Features.ExerciseShortcuts.GetShortcuts;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for reading and saving shortcuts.
/// </summary>
public class ExerciseShortcutHandlerTests
{
    [Fact]
    public async Task A_member_sees_their_shortcuts_in_order_with_the_activity_name_resolved()
    {
        var running = FakeActivityTypeRepository.Running();
        var walk = FakeActivityTypeRepository.BriskWalk();

        var builder = DietPlanBuilder.APlan()
            .WithShortcut(running.Id, 45, "Morning run")
            .WithShortcut(walk.Id, 30, "Dog walk");

        var plan = builder.Build();

        var response = await new GetShortcutsHandler(
                FakeDietPlanRepository.Containing(plan),
                Builder(running, walk),
                SignedInUser.WithId(builder.UserId))
            .Handle(new GetShortcutsQuery(), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Shortcuts.Select(s => s.Name).ShouldBe(["Morning run", "Dog walk"]);
        response.Shortcuts[0].ActivityName.ShouldBe(running.Name);
        response.Shortcuts.ShouldAllBe(s => s.Available);
        response.MaxShortcuts.ShouldBe(DietPlanAggregate.MaxShortcuts);
        response.RemainingSlots.ShouldBe(DietPlanAggregate.MaxShortcuts - 2);
    }

    [Fact]
    public async Task A_shortcut_whose_activity_has_left_the_catalogue_is_shown_as_unavailable()
    {
        var gone = FakeActivityTypeRepository.Running();
        var builder = DietPlanBuilder.APlan().WithShortcut(gone.Id, 45, "Morning run");
        var plan = builder.Build();

        var response = await new GetShortcutsHandler(
                FakeDietPlanRepository.Containing(plan),
                Builder(),
                SignedInUser.WithId(builder.UserId))
            .Handle(new GetShortcutsQuery(), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Shortcuts.Single().Available.ShouldBeFalse();
    }

    [Fact]
    public async Task Saving_derives_a_readable_name_when_none_is_given()
    {
        var running = FakeActivityTypeRepository.Running();
        var (plan, planRepo, userId) = APlan();

        var response = await CreateHandler(planRepo, userId, running)
            .Handle(
                new CreateShortcutCommand(new CreateShortcutRequest(running.Id, 45, null)),
                CancellationToken.None);

        response.ShouldNotBeNull();
        response.Shortcuts.Single().Name.ShouldBe("Running, 8 km/h, 45 min");
        planRepo.SaveCount.ShouldBe(1);
        plan.ExerciseShortcuts.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Saving_keeps_the_name_the_member_chose()
    {
        var running = FakeActivityTypeRepository.Running();
        var (_, planRepo, userId) = APlan();

        var response = await CreateHandler(planRepo, userId, running)
            .Handle(
                new CreateShortcutCommand(new CreateShortcutRequest(running.Id, 45, "Morning run")),
                CancellationToken.None);

        response.ShouldNotBeNull();
        response.Shortcuts.Single().Name.ShouldBe("Morning run");
    }

    [Fact]
    public async Task An_activity_that_is_not_in_the_catalogue_gives_null_so_the_endpoint_can_answer_404()
    {
        var (_, planRepo, userId) = APlan();

        var response = await CreateHandler(planRepo, userId)
            .Handle(
                new CreateShortcutCommand(new CreateShortcutRequest(Guid.NewGuid(), 45, null)),
                CancellationToken.None);

        response.ShouldBeNull();
    }

    [Fact]
    public async Task A_duplicate_is_refused_and_names_the_existing_shortcut()
    {
        var running = FakeActivityTypeRepository.Running();
        var builder = DietPlanBuilder.APlan().WithShortcut(running.Id, 45, "Morning run");
        var plan = builder.Build();
        var planRepo = FakeDietPlanRepository.Containing(plan);

        var error = await Should.ThrowAsync<DomainException>(
            CreateHandler(planRepo, builder.UserId, running)
                .Handle(
                    new CreateShortcutCommand(new CreateShortcutRequest(running.Id, 45, "Evening run")),
                    CancellationToken.None));

        error.Message.ShouldContain("Morning run");
        plan.ExerciseShortcuts.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Reaching_the_limit_is_refused_with_the_limit_stated()
    {
        var running = FakeActivityTypeRepository.Running();
        var (plan, planRepo, userId) = APlan();

        for (var i = 0; i < DietPlanAggregate.MaxShortcuts; i++)
        {
            plan.SaveExerciseShortcut(Guid.NewGuid(), 30 + i, $"Shortcut {i}");
        }

        var error = await Should.ThrowAsync<DomainException>(
            CreateHandler(planRepo, userId, running)
                .Handle(
                    new CreateShortcutCommand(new CreateShortcutRequest(running.Id, 45, null)),
                    CancellationToken.None));

        error.Message.ShouldContain(DietPlanAggregate.MaxShortcuts.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1441)]
    public async Task A_duration_a_session_would_refuse_is_refused_on_a_shortcut(int minutes)
    {
        // FR-005, through the handler rather than only at the aggregate.
        var running = FakeActivityTypeRepository.Running();
        var (_, planRepo, userId) = APlan();

        await Should.ThrowAsync<DomainException>(
            CreateHandler(planRepo, userId, running)
                .Handle(
                    new CreateShortcutCommand(new CreateShortcutRequest(running.Id, minutes, null)),
                    CancellationToken.None));
    }

    [Fact]
    public async Task A_shortcut_can_be_created_for_an_activity_never_logged_before()
    {
        // US3: no ordering constraint between logging something and saving a shortcut for it.
        var butterfly = FakeActivityTypeRepository.Butterfly();
        var (_, planRepo, userId) = APlan();

        var response = await CreateHandler(planRepo, userId, butterfly)
            .Handle(
                new CreateShortcutCommand(new CreateShortcutRequest(butterfly.Id, 30, "Tuesday swim")),
                CancellationToken.None);

        response.ShouldNotBeNull();
        response.Shortcuts.Single().ActivityName.ShouldBe(butterfly.Name);
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = new GetShortcutsHandler(
            FakeDietPlanRepository.Empty(), Builder(), SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetShortcutsQuery(), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Another_members_shortcuts_are_not_returned()
    {
        var running = FakeActivityTypeRepository.Running();
        var theirs = DietPlanBuilder.APlan().WithShortcut(running.Id, 45, "Theirs").Build();

        var handler = new GetShortcutsHandler(
            FakeDietPlanRepository.Containing(theirs), Builder(running), SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetShortcutsQuery(), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Asking_without_a_signed_in_member_is_refused()
    {
        var handler = new GetShortcutsHandler(
            FakeDietPlanRepository.Empty(), Builder(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetShortcutsQuery(), CancellationToken.None));
    }

    // --- Helpers ----------------------------------------------------------

    internal static ShortcutListBuilder Builder(params ActivityTypeAggregate[] activities) =>
        new(FakeActivityTypeRepository.Containing(activities));

    private static CreateShortcutHandler CreateHandler(
        FakeDietPlanRepository planRepo, Guid userId, params ActivityTypeAggregate[] activities) =>
        new(planRepo,
            FakeActivityTypeRepository.Containing(activities),
            Builder(activities),
            SignedInUser.WithId(userId));

    internal static (DietPlanAggregate Plan, FakeDietPlanRepository Repo, Guid UserId) APlan()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30).Weighing(70m);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
