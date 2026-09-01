using DDD.BuildingBlocks;
using MediatR;
using QuitSmokingApi.Domain.Entities;
using QuitSmokingApi.Domain.Repositories;
using QuitSmokingApi.Services;

namespace QuitSmokingApi.Features.SmokedDays.MarkDayAsSmoked;

public record MarkDayAsSmokedCommand(
    DateOnly Date,
    int CigarettesSmoked,
    RelapseTrigger Trigger,
    string? Note
) : IRequest<SmokedDayDto>;

public class MarkDayAsSmokedHandler(
    IQuitJourneyRepository journeyRepository,
    IUserService userService) : IRequestHandler<MarkDayAsSmokedCommand, SmokedDayDto>
{
    public async Task<SmokedDayDto> Handle(MarkDayAsSmokedCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var journey = await journeyRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Start your quit journey before marking a day as smoked");

        var smokedDay = journey.MarkDayAsSmoked(
            request.Date,
            request.CigarettesSmoked,
            request.Trigger,
            request.Note);

        await journeyRepository.UpdateAsync(journey, cancellationToken);

        return SmokedDayMapper.ToDto(smokedDay, journey.SmokingHabits.PricePerCigarette);
    }
}
