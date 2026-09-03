using DDD.BuildingBlocks;
using DietApi.Domain.Aggregates;
using DietApi.Domain.Rules;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// Positions stay contiguous, whatever the member does.
/// </summary>
/// <remarks>
/// A hole in the ordering is invisible until it is not: two shortcuts at the same position render
/// in an arbitrary order that can change between reads, and a member who carefully arranged their
/// list finds it rearranged itself. The invariant is asserted after every mutation rather than
/// trusted.
/// </remarks>
public class ExerciseShortcutOrderingTests
{
    [Fact]
    public void New_shortcuts_are_appended_in_order()
    {
        var plan = DietPlanBuilder.APlan().Build();

        var first = plan.SaveExerciseShortcut(Guid.NewGuid(), 45, "First");
        var second = plan.SaveExerciseShortcut(Guid.NewGuid(), 30, "Second");

        first.Position.ShouldBe(0);
        second.Position.ShouldBe(1);
        AssertContiguous(plan);
    }

    [Fact]
    public void Removing_from_the_middle_leaves_no_hole()
    {
        var plan = DietPlanBuilder.APlan().Build();
        plan.SaveExerciseShortcut(Guid.NewGuid(), 45, "First");
        var middle = plan.SaveExerciseShortcut(Guid.NewGuid(), 30, "Second");
        plan.SaveExerciseShortcut(Guid.NewGuid(), 20, "Third");

        plan.RemoveExerciseShortcut(middle.Id).ShouldBeTrue();

        AssertContiguous(plan);
        plan.ShortcutsInOrder().Select(s => s.Name).ShouldBe(["First", "Third"]);
    }

    [Fact]
    public void Reordering_puts_them_where_the_member_asked()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var a = plan.SaveExerciseShortcut(Guid.NewGuid(), 45, "A");
        var b = plan.SaveExerciseShortcut(Guid.NewGuid(), 30, "B");
        var c = plan.SaveExerciseShortcut(Guid.NewGuid(), 20, "C");

        plan.ReorderExerciseShortcuts([c.Id, a.Id, b.Id]);

        plan.ShortcutsInOrder().Select(s => s.Name).ShouldBe(["C", "A", "B"]);
        AssertContiguous(plan);
    }

    [Fact]
    public void Reordering_is_idempotent()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var a = plan.SaveExerciseShortcut(Guid.NewGuid(), 45, "A");
        var b = plan.SaveExerciseShortcut(Guid.NewGuid(), 30, "B");

        plan.ReorderExerciseShortcuts([b.Id, a.Id]);
        plan.ReorderExerciseShortcuts([b.Id, a.Id]);

        plan.ShortcutsInOrder().Select(s => s.Name).ShouldBe(["B", "A"]);
        AssertContiguous(plan);
    }

    [Fact]
    public void A_reorder_that_misses_a_shortcut_is_refused()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var a = plan.SaveExerciseShortcut(Guid.NewGuid(), 45, "A");
        plan.SaveExerciseShortcut(Guid.NewGuid(), 30, "B");

        var act = () => { plan.ReorderExerciseShortcuts([a.Id]); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(ReorderMustCoverEveryShortcutRule));
    }

    [Fact]
    public void A_reorder_naming_something_the_member_does_not_own_is_refused()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var a = plan.SaveExerciseShortcut(Guid.NewGuid(), 45, "A");

        var act = () => { plan.ReorderExerciseShortcuts([a.Id, Guid.NewGuid()]); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(ReorderMustCoverEveryShortcutRule));
    }

    [Fact]
    public void A_reorder_listing_the_same_shortcut_twice_is_refused()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var a = plan.SaveExerciseShortcut(Guid.NewGuid(), 45, "A");
        plan.SaveExerciseShortcut(Guid.NewGuid(), 30, "B");

        var act = () => { plan.ReorderExerciseShortcuts([a.Id, a.Id]); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(ReorderMustCoverEveryShortcutRule));
    }

    [Fact]
    public void Saving_after_a_reorder_still_appends_to_the_end()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var a = plan.SaveExerciseShortcut(Guid.NewGuid(), 45, "A");
        var b = plan.SaveExerciseShortcut(Guid.NewGuid(), 30, "B");

        plan.ReorderExerciseShortcuts([b.Id, a.Id]);
        plan.SaveExerciseShortcut(Guid.NewGuid(), 20, "C");

        plan.ShortcutsInOrder().Select(s => s.Name).ShouldBe(["B", "A", "C"]);
        AssertContiguous(plan);
    }

    /// <summary>Positions are exactly 0..n-1, with no gaps and no duplicates.</summary>
    private static void AssertContiguous(DietPlan plan)
    {
        var positions = plan.ExerciseShortcuts.Select(s => s.Position).OrderBy(p => p).ToList();

        positions.ShouldBe(Enumerable.Range(0, plan.ExerciseShortcuts.Count));
        positions.Distinct().Count().ShouldBe(positions.Count);
    }
}
