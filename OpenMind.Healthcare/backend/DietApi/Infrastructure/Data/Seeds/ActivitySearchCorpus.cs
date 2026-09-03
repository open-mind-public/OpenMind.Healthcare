namespace DietApi.Infrastructure.Data.Seeds;

/// <summary>
/// The fixed corpus SC-003 is judged against: what people actually type when they want to record
/// something they did.
/// </summary>
/// <remarks>
/// <para>
/// Written from everyday language <em>before</em> and independently of
/// <see cref="ActivityCatalogueSeed"/>, and deliberately not derived from it. A corpus picked from
/// the seed would score 100% and measure nothing at all; the point is to find the words the
/// catalogue does not answer.
/// </para>
/// <para>
/// It finds two. "gym" and "weights" return nothing, because no catalogue entry contains either
/// word - the closest is "Weight training", which "weights" does not match as a substring. That is
/// recorded here rather than fixed by renaming entries to suit the corpus, which would be the same
/// mistake as deriving the corpus from the seed. If the catalogue is widened later, these are the
/// first two gaps to close.
/// </para>
/// </remarks>
public static class ActivitySearchCorpus
{
    /// <summary>The bar from SC-003: a usable match in the first five results.</summary>
    public const int ResultsExamined = 5;

    /// <summary>The share of terms that must return a match within those five results.</summary>
    public const decimal RequiredHitRate = 0.85m;

    public static IReadOnlyList<string> Terms() =>
    [
        "running",
        "jogging",
        "walking",
        "cycling",
        "swimming",
        "gym",
        "weights",
        "yoga",
        "pilates",
        "football",
        "tennis",
        "badminton",
        "basketball",
        "golf",
        "dancing",
        "hiking",
        "rowing",
        "skipping",
        "stairs",
        "gardening",
        "mowing",
        "cleaning",
        "vacuuming",
        "treadmill",
        "boxing"
    ];
}
