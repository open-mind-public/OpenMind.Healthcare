using DDD.BuildingBlocks;

namespace DietApi.Domain.Rules;

/// <summary>
/// Business rule: a member may keep only so many shortcuts.
/// </summary>
/// <remarks>
/// Past roughly ten, scanning the list costs more than typing the session and the feature stops
/// being a shortcut. The cap is also what makes it safe to own the collection inside the plan
/// aggregate rather than splitting it out.
/// </remarks>
public class ShortcutLimitRule(int current, int maximum) : IBusinessRule
{
    public string RuleName => nameof(ShortcutLimitRule);

    public string ErrorMessage =>
        $"You can keep {maximum} shortcuts. Remove one you no longer use to make room.";

    public bool IsBroken() => current >= maximum;
}

/// <summary>
/// Business rule: no two shortcuts may record the same thing.
/// </summary>
/// <remarks>
/// Compares the activity and the duration, never the name. Two differently named buttons that both
/// record a 45 minute run are the duplication this prevents; a 30 minute and a 45 minute run are
/// not duplicates however similarly they are named.
/// </remarks>
public class ShortcutMustBeUniqueRule(string? existingName) : IBusinessRule
{
    public string RuleName => nameof(ShortcutMustBeUniqueRule);

    public string ErrorMessage => $"You already have a shortcut for that - it is called '{existingName}'.";

    public bool IsBroken() => existingName is not null;
}

/// <summary>
/// Business rule: a shortcut needs a name to be recognisable by.
/// </summary>
public class ShortcutNameMustNotBeEmptyRule(string? name) : IBusinessRule
{
    public string RuleName => nameof(ShortcutNameMustNotBeEmptyRule);

    public string ErrorMessage => "A shortcut needs a name";

    public bool IsBroken() => string.IsNullOrWhiteSpace(name);
}

/// <summary>
/// Business rule: a shortcut name must fit on a button.
/// </summary>
public class ShortcutNameWithinLengthRule(string? name) : IBusinessRule
{
    public const int MaximumLength = 80;

    public string RuleName => nameof(ShortcutNameWithinLengthRule);

    public string ErrorMessage => $"A shortcut name cannot be longer than {MaximumLength} characters";

    public bool IsBroken() => name is not null && name.Trim().Length > MaximumLength;
}

/// <summary>
/// Business rule: a reorder must account for every shortcut the member has, exactly once.
/// </summary>
/// <remarks>
/// Reordering is expressed as the complete ordered list rather than as "move this one up", because
/// a full list is idempotent and has no race: two clients sending different orders produce one of
/// the two, never an interleaving. This rule is what makes a partial or foreign list a refusal
/// rather than a silently wrong order.
/// </remarks>
public class ReorderMustCoverEveryShortcutRule(IReadOnlyCollection<Guid> submitted, IReadOnlyCollection<Guid> owned)
    : IBusinessRule
{
    public string RuleName => nameof(ReorderMustCoverEveryShortcutRule);

    public string ErrorMessage => "A reorder must list every one of your shortcuts exactly once";

    public bool IsBroken() =>
        submitted.Count != owned.Count
        || submitted.Distinct().Count() != submitted.Count
        || !submitted.All(owned.Contains);
}
