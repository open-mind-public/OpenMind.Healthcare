using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;
using MediatR;

namespace DietApi.Features.DietGuidance.GetEatingTips;

public record GetEatingTipsQuery(TipCategory? Category) : IRequest<IReadOnlyList<EatingTipDto>>;

public class GetEatingTipsHandler(IEatingTipRepository tipRepository)
    : IRequestHandler<GetEatingTipsQuery, IReadOnlyList<EatingTipDto>>
{
    public async Task<IReadOnlyList<EatingTipDto>> Handle(
        GetEatingTipsQuery request, CancellationToken cancellationToken)
    {
        var tips = await tipRepository.GetAsync(request.Category, cancellationToken);

        return [.. tips.Select(DietGuidanceMapper.ToDto)];
    }
}
