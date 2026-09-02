using DDD.BuildingBlocks;
using DietApi.Features.Weight;
using DietApi.Features.Weight.DeleteWeightReading;
using DietApi.Features.Weight.GetWeightTrend;
using DietApi.Features.Weight.RecordWeight;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the weight use cases.
/// </summary>
public class WeightHandlerTests
{
    [Fact]
    public async Task The_trend_comes_back_for_the_signed_in_member()
    {
        var builder = DietPlanBuilder.APlan().Weighing(84.6m).TargetingWeight(78m);
        var plan = builder.Build();
        var handler = new GetWeightTrendHandler(
            FakeDietPlanRepository.Containing(plan), SignedInUser.WithId(builder.UserId));

        var trend = await handler.Handle(new GetWeightTrendQuery(null, null), CancellationToken.None);

        trend.ShouldNotBeNull();
        trend.CurrentWeightKg.ShouldBe(84.6m);
        trend.TargetWeightKg.ShouldBe(78m);
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = new GetWeightTrendHandler(FakeDietPlanRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetWeightTrendQuery(null, null), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Reading_the_trend_without_a_signed_in_member_is_refused()
    {
        var handler = new GetWeightTrendHandler(FakeDietPlanRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetWeightTrendQuery(null, null), CancellationToken.None));
    }

    [Fact]
    public async Task Recording_a_weight_persists_it_and_returns_the_updated_trend()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30).Weighing(84.6m);
        var plan = builder.Build();
        var repository = FakeDietPlanRepository.Containing(plan);
        var handler = new RecordWeightHandler(repository, SignedInUser.WithId(builder.UserId));

        var trend = await handler.Handle(
            new RecordWeightCommand(builder.DaysAgo(1), new RecordWeightRequest(83.2m)), CancellationToken.None);

        repository.SaveCount.ShouldBe(1);
        trend.Readings.Count.ShouldBe(2);
        trend.CurrentWeightKg.ShouldBe(84.6m);   // today's reading is still the newest
    }

    [Fact]
    public async Task Recording_twice_for_the_same_date_replaces_rather_than_duplicates()
    {
        var builder = DietPlanBuilder.APlan().Weighing(84.6m);
        var plan = builder.Build();
        var handler = new RecordWeightHandler(
            FakeDietPlanRepository.Containing(plan), SignedInUser.WithId(builder.UserId));

        await handler.Handle(
            new RecordWeightCommand(builder.Today, new RecordWeightRequest(83.2m)), CancellationToken.None);
        var trend = await handler.Handle(
            new RecordWeightCommand(builder.Today, new RecordWeightRequest(82.9m)), CancellationToken.None);

        trend.Readings.Count.ShouldBe(1);
        trend.CurrentWeightKg.ShouldBe(82.9m);
    }

    [Fact]
    public async Task Recording_a_future_weight_is_refused()
    {
        var builder = DietPlanBuilder.APlan();
        var plan = builder.Build();
        var handler = new RecordWeightHandler(
            FakeDietPlanRepository.Containing(plan), SignedInUser.WithId(builder.UserId));

        await Should.ThrowAsync<BusinessRuleValidationException>(
            handler.Handle(new RecordWeightCommand(builder.Today.AddDays(1), new RecordWeightRequest(83m)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Recording_an_implausible_weight_is_refused()
    {
        var builder = DietPlanBuilder.APlan();
        var plan = builder.Build();
        var handler = new RecordWeightHandler(
            FakeDietPlanRepository.Containing(plan), SignedInUser.WithId(builder.UserId));

        await Should.ThrowAsync<BusinessRuleValidationException>(
            handler.Handle(new RecordWeightCommand(builder.Today, new RecordWeightRequest(900m)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Recording_before_a_plan_exists_is_refused()
    {
        var handler = new RecordWeightHandler(FakeDietPlanRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        await Should.ThrowAsync<DomainException>(
            handler.Handle(new RecordWeightCommand(DateOnly.FromDateTime(DateTime.UtcNow), new RecordWeightRequest(83m)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Recording_without_a_signed_in_member_is_refused()
    {
        var handler = new RecordWeightHandler(FakeDietPlanRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new RecordWeightCommand(DateOnly.FromDateTime(DateTime.UtcNow), new RecordWeightRequest(83m)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Deleting_a_reading_works_while_others_remain()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30).WeighedDaysAgo(5, 85m);
        var plan = builder.Build();
        var repository = FakeDietPlanRepository.Containing(plan);
        var handler = new DeleteWeightReadingHandler(repository, SignedInUser.WithId(builder.UserId));

        (await handler.Handle(new DeleteWeightReadingCommand(builder.DaysAgo(5)), CancellationToken.None))
            .ShouldBeTrue();
        repository.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Deleting_a_date_with_no_reading_reports_not_found()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30);
        var plan = builder.Build();
        var handler = new DeleteWeightReadingHandler(
            FakeDietPlanRepository.Containing(plan), SignedInUser.WithId(builder.UserId));

        (await handler.Handle(new DeleteWeightReadingCommand(builder.DaysAgo(9)), CancellationToken.None))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Deleting_the_only_remaining_reading_is_refused()
    {
        // Current weight feeds the target suggestion, so it must always have a source.
        var builder = DietPlanBuilder.APlan();
        var plan = builder.Build();
        var handler = new DeleteWeightReadingHandler(
            FakeDietPlanRepository.Containing(plan), SignedInUser.WithId(builder.UserId));

        await Should.ThrowAsync<BusinessRuleValidationException>(
            handler.Handle(new DeleteWeightReadingCommand(builder.Today), CancellationToken.None));

        plan.WeightReadings.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Deleting_without_a_signed_in_member_is_refused()
    {
        var handler = new DeleteWeightReadingHandler(FakeDietPlanRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new DeleteWeightReadingCommand(DateOnly.FromDateTime(DateTime.UtcNow)),
                CancellationToken.None));
    }
}
