namespace DietApi.Domain.ValueObjects;

/// <summary>
/// Whether the target in force was accepted from the system's suggestion or set by the member.
/// Recorded so a refreshed suggestion never silently overwrites a deliberate choice.
/// </summary>
public enum TargetSource
{
    Suggested,
    MemberSet
}
