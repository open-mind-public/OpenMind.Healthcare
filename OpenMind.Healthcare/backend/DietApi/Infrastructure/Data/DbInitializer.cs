using DietApi.Infrastructure.Data.Seeds;

namespace DietApi.Infrastructure.Data;

/// <summary>
/// Seeds curated reference data - the food library, the activity catalogue, achievement
/// definitions, and eating tips.
/// </summary>
/// <remarks>
/// Every seed is guarded so restarting the service never duplicates rows, and there is exactly
/// one <c>SaveChanges</c> at the end. Containers restart; a seed that is not idempotent corrupts
/// the catalogue on the second boot.
/// </remarks>
public static class DbInitializer
{
    public static void Initialize(DietDbContext context)
    {
        var hasChanges = false;

        if (!context.FoodLibraryItems.Any())
        {
            context.FoodLibraryItems.AddRange(FoodLibrarySeed.Items());
            hasChanges = true;
        }

        if (!context.ActivityTypes.Any())
        {
            context.ActivityTypes.AddRange(ActivityCatalogueSeed.Activities());
            hasChanges = true;
        }

        if (!context.DietAchievements.Any())
        {
            context.DietAchievements.AddRange(GuidanceSeed.Achievements());
            hasChanges = true;
        }

        if (!context.EatingTips.Any())
        {
            context.EatingTips.AddRange(GuidanceSeed.Tips());
            hasChanges = true;
        }

        if (hasChanges)
        {
            context.SaveChanges();
        }
    }
}
