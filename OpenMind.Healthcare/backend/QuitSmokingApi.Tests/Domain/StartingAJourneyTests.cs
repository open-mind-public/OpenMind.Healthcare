using DDD.BuildingBlocks;
using QuitSmokingApi.Domain.Aggregates;
using QuitSmokingApi.Tests.TestSupport;

namespace QuitSmokingApi.Tests.Domain;

public class StartingAJourneyTests
{
    [Fact]
    public void A_journey_records_the_quit_date_and_the_habits_being_given_up()
    {
        var userId = Guid.NewGuid();
        var quitDate = DateTime.UtcNow.AddDays(-3);

        var journey = QuitJourney.Start(userId, quitDate, cigarettesPerDay: 15, cigarettesPerPack: 20, pricePerPack: 12.5m, currency: "USD");

        journey.UserId.ShouldBe(userId);
        journey.QuitDate.ShouldBe(quitDate);
        journey.SmokingHabits.CigarettesPerDay.ShouldBe(15);
        journey.SmokingHabits.CigarettesPerPack.ShouldBe(20);
        journey.SmokingHabits.PricePerPack.Amount.ShouldBe(12.5m);
        journey.SmokingHabits.PricePerPack.Currency.ShouldBe("USD");
        journey.SmokedDays.ShouldBeEmpty();
    }

    [Fact]
    public void The_quit_day_is_the_calendar_day_the_quit_date_falls_on()
    {
        var quitDate = new DateTime(2026, 6, 23, 22, 45, 0, DateTimeKind.Utc);

        var journey = QuitJourney.Start(Guid.NewGuid(), quitDate, 20, 20, 10m);

        journey.QuitDay.ShouldBe(new DateOnly(2026, 6, 23));
    }

    [Fact]
    public void A_journey_cannot_start_in_the_future()
    {
        var start = () => QuitJourney.Start(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), 20, 20, 10m);

        start.ShouldThrow<BusinessRuleValidationException>()
            .Message.ShouldBe("Quit date cannot be in the future");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_journey_needs_a_positive_number_of_cigarettes_per_day(int cigarettesPerDay)
    {
        var start = () => QuitJourney.Start(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), cigarettesPerDay, 20, 10m);

        start.ShouldThrow<BusinessRuleValidationException>()
            .Message.ShouldBe("Cigarettes per day must be greater than zero");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void A_journey_needs_a_positive_pack_price(decimal pricePerPack)
    {
        var start = () => QuitJourney.Start(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), 20, 20, pricePerPack);

        start.ShouldThrow<BusinessRuleValidationException>()
            .Message.ShouldBe("Price per pack must be greater than zero");
    }

    [Fact]
    public void A_journey_must_belong_to_a_user()
    {
        var start = () => QuitJourney.Start(Guid.Empty, DateTime.UtcNow.AddDays(-1), 20, 20, 10m);

        start.ShouldThrow<DomainException>()
            .Message.ShouldBe("User ID cannot be empty");
    }

    [Fact]
    public void Updating_a_journey_replaces_the_quit_date_and_the_habits()
    {
        var journey = JourneyBuilder.AJourney().StartedDaysAgo(40).Smoking(20, 20, 10m).Build();
        var newQuitDate = DateTime.UtcNow.AddDays(-10);

        journey.Update(newQuitDate, cigarettesPerDay: 30, cigarettesPerPack: 25, pricePerPack: 15m, currency: "VND");

        journey.QuitDate.ShouldBe(newQuitDate);
        journey.SmokingHabits.CigarettesPerDay.ShouldBe(30);
        journey.SmokingHabits.CigarettesPerPack.ShouldBe(25);
        journey.SmokingHabits.PricePerPack.Amount.ShouldBe(15m);
        journey.SmokingHabits.PricePerPack.Currency.ShouldBe("VND");
    }

    [Fact]
    public void A_journey_cannot_be_moved_into_the_future()
    {
        var journey = JourneyBuilder.AJourney().Build();

        var update = () => journey.Update(DateTime.UtcNow.AddDays(1), 20, 20, 10m);

        update.ShouldThrow<BusinessRuleValidationException>()
            .Message.ShouldBe("Quit date cannot be in the future");
    }

    [Fact]
    public void Moving_the_quit_date_forward_drops_smoked_days_that_now_sit_before_it()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(60)
            .SmokedDaysAgo(50)
            .SmokedDaysAgo(30)
            .SmokedDaysAgo(5);
        var journey = builder.Build();

        journey.Update(builder.Clock.AddDays(-31), 20, 20, 10m);

        journey.SmokedDays.Select(d => d.Date)
            .ShouldBe([builder.DaysAgo(30), builder.DaysAgo(5)], ignoreOrder: true);
    }

    [Fact]
    public void Moving_the_quit_date_keeps_a_smoked_day_that_falls_on_the_new_quit_day()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(60).SmokedDaysAgo(30);
        var journey = builder.Build();

        journey.Update(builder.Clock.AddDays(-30), 20, 20, 10m);

        journey.IsSmokedDay(builder.DaysAgo(30)).ShouldBeTrue();
    }
}
