using DDD.BuildingBlocks;
using QuitSmokingApi.Features.Progress.CreateOrUpdateProgress;
using QuitSmokingApi.Features.Progress.GetProgress;
using QuitSmokingApi.Features.Progress.GetStats;
using QuitSmokingApi.Tests.TestSupport;

namespace QuitSmokingApi.Tests.Features;

public class CreateOrUpdateProgressHandlerTests
{
    [Fact]
    public async Task A_first_time_user_gets_a_new_journey()
    {
        var userId = Guid.NewGuid();
        var repository = FakeQuitJourneyRepository.Empty();
        var handler = new CreateOrUpdateProgressHandler(repository, SignedInUser.WithId(userId));
        var quitDate = DateTime.UtcNow.AddDays(-2);

        var journey = await handler.Handle(
            new CreateOrUpdateProgressCommand(quitDate, CigarettesPerDay: 20, PricePerPack: 10m, CigarettesPerPack: 20),
            CancellationToken.None);

        journey.UserId.ShouldBe(userId);
        journey.QuitDate.ShouldBe(quitDate);
        repository.StoredFor(userId).ShouldNotBeNull();
        repository.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Saving_again_updates_the_existing_journey_rather_than_starting_a_second()
    {
        var existing = JourneyBuilder.AJourney().StartedDaysAgo(40).Smoking(20, 20, 10m).Build();
        var repository = FakeQuitJourneyRepository.Containing(existing);
        var handler = new CreateOrUpdateProgressHandler(repository, SignedInUser.WithId(existing.UserId));
        var newQuitDate = DateTime.UtcNow.AddDays(-5);

        var journey = await handler.Handle(
            new CreateOrUpdateProgressCommand(newQuitDate, CigarettesPerDay: 30, PricePerPack: 15m, CigarettesPerPack: 25, Currency: "VND"),
            CancellationToken.None);

        journey.Id.ShouldBe(existing.Id);
        journey.QuitDate.ShouldBe(newQuitDate);
        journey.SmokingHabits.CigarettesPerDay.ShouldBe(30);
        journey.SmokingHabits.PricePerPack.Currency.ShouldBe("VND");
        repository.StoredFor(existing.UserId)!.Id.ShouldBe(existing.Id);
    }

    [Fact]
    public async Task Setting_up_a_journey_with_impossible_habits_is_refused_and_saves_nothing()
    {
        var repository = FakeQuitJourneyRepository.Empty();
        var handler = new CreateOrUpdateProgressHandler(repository, SignedInUser.WithId(Guid.NewGuid()));

        var handle = async () => await handler.Handle(
            new CreateOrUpdateProgressCommand(DateTime.UtcNow.AddDays(-1), CigarettesPerDay: 0, PricePerPack: 10m, CigarettesPerPack: 20),
            CancellationToken.None);

        await handle.ShouldThrowAsync<BusinessRuleValidationException>();
        repository.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_caller_without_an_identity_is_refused()
    {
        var handler = new CreateOrUpdateProgressHandler(FakeQuitJourneyRepository.Empty(), SignedInUser.Anonymous());

        var handle = async () => await handler.Handle(
            new CreateOrUpdateProgressCommand(DateTime.UtcNow.AddDays(-1), 20, 10m, 20),
            CancellationToken.None);

        await handle.ShouldThrowAsync<UnauthorizedAccessException>();
    }
}

public class GetProgressHandlerTests
{
    [Fact]
    public async Task The_signed_in_users_journey_comes_back()
    {
        var journey = JourneyBuilder.AJourney().Build();
        var handler = new GetProgressHandler(FakeQuitJourneyRepository.Containing(journey), SignedInUser.WithId(journey.UserId));

        var found = await handler.Handle(new GetProgressQuery(), CancellationToken.None);

        found!.Id.ShouldBe(journey.Id);
    }

    [Fact]
    public async Task Nothing_comes_back_for_a_user_who_has_not_set_up_a_journey()
    {
        var handler = new GetProgressHandler(FakeQuitJourneyRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetProgressQuery(), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task One_users_journey_is_not_visible_to_another()
    {
        var journey = JourneyBuilder.AJourney().Build();
        var handler = new GetProgressHandler(FakeQuitJourneyRepository.Containing(journey), SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetProgressQuery(), CancellationToken.None)).ShouldBeNull();
    }
}

public class GetStatsHandlerTests
{
    [Fact]
    public async Task A_user_with_no_journey_sees_a_blank_slate_rather_than_an_error()
    {
        var handler = new GetStatsHandler(FakeQuitJourneyRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        var stats = await handler.Handle(new GetStatsQuery(), CancellationToken.None);

        stats.DaysSmokeFree.ShouldBe(0);
        stats.CigarettesAvoided.ShouldBe(0);
        stats.MoneySaved.Amount.ShouldBe(0m);
        stats.SmokedDays.ShouldBe(0);
        stats.CurrentStreak.ShouldBe(0);
        stats.CurrentMilestone.RequiredDays.ShouldBe(0);
        stats.NextMilestone!.RequiredDays.ShouldBe(1);
        stats.DaysToNextMilestone.ShouldBe(1);
    }

    [Fact]
    public async Task Stats_leave_out_the_days_the_user_marked_as_smoked()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(70)
            .Smoking(20, 20, 10m)
            .SmokedDaysAgo(40, cigarettes: 8)
            .SmokedDaysAgo(12, cigarettes: 3)
            .SmokedDaysAgo(5, cigarettes: 9);
        var journey = builder.Build();
        var handler = new GetStatsHandler(FakeQuitJourneyRepository.Containing(journey), SignedInUser.WithId(journey.UserId));

        var stats = await handler.Handle(new GetStatsQuery(), CancellationToken.None);

        stats.TotalDaysInJourney.ShouldBe(70);
        stats.SmokedDays.ShouldBe(3);
        stats.DaysSmokeFree.ShouldBe(67);
        stats.CigarettesAvoided.ShouldBe(1340);
        stats.MoneySaved.Amount.ShouldBe(670m);
        stats.CigarettesSmoked.ShouldBe(20);
        stats.MoneySpentOnRelapses.Amount.ShouldBe(10m);
        stats.CurrentStreak.ShouldBe(5);
        stats.LongestStreak.ShouldBe(30);
        stats.SmokeFreeRate.ShouldBe(95.71);
    }

    [Fact]
    public async Task A_caller_without_an_identity_is_refused()
    {
        var handler = new GetStatsHandler(FakeQuitJourneyRepository.Empty(), SignedInUser.Anonymous());

        var handle = async () => await handler.Handle(new GetStatsQuery(), CancellationToken.None);

        await handle.ShouldThrowAsync<UnauthorizedAccessException>();
    }
}
