using System.Globalization;
using DDD.BuildingBlocks;
using QuitSmokingApi.Domain.ValueObjects;

namespace QuitSmokingApi.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void An_amount_is_kept_to_two_decimal_places()
    {
        Money.Create(2.567m).Amount.ShouldBe(2.57m);
    }

    [Fact]
    public void Money_cannot_be_created_from_a_negative_amount()
    {
        var create = () => Money.Create(-1m);

        create.ShouldThrow<DomainException>().Message.ShouldBe("Money amount cannot be negative");
    }

    [Fact]
    public void Zero_defaults_to_dollars_but_can_be_asked_for_in_another_currency()
    {
        Money.Zero().Amount.ShouldBe(0m);
        Money.Zero().Currency.ShouldBe("USD");
        Money.Zero("VND").Currency.ShouldBe("VND");
    }

    [Fact]
    public void Amounts_in_the_same_currency_add_up()
    {
        Money.Create(4.50m).Add(Money.Create(2.25m)).Amount.ShouldBe(6.75m);
    }

    [Fact]
    public void Amounts_in_the_same_currency_can_be_taken_away()
    {
        Money.Create(9m).Subtract(Money.Create(2.5m)).Amount.ShouldBe(6.5m);
    }

    [Fact]
    public void Amounts_in_different_currencies_cannot_be_added()
    {
        var add = () => Money.Create(5m, "USD").Add(Money.Create(5m, "VND"));

        add.ShouldThrow<DomainException>().Message.ShouldBe("Cannot add money with different currencies");
    }

    [Fact]
    public void Amounts_in_different_currencies_cannot_be_subtracted()
    {
        var subtract = () => Money.Create(5m, "USD").Subtract(Money.Create(1m, "VND"));

        subtract.ShouldThrow<DomainException>().Message.ShouldBe("Cannot subtract money with different currencies");
    }

    [Fact]
    public void Multiplying_keeps_the_currency_and_rounds_the_result()
    {
        var total = Money.Create(0.5m, "VND").Multiply(1340);

        total.Amount.ShouldBe(670m);
        total.Currency.ShouldBe("VND");
    }

    [Theory]
    [InlineData("USD", 12.5, "$12.50")]
    [InlineData("VND", 40000, "40,000 ₫")]
    public void Money_prints_in_the_style_of_its_currency(string currency, decimal amount, string expected)
    {
        // Formatting follows the ambient culture, so pin one to keep the expectation meaningful
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            Money.Create(amount, currency).ToString().ShouldBe(expected);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData("USD", "$")]
    [InlineData("VND", "₫")]
    [InlineData("EUR", "$")]
    public void Every_currency_reports_a_symbol_with_dollars_as_the_fallback(string currency, string expected)
    {
        Money.Create(1m, currency).GetSymbol().ShouldBe(expected);
    }

    [Fact]
    public void Two_amounts_are_the_same_money_when_the_value_and_currency_match()
    {
        Money.Create(10m, "USD").ShouldBe(Money.Create(10m, "USD"));
        Money.Create(10m, "USD").ShouldNotBe(Money.Create(10m, "VND"));
        Money.Create(10m, "USD").ShouldNotBe(Money.Create(11m, "USD"));
    }
}

public class DurationTests
{
    [Fact]
    public void A_duration_cannot_be_negative()
    {
        var create = () => Duration.FromMinutes(-1);

        create.ShouldThrow<DomainException>().Message.ShouldBe("Duration cannot be negative");
    }

    [Fact]
    public void A_duration_breaks_down_into_days_hours_and_minutes()
    {
        var duration = Duration.FromMinutes(1_505); // one day, one hour and five minutes

        duration.TotalMinutes.ShouldBe(1_505);
        duration.Days.ShouldBe(1);
        duration.Hours.ShouldBe(25);      // total hours, not hours within the day
        duration.HoursWithinDay.ShouldBe(1);
        duration.Minutes.ShouldBe(5);
    }

