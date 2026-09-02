using DietApi.Features.FoodLibrary.GetFood;
using DietApi.Features.FoodLibrary.SearchFoods;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DietApi.Features.FoodLibrary;

public static class FoodLibraryEndpoints
{
    public static void MapFoodLibraryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/food-library")
            .WithTags("FoodLibrary")
            .RequireAuthorization();

        group.MapGet("/search", Search)
            .WithName("SearchFoods")
            .WithOpenApi();

        group.MapGet("/{id:guid}", GetFood)
            .WithName("GetFood")
            .WithOpenApi();
    }

    private static async Task<IResult> Search(
        IMediator mediator,
        [FromQuery] string q = "",
        [FromQuery] int limit = 20)
    {
        var result = await mediator.Send(new SearchFoodsQuery(q, limit));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetFood(Guid id, IMediator mediator)
    {
        var food = await mediator.Send(new GetFoodQuery(id));
        return food is null ? Results.NotFound() : Results.Ok(food);
    }
}
