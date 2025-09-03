using Core.Application.Handlers.Recipe;
using Core.Application.Interfaces;
using Core.Application.Queries.Recipe;
using Core.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Core.Application.Tests;

public class GetRecipeByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldFail_WhenRecipeDoesNotExist()
    {
        var repo = new Mock<IRecipeRepository>();
        var logger = Mock.Of<ILogger<GetRecipeByIdQueryHandler>>();
        repo.Setup(r => r.RecipeExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var handler = new GetRecipeByIdQueryHandler(repo.Object, logger);

        var res = await handler.Handle(new GetRecipeByIdQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
        res.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenNullAfterExists()
    {
        var repo = new Mock<IRecipeRepository>();
        var logger = Mock.Of<ILogger<GetRecipeByIdQueryHandler>>();
        var id = Guid.NewGuid();
        var user = Guid.NewGuid();
        repo.Setup(r => r.RecipeExistsAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Recipe?)null);
        var handler = new GetRecipeByIdQueryHandler(repo.Object, logger);

        var res = await handler.Handle(new GetRecipeByIdQuery(id, user), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnDto_WhenFound()
    {
        var repo = new Mock<IRecipeRepository>();
        var logger = Mock.Of<ILogger<GetRecipeByIdQueryHandler>>();
        var id = Guid.NewGuid();
        var user = Guid.NewGuid();
        var recipe = new Recipe("T", "D", "C", user) { };

        repo.Setup(r => r.RecipeExistsAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        repo.Setup(r => r.IsRecipeFavoriteAsync(recipe.Id, user, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new GetRecipeByIdQueryHandler(repo.Object, logger);

        var res = await handler.Handle(new GetRecipeByIdQuery(id, user), CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
        res.Value.Id.Should().Be(recipe.Id);
        res.Value.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_OnException()
    {
        var repo = new Mock<IRecipeRepository>();
        var logger = Mock.Of<ILogger<GetRecipeByIdQueryHandler>>();
        var id = Guid.NewGuid();
        var user = Guid.NewGuid();
        repo.Setup(r => r.RecipeExistsAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("db"));
        var handler = new GetRecipeByIdQueryHandler(repo.Object, logger);

        var res = await handler.Handle(new GetRecipeByIdQuery(id, user), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
    }
}

