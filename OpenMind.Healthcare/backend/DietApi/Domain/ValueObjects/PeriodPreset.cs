namespace DietApi.Domain.ValueObjects;

/// <summary>
/// The windows a member can analyse. Presets rather than an arbitrary date range, because the
/// question is almost always "this week", "this month" or "since I started".
/// </summary>
/// <remarks>
/// The day counts each resolves to: <see cref="Week"/> 7, <see cref="Month"/> 30,
/// <see cref="Quarter"/> 90, and <see cref="Plan"/> everything from the plan's start date. Every
/// one is then clamped to the plan and to today, so a member who joined on Thursday does not get
/// a quarter of empty days (FR-002).
/// </remarks>
public enum PeriodPreset
{
    Week,
    Month,
    Quarter,
    Plan
}
