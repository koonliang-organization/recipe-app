using Core.Application.Commands.Recipe;
using Core.Application.DTOs;
using Core.Application.Queries.Recipe;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Recipe.Controllers;
using Xunit;

namespace Lambdas.Tests;

public class RecipesControllerTests
{
    private static RecipesController NewController(Mock<IMediator> mediator, Guid? userId = null, bool includeAuthContext = true)
    {
        var ctrl = new RecipesController(mediator.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var ctx = ctrl.ControllerContext.HttpContext!;
        var uid = (userId ?? Guid.NewGuid()).ToString();
        ctx.Request.Headers["X-User-Id"] = uid;
        if (includeAuthContext)
            ctx.Request.Headers["X-Authorizer-Context"] = "ok";
        return ctrl;
    }

    [Fact]
    public async Task GetRecipes_ReturnsOk_WithFormattedResponse()
    {
        var mediator = new Mock<IMediator>();
        var items = new List<RecipeDto> { new() { Id = Guid.NewGuid(), Title = "T" } };
        var pag = new PaginationInfo { Page = 1, Limit = 20, Total = 1, TotalPages = 1 };
        var paged = new PagedResult<RecipeDto> { Items = items, Pagination = pag };
        mediator.Setup(m => m.Send(It.IsAny<GetRecipesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Common.Result.Success(paged));

        var ctrl = NewController(mediator);
        var res = await ctrl.GetRecipes(null, null, 1, 20) as OkObjectResult;
        res.Should().NotBeNull();
        // Serialize anonymous object and inspect JSON
        var json = System.Text.Json.JsonSerializer.Serialize(res!.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var recipes = doc.RootElement.GetProperty("recipes");
        recipes.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        recipes.EnumerateArray().Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateRecipe_ReturnsCreated_OnSuccess()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateRecipeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Common.Result.Success(new RecipeDto { Id = Guid.NewGuid(), Title = "T" }));

        var ctrl = NewController(mediator);
        var req = new CreateRecipeRequest("T", "D", "C", null,
            new List<CreateIngredientDto> { new() { Name = "i", Quantity = "1", Unit = "u" } },
            new List<CreateStepDto> { new() { StepNumber = 1, InstructionText = "s" } });

        var res = await ctrl.CreateRecipe(req) as CreatedAtActionResult;
        res.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateRecipe_MapsErrors_ToStatusCodes()
    {
        var mediator = new Mock<IMediator>();
        var ctrl = NewController(mediator);

        // not found
        mediator.Reset();
        mediator.Setup(m => m.Send(It.IsAny<UpdateRecipeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Common.Result.Failure<RecipeDto>("Recipe not found"));
        var res404 = await ctrl.UpdateRecipe(Guid.NewGuid(), new UpdateRecipeRequest("t","d","c",null,new(),new()));
        res404.Should().BeOfType<NotFoundObjectResult>();

        // access denied
        mediator.Reset();
        mediator.Setup(m => m.Send(It.IsAny<UpdateRecipeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Common.Result.Failure<RecipeDto>("Access denied"));
        var res403 = await ctrl.UpdateRecipe(Guid.NewGuid(), new UpdateRecipeRequest("t","d","c",null,new(),new()));
        res403.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task FavoriteEndpoints_ReturnOk_OnSuccess()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<ToggleFavoriteCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Common.Result.Success());
        var ctrl = NewController(mediator);

        var ok1 = await ctrl.AddToFavorites(Guid.NewGuid()) as OkObjectResult;
        var ok2 = await ctrl.RemoveFromFavorites(Guid.NewGuid()) as OkObjectResult;
        ok1.Should().NotBeNull();
        ok2.Should().NotBeNull();
    }

    [Fact]
    public async Task MissingHeaders_ThrowsUnauthorized()
    {
        var mediator = new Mock<IMediator>();
        var ctrl = NewController(mediator, includeAuthContext: false);
        // Remove header to force unauthorized path
        ctrl.ControllerContext.HttpContext!.Request.Headers.Remove("X-Authorizer-Context");
        ctrl.ControllerContext.HttpContext!.Request.Headers.Remove("X-User-Id");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ctrl.GetRecipes(null, null, 1, 20));
    }
}
