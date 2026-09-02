using DietApi.Domain.Repositories;
using MediatR;

namespace DietApi.Features.FoodLibrary.SearchFoods;

public record SearchFoodsQuery(string Query, int Limit = 20) : IRequest<FoodSearchResponse>;

public class SearchFoodsHandler(IFoodLibraryRepository libraryRepository)
    : IRequestHandler<SearchFoodsQuery, FoodSearchResponse>
{
    public async Task<FoodSearchResponse> Handle(SearchFoodsQuery request, CancellationToken cancellationToken)
    {
        var matches = await libraryRepository.SearchAsync(request.Query, request.Limit, cancellationToken);

        return new FoodSearchResponse(
            request.Query,
            [.. matches.Select(FoodLibraryMapper.ToDto)]);
    }
}
