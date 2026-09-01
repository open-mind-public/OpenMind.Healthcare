using DDD.BuildingBlocks;
using QuitSmokingApi.Domain.Entities;
using QuitSmokingApi.Features.SmokedDays.MarkDayAsSmoked;
using QuitSmokingApi.Features.SmokedDays.UnmarkSmokedDay;
using QuitSmokingApi.Tests.TestSupport;

namespace QuitSmokingApi.Tests.Features;

public class MarkDayAsSmokedHandlerTests
{
    [Fact]
    public async Task Marking_a_day_records_it_against_the_signed_in_users_journey()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30).Smoking(20, 20, 10m);
        var journey = builder.Build();
        var repository = FakeQuitJourneyRepository.Containing(journey);
        var handler = new MarkDayAsSmokedHandler(repository, SignedInUser.WithId(journey.UserId));

        var result = await handler.Handle(
            new MarkDayAsSmokedCommand(builder.DaysAgo(3), 6, RelapseTrigger.Stress, "rough day"),
            CancellationToken.None);

        result.Date.ShouldBe(builder.DaysAgo(3));
        result.CigarettesSmoked.ShouldBe(6);
        result.Trigger.ShouldBe(nameof(RelapseTrigger.Stress));
        result.Note.ShouldBe("rough day");
        result.MoneySpent.ShouldBe(3m); // six cigarettes at 50c
        result.Currency.ShouldBe("USD");

        repository.StoredFor(journey.UserId)!.IsSmokedDay(builder.DaysAgo(3)).ShouldBeTrue();
        repository.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Marking_a_day_a_second_time_updates_the_one_record_that_is_stored()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30).SmokedDaysAgo(3, cigarettes: 2);
        var journey = builder.Build();
        var repository = FakeQuitJourneyRepository.Containing(journey);
        var handler = new MarkDayAsSmokedHandler(repository, SignedInUser.WithId(journey.UserId));

        var result = await handler.Handle(
            new MarkDayAsSmokedCommand(builder.DaysAgo(3), 11, RelapseTrigger.Social, null),
            CancellationToken.None);

        result.CigarettesSmoked.ShouldBe(11);
        repository.StoredFor(journey.UserId)!.SmokedDays.Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_user_who_has_not_started_a_journey_is_told_to_set_one_up_first()
    {
        var handler = new MarkDayAsSmokedHandler(FakeQuitJourneyRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        var handle = async () => await handler.Handle(
            new MarkDayAsSmokedCommand(DateOnly.FromDateTime(DateTime.UtcNow), 1, RelapseTrigger.Unspecified, null),
            CancellationToken.None);

        var exception = await handle.ShouldThrowAsync<DomainException>();
        exception.Message.ShouldBe("Start your quit journey before marking a day as smoked");
    }

    [Fact]
    public async Task A_caller_without_an_identity_is_refused()
    {
        var journey = JourneyBuilder.AJourney().Build();
        var handler = new MarkDayAsSmokedHandler(FakeQuitJourneyRepository.Containing(journey), SignedInUser.Anonymous());

        var handle = async () => await handler.Handle(
            new MarkDayAsSmokedCommand(DateOnly.FromDateTime(DateTime.UtcNow), 1, RelapseTrigger.Unspecified, null),
            CancellationToken.None);

        await handle.ShouldThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task One_user_cannot_mark_a_day_on_another_users_journey()
    {
        var journey = JourneyBuilder.AJourney().Build();
        var repository = FakeQuitJourneyRepository.Containing(journey);
        var handler = new MarkDayAsSmokedHandler(repository, SignedInUser.WithId(Guid.NewGuid()));

        var handle = async () => await handler.Handle(
            new MarkDayAsSmokedCommand(DateOnly.FromDateTime(DateTime.UtcNow), 1, RelapseTrigger.Unspecified, null),
            CancellationToken.None);

        await handle.ShouldThrowAsync<DomainException>();
        journey.SmokedDays.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_day_the_rules_reject_is_never_stored()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30);
        var journey = builder.Build();
        var repository = FakeQuitJourneyRepository.Containing(journey);
        var handler = new MarkDayAsSmokedHandler(repository, SignedInUser.WithId(journey.UserId));

        var handle = async () => await handler.Handle(
            new MarkDayAsSmokedCommand(builder.Today.AddDays(2), 1, RelapseTrigger.Unspecified, null),
            CancellationToken.None);

        await handle.ShouldThrowAsync<BusinessRuleValidationException>();
        repository.SaveCount.ShouldBe(0);
        journey.SmokedDays.ShouldBeEmpty();
    }
}

public class UnmarkSmokedDayHandlerTests
{
    [Fact]
    public async Task Unmarking_a_day_removes_it_and_saves_the_journey()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30).SmokedDaysAgo(3);
        var journey = builder.Build();
        var repository = FakeQuitJourneyRepository.Containing(journey);
        var handler = new UnmarkSmokedDayHandler(repository, SignedInUser.WithId(journey.UserId));

        var removed = await handler.Handle(new UnmarkSmokedDayCommand(builder.DaysAgo(3)), CancellationToken.None);

        removed.ShouldBeTrue();
        repository.StoredFor(journey.UserId)!.IsSmokedDay(builder.DaysAgo(3)).ShouldBeFalse();
        repository.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Unmarking_a_day_that_was_never_marked_reports_nothing_removed_and_saves_nothing()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30).SmokedDaysAgo(3);
        var journey = builder.Build();
        var repository = FakeQuitJourneyRepository.Containing(journey);
        var handler = new UnmarkSmokedDayHandler(repository, SignedInUser.WithId(journey.UserId));

        var removed = await handler.Handle(new UnmarkSmokedDayCommand(builder.DaysAgo(9)), CancellationToken.None);

        removed.ShouldBeFalse();
        repository.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task There_is_nothing_to_unmark_when_the_user_has_no_journey()
    {
        var handler = new UnmarkSmokedDayHandler(FakeQuitJourneyRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        var removed = await handler.Handle(
            new UnmarkSmokedDayCommand(DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        removed.ShouldBeFalse();
    }

    [Fact]
    public async Task A_caller_without_an_identity_is_refused()
    {
        var handler = new UnmarkSmokedDayHandler(FakeQuitJourneyRepository.Empty(), SignedInUser.Anonymous());

        var handle = async () => await handler.Handle(
            new UnmarkSmokedDayCommand(DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        await handle.ShouldThrowAsync<UnauthorizedAccessException>();
    }
}
