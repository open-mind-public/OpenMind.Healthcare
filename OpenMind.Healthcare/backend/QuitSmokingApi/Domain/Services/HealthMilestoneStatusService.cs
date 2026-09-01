using QuitSmokingApi.Domain.Aggregates;
using QuitSmokingApi.Domain.ValueObjects;

namespace QuitSmokingApi.Domain.Services;

public class HealthMilestoneStatusService
{
    public HealthMilestoneStatus ComputeStatus(HealthMilestone milestone, QuitJourney? journey)
    {
        if (journey == null)
        {
            return HealthMilestoneStatus.CreateNotStarted(
                milestone.Id,
                milestone.Title,
                milestone.Description,
                milestone.TimeRequiredMinutes,
                milestone.TimeDisplay,
                milestone.Icon,
                milestone.Category.ToString());
        }
        
        // Healing is measured in smoke-free time, so days the user marked as smoked do not count
        // towards a milestone - and they push the date it is reached back by the same amount.
        var smokeFreeMinutes = journey.GetTimeSmokeFree().TotalMinutes;
        var effectiveStart = journey.QuitDate.AddDays(journey.GetSmokedDayCount());

        return HealthMilestoneStatus.Create(
            milestone.Id,
            milestone.Title,
            milestone.Description,
            milestone.TimeRequiredMinutes,
            milestone.TimeDisplay,
            milestone.Icon,
            milestone.Category.ToString(),
            smokeFreeMinutes,
            effectiveStart);
    }
    
    public IReadOnlyList<HealthMilestoneStatus> ComputeStatuses(
        IEnumerable<HealthMilestone> milestones, 
        QuitJourney? journey)
    {
        return milestones
            .Select(m => ComputeStatus(m, journey))
            .ToList();
    }
}
