using DietApi.Domain.Aggregates;
using DietApi.Domain.ValueObjects;

namespace DietApi.Infrastructure.Data.Seeds;

/// <summary>
/// The curated activity catalogue members choose from.
/// </summary>
/// <remarks>
/// <para>
/// MET values are taken from the <em>2011 Compendium of Physical Activities</em> (Ainsworth et
/// al., <i>Medicine and Science in Sports and Exercise</i> 43(8):1575-1581). Where the compendium
/// lists a range for an activity, the value for the effort named in the entry is used. Recording
/// the edition matters: a later revision changes some figures, and an unlabelled number is one
/// nobody can check.
/// </para>
/// <para>
/// Intensity is expressed as separate entries rather than a field on the log, so "Running,
/// 8 km/h" and "Running, 13 km/h" are two rows (research.md R-003). Common synonyms are carried
/// in the display name - "Football (soccer)", "Exercise bike" - because a member types what they
/// call it, not what a compendium calls it.
/// </para>
/// </remarks>
public static class ActivityCatalogueSeed
{
    public static IEnumerable<ActivityType> Activities() =>
    [
        // Walking
        ActivityType.Create("Walking, slow (3 km/h)", ActivityCategory.Walking, 2.8m),
        ActivityType.Create("Walking, moderate (5 km/h)", ActivityCategory.Walking, 3.5m),
        ActivityType.Create("Walking, brisk (5.5 km/h)", ActivityCategory.Walking, 4.3m),
        ActivityType.Create("Walking uphill (5.5 km/h, 5% grade)", ActivityCategory.Walking, 5.3m),
        ActivityType.Create("Walking the dog", ActivityCategory.Walking, 3.0m),
        ActivityType.Create("Hiking, cross country", ActivityCategory.Walking, 6.0m),
        ActivityType.Create("Nordic walking", ActivityCategory.Walking, 6.8m),
        ActivityType.Create("Walking up stairs", ActivityCategory.Walking, 8.0m),

        // Running
        ActivityType.Create("Jogging, general", ActivityCategory.Running, 7.0m),
        ActivityType.Create("Running, 8 km/h", ActivityCategory.Running, 8.3m),
        ActivityType.Create("Running, 10 km/h", ActivityCategory.Running, 9.8m),
        ActivityType.Create("Running, 11 km/h", ActivityCategory.Running, 11.0m),
        ActivityType.Create("Running, 13 km/h", ActivityCategory.Running, 11.8m),
        ActivityType.Create("Running, 16 km/h", ActivityCategory.Running, 14.5m),
        ActivityType.Create("Running, cross country", ActivityCategory.Running, 9.0m),
        ActivityType.Create("Treadmill running, general", ActivityCategory.Running, 8.0m),

        // Cycling
        ActivityType.Create("Cycling, leisurely (under 16 km/h)", ActivityCategory.Cycling, 4.0m),
        ActivityType.Create("Cycling, light (16-19 km/h)", ActivityCategory.Cycling, 6.8m),
        ActivityType.Create("Cycling, moderate (19-22 km/h)", ActivityCategory.Cycling, 8.0m),
        ActivityType.Create("Cycling, vigorous (22-25 km/h)", ActivityCategory.Cycling, 10.0m),
        ActivityType.Create("Mountain biking, general", ActivityCategory.Cycling, 8.5m),
        ActivityType.Create("Exercise bike, moderate", ActivityCategory.Cycling, 6.8m),
        ActivityType.Create("Spin class", ActivityCategory.Cycling, 8.5m),

        // Swimming
        ActivityType.Create("Swimming, leisurely", ActivityCategory.Swimming, 6.0m),
        ActivityType.Create("Swimming laps, front crawl, moderate", ActivityCategory.Swimming, 5.8m),
        ActivityType.Create("Swimming laps, front crawl, vigorous", ActivityCategory.Swimming, 9.8m),
        ActivityType.Create("Swimming, backstroke", ActivityCategory.Swimming, 4.8m),
        ActivityType.Create("Swimming, breaststroke", ActivityCategory.Swimming, 5.3m),
        ActivityType.Create("Swimming, butterfly", ActivityCategory.Swimming, 13.8m),
        ActivityType.Create("Water aerobics", ActivityCategory.Swimming, 5.5m),

        // Gym
        ActivityType.Create("Weight training, light or moderate", ActivityCategory.Gym, 3.5m),
        ActivityType.Create("Weight training, vigorous", ActivityCategory.Gym, 6.0m),
        ActivityType.Create("Circuit training, general", ActivityCategory.Gym, 7.2m),
        ActivityType.Create("Calisthenics, light (press-ups, sit-ups)", ActivityCategory.Gym, 3.8m),
        ActivityType.Create("Calisthenics, vigorous", ActivityCategory.Gym, 8.0m),
        ActivityType.Create("Rowing machine, moderate", ActivityCategory.Gym, 7.0m),
        ActivityType.Create("Elliptical trainer, moderate", ActivityCategory.Gym, 5.0m),
        ActivityType.Create("Stair climbing machine", ActivityCategory.Gym, 9.0m),
        ActivityType.Create("Yoga, hatha", ActivityCategory.Gym, 2.5m),
        ActivityType.Create("Yoga, power", ActivityCategory.Gym, 4.0m),
        ActivityType.Create("Pilates, general", ActivityCategory.Gym, 3.0m),
        ActivityType.Create("Aerobics class, low impact", ActivityCategory.Gym, 5.0m),
        ActivityType.Create("Aerobics class, high impact", ActivityCategory.Gym, 7.3m),
        ActivityType.Create("Skipping rope, moderate", ActivityCategory.Gym, 11.8m),
        ActivityType.Create("Stretching, general", ActivityCategory.Gym, 2.3m),

        // Sport
        ActivityType.Create("Football (soccer), casual", ActivityCategory.Sport, 7.0m),
        ActivityType.Create("Football (soccer), competitive", ActivityCategory.Sport, 10.0m),
        ActivityType.Create("Basketball, general", ActivityCategory.Sport, 6.5m),
        ActivityType.Create("Tennis, doubles", ActivityCategory.Sport, 6.0m),
        ActivityType.Create("Tennis, singles", ActivityCategory.Sport, 8.0m),
        ActivityType.Create("Badminton, social", ActivityCategory.Sport, 5.5m),
        ActivityType.Create("Table tennis", ActivityCategory.Sport, 4.0m),
        ActivityType.Create("Volleyball, non-competitive", ActivityCategory.Sport, 3.0m),
        ActivityType.Create("Squash", ActivityCategory.Sport, 7.3m),
        ActivityType.Create("Golf, walking with clubs", ActivityCategory.Sport, 4.8m),
        ActivityType.Create("Martial arts, moderate", ActivityCategory.Sport, 10.3m),
        ActivityType.Create("Boxing, punch bag", ActivityCategory.Sport, 5.5m),
        ActivityType.Create("Rock climbing, ascending", ActivityCategory.Sport, 8.0m),
        ActivityType.Create("Skiing, downhill, moderate", ActivityCategory.Sport, 5.3m),
        ActivityType.Create("Ice skating, general", ActivityCategory.Sport, 7.0m),
        ActivityType.Create("Kayaking", ActivityCategory.Sport, 5.0m),
        ActivityType.Create("Horse riding, trotting", ActivityCategory.Sport, 5.8m),
        ActivityType.Create("Dancing, ballroom, fast", ActivityCategory.Sport, 5.5m),
        ActivityType.Create("Dancing, aerobic", ActivityCategory.Sport, 7.3m),

        // Home and garden
        ActivityType.Create("Cleaning the house, light", ActivityCategory.HomeAndGarden, 2.5m),
        ActivityType.Create("Vacuuming", ActivityCategory.HomeAndGarden, 3.3m),
        ActivityType.Create("Mopping and scrubbing floors", ActivityCategory.HomeAndGarden, 3.5m),
        ActivityType.Create("Gardening, general", ActivityCategory.HomeAndGarden, 3.8m),
        ActivityType.Create("Mowing the lawn, powered mower", ActivityCategory.HomeAndGarden, 5.0m),
        ActivityType.Create("Digging in the garden", ActivityCategory.HomeAndGarden, 5.0m),
        ActivityType.Create("Raking the lawn", ActivityCategory.HomeAndGarden, 3.8m),
        ActivityType.Create("Shovelling snow by hand", ActivityCategory.HomeAndGarden, 5.3m),
        ActivityType.Create("Decorating and painting", ActivityCategory.HomeAndGarden, 3.3m),
        ActivityType.Create("Moving furniture", ActivityCategory.HomeAndGarden, 5.8m),

        // Everyday
        ActivityType.Create("Climbing stairs, slow", ActivityCategory.Everyday, 4.0m),
        ActivityType.Create("Carrying shopping upstairs", ActivityCategory.Everyday, 7.5m),
        ActivityType.Create("Shopping with a trolley", ActivityCategory.Everyday, 2.3m),
        ActivityType.Create("Playing with children, moderate", ActivityCategory.Everyday, 3.5m),
        ActivityType.Create("Pushing a pram", ActivityCategory.Everyday, 3.8m),
        ActivityType.Create("Cooking and food preparation", ActivityCategory.Everyday, 3.3m)
    ];
}
