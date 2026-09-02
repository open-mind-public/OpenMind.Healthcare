using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietGuidance.GetDailyEncouragement;
using DietApi.Features.DietGuidance.GetEatingTips;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the guidance use cases.
/// </summary>
public class DietGuidanceHandlerTests
{
    private readonly StreakCalculator _streaks = new();

    [Fact]
    public async Task Tips_come_back_from_the_curated_library()
    {
        var handler = new GetEatingTipsHandler(FakeEatingTipRepository.WithSampleTips());

        var tips = await handler.Handle(new GetEatingTipsQuery(null), CancellationToken.None);

        tips.Count.ShouldBe(3);
        tips.ShouldContain(t => t.Title == "Wait ten minutes");
    }

    [Fact]
    public async Task Tips_can_be_filtered_by_category()
    {
        var handler = new GetEatingTipsHandler(FakeEatingTipRepository.WithSampleTips());

        var tips = await handler.Handle(new GetEatingTipsQuery(TipCategory.Craving), CancellationToken.None);

        tips.Count.ShouldBe(1);
        tips.Single().Category.ShouldBe(TipCategory.Craving);
    }

    [Fact]
    public async Task A_member_on_a_streak_gets_a_message_that_says_so()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30);
        var plan = builder.Build();

        var days = Enumerable.Range(0, 5)
            .Select(i => LoggedDayBuilder.ADay()
                .ForUser(builder.UserId).ForPlan(plan.Id)
                .PlanStartedDaysAgo(30).DaysAgo(i)
                .Targeting(2100)
                .Ate(FakeFoodLibraryRepository.Oats())
                .Build())
            .ToArray();

        var handler = new GetDailyEncouragementHandler(
            FakeDietPlanRepository.Containing(plan), FakeLoggedDayRepository.Containing(days),
            _streaks, SignedInUser.WithId(builder.UserId));

        var result = await handler.Handle(new GetDailyEncouragementQuery(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.CurrentStreakDays.ShouldBe(5);
        result.Message.ShouldContain("5");
        result.Tone.ShouldBe("Streak");
    }

    [Fact]
    public async Task A_member_with_nothing_logged_gets_a_getting_started_message_not_an_error()
    {
        var builder = DietPlanBuilder.APlan();
        var plan = builder.Build();
        var handler = new GetDailyEncouragementHandler(
            FakeDietPlanRepository.Containing(plan), FakeLoggedDayRepository.Empty(),
            _streaks, SignedInUser.WithId(builder.UserId));

        var result = await handler.Handle(new GetDailyEncouragementQuery(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Tone.ShouldBe("GettingStarted");
        result.CurrentStreakDays.ShouldBe(0);
    }

    [Fact]
    public async Task A_member_whose_streak_lapsed_is_invited_to_start_again()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30);
        var plan = builder.Build();

        // Logged three days ago, nothing since.
        var day = LoggedDayBuilder.ADay()
            .ForUser(builder.UserId).ForPlan(plan.Id)
            .PlanStartedDaysAgo(30).DaysAgo(3)
            .Targeting(2100)
            .Ate(FakeFoodLibraryRepository.Oats())
            .Build();

        var handler = new GetDailyEncouragementHandler(
            FakeDietPlanRepository.Containing(plan), FakeLoggedDayRepository.Containing(day),
            _streaks, SignedInUser.WithId(builder.UserId));

        var result = await handler.Handle(new GetDailyEncouragementQuery(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Tone.ShouldBe("Restart");
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = new GetDailyEncouragementHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(),
            _streaks, SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetDailyEncouragementQuery(), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Encouragement_without_a_signed_in_member_is_refused()
    {
        var handler = new GetDailyEncouragementHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(),
            _streaks, SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetDailyEncouragementQuery(), CancellationToken.None));
    }
}
