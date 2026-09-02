using DietApi.Domain.Repositories;
using MediatR;

namespace DietApi.Features.FoodLibrary.GetFood;

public record GetFoodQuery(Guid Id) : IRequest<FoodLibraryItemDto?>;

public class GetFoodHandler(IFoodLibraryRepository libraryRepository)
    : IRequestHandler<GetFoodQuery, FoodLibraryItemDto?>
{
    public async Task<FoodLibraryItemDto?> Handle(GetFoodQuery request, CancellationToken cancellationToken)
    {
        var item = await libraryRepository.GetByIdAsync(request.Id, cancellationToken);

        return item is null ? null : FoodLibraryMapper.ToDto(item);
    }
}
