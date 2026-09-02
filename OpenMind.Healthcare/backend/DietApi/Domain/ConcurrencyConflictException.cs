namespace DietApi.Domain;

/// <summary>
/// Thrown when a write is built on a copy of a day that another session has already changed.
/// </summary>
/// <remarks>
/// Distinct from <c>DomainException</c> because it is not a broken business rule - the member did
/// nothing wrong, their copy is simply out of date. The endpoint answers 409 so the client can
/// reload and reapply, rather than 400, which would suggest the request itself was invalid.
/// Writes are refused rather than merged: merging would silently resurrect an entry the member
/// deleted on the other device.
/// </remarks>
public class ConcurrencyConflictException(string message) : Exception(message)
{
    public static ConcurrencyConflictException ForDay(DateOnly date) =>
        new($"This day was changed somewhere else since you loaded it ({date:yyyy-MM-dd}). "
            + "Reload to see the latest entries, then try again.");
}
