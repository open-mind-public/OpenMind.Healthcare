using DietApi.Domain.Repositories;
using MediatR;

namespace DietApi.Features.ActivityCatalogue.SearchActivities;

public record SearchActivitiesQuery(string Query, int Limit = 20) : IRequest<ActivitySearchResponse>;

public class SearchActivitiesHandler(IActivityTypeRepository catalogue)
    : IRequestHandler<SearchActivitiesQuery, ActivitySearchResponse>
{
    public async Task<ActivitySearchResponse> Handle(SearchActivitiesQuery request, CancellationToken cancellationToken)
    {
        var matches = await catalogue.SearchAsync(request.Query, request.Limit, cancellationToken);

        return new ActivitySearchResponse(
            request.Query,
            [.. matches.Select(ActivityCatalogueMapper.ToDto)]);
    }
}
