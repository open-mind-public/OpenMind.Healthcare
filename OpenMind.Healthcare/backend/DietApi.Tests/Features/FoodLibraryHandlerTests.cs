using DietApi.Features.FoodLibrary.GetFood;
using DietApi.Features.FoodLibrary.SearchFoods;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the curated library. These handlers resolve no member - the catalogue is the
/// same for everyone, and the endpoint group's RequireAuthorization keeps it behind a sign-in.
/// </summary>
public class FoodLibraryHandlerTests
{
    private readonly FakeFoodLibraryRepository _library =
        FakeFoodLibraryRepository.Containing(
            FakeFoodLibraryRepository.Oats(),
            FakeFoodLibraryRepository.Banana());

    [Fact]
    public async Task Searching_returns_matching_foods_with_their_serving_sizes()
    {
        var handler = new SearchFoodsHandler(_library);

        var result = await handler.Handle(new SearchFoodsQuery("oat"), CancellationToken.None);

        result.Matches.Count.ShouldBe(1);
        result.Matches[0].Name.ShouldBe("Porridge oats");
        result.Matches[0].ServingSizes.Count.ShouldBe(2);
        result.Matches[0].ServingSizes[0].Nutrition.Calories.ShouldBe(228);
    }

    [Fact]
    public async Task Search_is_case_insensitive()
    {
        var handler = new SearchFoodsHandler(_library);

        (await handler.Handle(new SearchFoodsQuery("BANANA"), CancellationToken.None))
            .Matches.Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_food_the_library_does_not_have_returns_no_matches_rather_than_an_error()
    {
        // This empty list is how a member learns the food is unavailable. Nothing is invented.
        var handler = new SearchFoodsHandler(_library);

        var result = await handler.Handle(new SearchFoodsQuery("kohlrabi gratin"), CancellationToken.None);

        result.Matches.ShouldBeEmpty();
        result.Query.ShouldBe("kohlrabi gratin");
    }

    [Fact]
    public async Task An_empty_query_returns_nothing()
    {
        var handler = new SearchFoodsHandler(_library);

        (await handler.Handle(new SearchFoodsQuery("  "), CancellationToken.None)).Matches.ShouldBeEmpty();
    }

    [Fact]
    public async Task Fetching_a_food_by_id_returns_it()
    {
        var oats = FakeFoodLibraryRepository.Oats();
        var handler = new GetFoodHandler(FakeFoodLibraryRepository.Containing(oats));

        var result = await handler.Handle(new GetFoodQuery(oats.Id), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Porridge oats");
    }

    [Fact]
    public async Task Fetching_an_unknown_food_returns_null_so_the_endpoint_can_answer_404()
    {
        var handler = new GetFoodHandler(_library);

        (await handler.Handle(new GetFoodQuery(Guid.NewGuid()), CancellationToken.None)).ShouldBeNull();
    }
}
