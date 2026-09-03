namespace DietApi.Domain.ValueObjects;

/// <summary>
/// One food's contribution to a period's energy.
/// </summary>
/// <remarks>
/// These come as a top ten, and the collection is deliberately <em>not</em> exhaustive - their
/// shares do not sum to 100 and nothing here should imply they do. The question this answers is
/// "what are my biggest sources", which needs the largest few, not a partition of everything.
/// </remarks>
public record FoodContribution(
    Guid FoodLibraryItemId,
    string FoodName,
    int Kilocalories,
    decimal ShareOfTotal,
    int TimesLogged);