    [Fact]
    public void Durations_can_be_built_from_hours_days_or_a_timespan()
    {
        Duration.FromHours(2).TotalMinutes.ShouldBe(120);
        Duration.FromDays(3).TotalMinutes.ShouldBe(4_320);
        Duration.FromTimeSpan(TimeSpan.FromMinutes(90)).TotalMinutes.ShouldBe(90);
        Duration.Zero.TotalMinutes.ShouldBe(0);
    }

    [Theory]
    [InlineData(45, "45 minutes")]
    [InlineData(90, "1 hours 30 minutes")]
    [InlineData(120, "2 hours")]
    [InlineData(1_440, "1 days")]
    [InlineData(1_500, "1 days 1 hours")]
    public void A_duration_reads_back_in_the_largest_units_that_fit(int minutes, string expected)
    {
        Duration.FromMinutes(minutes).ToFriendlyString().ShouldBe(expected);
    }

    [Theory]
    [InlineData(30, "30m")]
    [InlineData(120, "2h")]
    [InlineData(4_320, "3d")]
    [InlineData(14_400, "1w")]
    [InlineData(86_400, "2mo")]
    [InlineData(576_000, "1y")]
    public void A_duration_has_a_compact_form(int minutes, string expected)
    {
        Duration.FromMinutes(minutes).ToCompactString().ShouldBe(expected);
    }

    [Fact]
    public void Durations_add_up_and_compare()
    {
        var hour = Duration.FromHours(1);
        var halfHour = Duration.FromMinutes(30);

        hour.Add(halfHour).TotalMinutes.ShouldBe(90);
        hour.IsGreaterThan(halfHour).ShouldBeTrue();
        halfHour.IsGreaterThan(hour).ShouldBeFalse();
        hour.IsGreaterThanOrEqual(Duration.FromMinutes(60)).ShouldBeTrue();
        hour.IsGreaterThan(Duration.FromMinutes(60)).ShouldBeFalse();
    }
}

public class SmokingHabitsTests
{
    [Fact]
    public void A_cigarette_costs_the_pack_price_divided_by_what_is_in_it()
    {
        var habits = SmokingHabits.Create(cigarettesPerDay: 20, cigarettesPerPack: 20, pricePerPack: 10m);

        habits.PricePerCigarette.Amount.ShouldBe(0.5m);
    }

    [Fact]
    public void A_day_of_smoking_costs_a_cigarette_price_times_the_daily_habit()
    {
        var habits = SmokingHabits.Create(cigarettesPerDay: 15, cigarettesPerPack: 20, pricePerPack: 10m);

        habits.DailyCost.Amount.ShouldBe(7.5m);
    }

    [Fact]
    public void Habits_keep_the_currency_they_were_priced_in()
    {
        var habits = SmokingHabits.Create(20, 20, 40_000m, "VND");

        habits.PricePerCigarette.Currency.ShouldBe("VND");
        habits.DailyCost.Amount.ShouldBe(40_000m);
    }

    [Theory]
    [InlineData(0, 20, 10, "Cigarettes per day must be greater than zero")]
    [InlineData(20, 0, 10, "Cigarettes per pack must be greater than zero")]
    [InlineData(20, 20, 0, "Price per pack must be greater than zero")]
    public void Habits_need_positive_numbers_throughout(int perDay, int perPack, decimal price, string expected)
    {
        var create = () => SmokingHabits.Create(perDay, perPack, price);

        create.ShouldThrow<DomainException>().Message.ShouldBe(expected);
    }

    [Fact]
    public void Two_sets_of_habits_are_the_same_when_all_their_values_match()
    {
        SmokingHabits.Create(20, 20, 10m).ShouldBe(SmokingHabits.Create(20, 20, 10m));
        SmokingHabits.Create(20, 20, 10m).ShouldNotBe(SmokingHabits.Create(21, 20, 10m));
    }
}
