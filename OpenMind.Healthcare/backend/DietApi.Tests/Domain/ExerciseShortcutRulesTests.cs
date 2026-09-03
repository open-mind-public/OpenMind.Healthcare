using DDD.BuildingBlocks;
using DietApi.Domain.Aggregates;
using DietApi.Domain.Rules;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// The rules guarding a member's shortcuts.
/// </summary>
/// <remarks>
/// The cap and the no-duplicates rule are the reason shortcuts are owned by the plan rather than
/// standing alone: both are invariants over the whole set, and neither could be enforced from
/// inside a single shortcut without a race.
/// </remarks>
public class ExerciseShortcutRulesTests
{
    // --- The cap ----------------------------------------------------------

    [Fact]
    public void The_tenth_shortcut_saves_and_the_eleventh_does_not()
    {
        var plan = DietPlanBuilder.APlan().Build();

        for (var i = 0; i < DietPlan.MaxShortcuts; i++)
        {
            plan.SaveExerciseShortcut(Guid.NewGuid(), 30, $"Shortcut {i}");
        }

        plan.ExerciseShortcuts.Count.ShouldBe(DietPlan.MaxShortcuts);
        plan.RemainingShortcutSlots.ShouldBe(0);

        var act = () => { plan.SaveExerciseShortcut(Guid.NewGuid(), 30, "One too many"); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(ShortcutLimitRule));
    }

    [Fact]
    public void The_limit_message_tells_the_member_what_to_do_about_it()
    {
        var plan = DietPlanBuilder.APlan().Build();

        for (var i = 0; i < DietPlan.MaxShortcuts; i++)
        {
            plan.SaveExerciseShortcut(Guid.NewGuid(), 30, $"Shortcut {i}");
        }

        var error = Should.Throw<BusinessRuleValidationException>(
            () => plan.SaveExerciseShortcut(Guid.NewGuid(), 30, "One too many"));

        error.Message.ShouldContain(DietPlan.MaxShortcuts.ToString());
        error.Message.ShouldContain("Remove");
    }

    [Fact]
    public void Removing_one_makes_room_again()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var first = plan.SaveExerciseShortcut(Guid.NewGuid(), 30, "First");

        for (var i = 1; i < DietPlan.MaxShortcuts; i++)
        {
            plan.SaveExerciseShortcut(Guid.NewGuid(), 30, $"Shortcut {i}");
        }

        plan.RemoveExerciseShortcut(first.Id).ShouldBeTrue();
        plan.RemainingShortcutSlots.ShouldBe(1);

        Should.NotThrow(() => plan.SaveExerciseShortcut(Guid.NewGuid(), 30, "Room again"));
    }

    // --- Duplicates -------------------------------------------------------

    [Fact]
    public void The_same_activity_and_duration_cannot_be_saved_twice()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var running = Guid.NewGuid();

        plan.SaveExerciseShortcut(running, 45, "Morning run");

        var act = () => { plan.SaveExerciseShortcut(running, 45, "Evening run"); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(ShortcutMustBeUniqueRule));
    }

    [Fact]
    public void The_duplicate_message_names_the_shortcut_it_clashes_with()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var running = Guid.NewGuid();

        plan.SaveExerciseShortcut(running, 45, "Morning run");

        Should.Throw<BusinessRuleValidationException>(
                () => plan.SaveExerciseShortcut(running, 45, "Evening run"))
            .Message.ShouldContain("Morning run");
    }

    [Fact]
    public void The_same_activity_at_a_different_duration_is_not_a_duplicate()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var running = Guid.NewGuid();

        plan.SaveExerciseShortcut(running, 45, "Long run");

        Should.NotThrow(() => plan.SaveExerciseShortcut(running, 30, "Short run"));
        plan.ExerciseShortcuts.Count.ShouldBe(2);
    }

    [Fact]
    public void A_different_activity_at_the_same_duration_is_not_a_duplicate()
    {
        var plan = DietPlanBuilder.APlan().Build();

        plan.SaveExerciseShortcut(Guid.NewGuid(), 45, "Run");

        Should.NotThrow(() => plan.SaveExerciseShortcut(Guid.NewGuid(), 45, "Swim"));
    }

    [Fact]
    public void Renaming_can_never_create_a_duplicate()
    {
        // Duplicates compare activity and duration, so two shortcuts may share a name without
        // being the same thing. This is why renaming needs no uniqueness check.
        var plan = DietPlanBuilder.APlan().Build();
        var first = plan.SaveExerciseShortcut(Guid.NewGuid(), 45, "Run");
        var second = plan.SaveExerciseShortcut(Guid.NewGuid(), 30, "Swim");

        Should.NotThrow(() => plan.RenameExerciseShortcut(second.Id, "Run"));

        plan.ExerciseShortcut(first.Id)!.Name.ShouldBe("Run");
        plan.ExerciseShortcut(second.Id)!.Name.ShouldBe("Run");
    }

    // --- Names ------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void A_shortcut_needs_a_name(string? name)
    {
        var plan = DietPlanBuilder.APlan().Build();

        var act = () => { plan.SaveExerciseShortcut(Guid.NewGuid(), 45, name!); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(ShortcutNameMustNotBeEmptyRule));
    }

    [Fact]
    public void A_name_of_exactly_the_maximum_length_is_allowed()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var name = new string('a', ShortcutNameWithinLengthRule.MaximumLength);

        Should.NotThrow(() => plan.SaveExerciseShortcut(Guid.NewGuid(), 45, name));
    }

    [Fact]
    public void A_name_one_character_too_long_is_refused()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var name = new string('a', ShortcutNameWithinLengthRule.MaximumLength + 1);

        var act = () => { plan.SaveExerciseShortcut(Guid.NewGuid(), 45, name); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(ShortcutNameWithinLengthRule));
    }

    [Fact]
    public void A_name_is_trimmed_when_saved_and_when_renamed()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var shortcut = plan.SaveExerciseShortcut(Guid.NewGuid(), 45, "  Morning run  ");

        shortcut.Name.ShouldBe("Morning run");

        plan.RenameExerciseShortcut(shortcut.Id, "  Evening run  ");
        plan.ExerciseShortcut(shortcut.Id)!.Name.ShouldBe("Evening run");
    }

    [Fact]
    public void Renaming_or_removing_something_that_is_not_the_members_reports_not_found()
    {
        var plan = DietPlanBuilder.APlan().Build();

        plan.RenameExerciseShortcut(Guid.NewGuid(), "Nope").ShouldBeFalse();
        plan.RemoveExerciseShortcut(Guid.NewGuid()).ShouldBeFalse();
    }
}
