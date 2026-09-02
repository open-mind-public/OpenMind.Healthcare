using DietApi.Domain.Aggregates;
using DietApi.Domain.ValueObjects;

namespace DietApi.Infrastructure.Data.Seeds;

/// <summary>
/// Achievement definitions and eating tips shipped with the application.
/// </summary>
public static class GuidanceSeed
{
    public static DietAchievement[] Achievements() =>
    [
        DietAchievement.Create("First day logged", "You logged your first day. Everything starts here.", "🌱",
            AchievementCriterion.TotalDaysLogged, 1),
        DietAchievement.Create("Thirty days logged", "Thirty days recorded. You are building a real picture.", "📔",
            AchievementCriterion.TotalDaysLogged, 30),
        DietAchievement.Create("One hundred days logged", "A hundred days of logging. That is dedication.", "💯",
            AchievementCriterion.TotalDaysLogged, 100),

        DietAchievement.Create("A week on target", "Seven days in a row on target.", "🥗",
            AchievementCriterion.ConsecutiveOnTargetDays, 7),
        DietAchievement.Create("A fortnight on target", "Fourteen days in a row. This is becoming a habit.", "🥑",
            AchievementCriterion.ConsecutiveOnTargetDays, 14),
        DietAchievement.Create("A month on target", "Thirty days in a row on target. Remarkable.", "🏅",
            AchievementCriterion.ConsecutiveOnTargetDays, 30),

        DietAchievement.Create("A month on plan", "Thirty days since you started. Still here.", "📅",
            AchievementCriterion.DaysOnPlan, 30),
        DietAchievement.Create("A hundred days on plan", "A hundred days since you started your plan.", "🗓️",
            AchievementCriterion.DaysOnPlan, 100)
    ];

    public static EatingTip[] Tips() =>
    [
        // Craving
        EatingTip.Create("Wait ten minutes", "Most cravings peak and pass within ten minutes. Set a timer and do something else until it rings.", "⏳", TipCategory.Craving),
        EatingTip.Create("Drink a glass of water first", "Thirst is easy to mistake for hunger. Have a glass of water, then see whether you are still hungry.", "💧", TipCategory.Craving),
        EatingTip.Create("Have the smaller version", "A craving satisfied with two squares of chocolate is still satisfied. Denying it entirely often costs more later.", "🍫", TipCategory.Craving),
        EatingTip.Create("Name what you actually want", "Bored, tired, or stressed feels a lot like hungry. Naming it makes it easier to answer properly.", "🏷️", TipCategory.Craving),

        // Planning
        EatingTip.Create("Decide before you are hungry", "Choosing dinner at 6pm on an empty stomach rarely goes the way you planned at breakfast.", "📝", TipCategory.Planning),
        EatingTip.Create("Cook once, eat twice", "Making a double portion costs almost no extra effort and removes tomorrow's decision entirely.", "🍲", TipCategory.Planning),
        EatingTip.Create("Keep an easy default", "One reliable meal you can make without thinking is worth more than ten you never cook.", "🥘", TipCategory.Planning),
        EatingTip.Create("Shop from a list", "What comes home is what gets eaten. The decision is easier in the shop than in the kitchen.", "🛒", TipCategory.Planning),

        // Portion control
        EatingTip.Create("Use a smaller plate", "The same food on a smaller plate reads as a fuller meal. It is a trick, and it works anyway.", "🍽️", TipCategory.PortionControl),
        EatingTip.Create("Serve, then sit", "Leaving the serving dish in the kitchen makes seconds a decision rather than a reflex.", "🪑", TipCategory.PortionControl),
        EatingTip.Create("Half the plate vegetables", "Filling half the plate with vegetables first leaves less room to overshoot on everything else.", "🥦", TipCategory.PortionControl),
        EatingTip.Create("Eat slower than feels natural", "Fullness takes about twenty minutes to register. Eating quickly beats the signal.", "🐢", TipCategory.PortionControl),

        // Eating out
        EatingTip.Create("Look at the menu first", "Deciding before you arrive, while you are calm and not hungry, is a different decision entirely.", "📱", TipCategory.EatingOut),
        EatingTip.Create("Ask for the sauce on the side", "Dressings and sauces carry more calories than almost anything else you did not choose.", "🥄", TipCategory.EatingOut),
        EatingTip.Create("One indulgence, not three", "Starter, wine, and dessert is three decisions. Picking the one you actually want makes it a treat.", "🍷", TipCategory.EatingOut),

        // Mindset
        EatingTip.Create("One day is just one day", "A day over target changes nothing about tomorrow. The pattern matters, not the exception.", "🌤️", TipCategory.Mindset),
        EatingTip.Create("Log the days you would rather not", "The honest record is the useful one. A gap where a bad day was helps nobody.", "✍️", TipCategory.Mindset),
        EatingTip.Create("Progress is not a straight line", "Weight moves for reasons that have nothing to do with what you ate. Look at weeks, not days.", "📈", TipCategory.Mindset),
        EatingTip.Create("Be as kind as you would be to a friend", "You would not tell a friend they had ruined everything. Do not say it to yourself either.", "💚", TipCategory.Mindset)
    ];
}
