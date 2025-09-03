using Core.Application.Commands.Recipe;
using Core.Application.Handlers.Recipe;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace Core.Application.Tests;

public class DeleteAndFavoriteHandlersTests
{
    [Fact]
    public async Task Delete_ShouldFail_WhenNotFound()
    {
        var repo = new Mock<IRecipeRepository>();
        var img = new Mock<IImageStorageService>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe?)null);
        var handler = new DeleteRecipeCommandHandler(repo.Object, img.Object);

        var res = await handler.Handle(new DeleteRecipeCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldFail_WhenAccessDenied()
    {
        var repo = new Mock<IRecipeRepository>();
        var img = new Mock<IImageStorageService>();
        var owner = Guid.NewGuid();
        var recipe = new Recipe("T", "D", "C", owner);
        repo.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        var handler = new DeleteRecipeCommandHandler(repo.Object, img.Object);

        var res = await handler.Handle(new DeleteRecipeCommand(recipe.Id, Guid.NewGuid()), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
        res.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Delete_ShouldDeleteImageAndRecipe()
    {
        var repo = new Mock<IRecipeRepository>();
        var img = new Mock<IImageStorageService>();
        var owner = Guid.NewGuid();
        var recipe = new Recipe("T", "D", "C", owner, photoUrl: "http://img");
        repo.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        repo.Setup(r => r.DeleteAsync(recipe, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        img.Setup(i => i.DeleteImageAsync("http://img")).Returns(Task.CompletedTask).Verifiable();
        var handler = new DeleteRecipeCommandHandler(repo.Object, img.Object);

        var res = await handler.Handle(new DeleteRecipeCommand(recipe.Id, owner), CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
        repo.Verify();
        img.Verify();
    }

    [Fact]
    public async Task ToggleFavorite_ShouldHandleAddAndRemove()
    {
        var repo = new Mock<IRecipeRepository>();
        var recipe = new Recipe("T", "D", "C", Guid.NewGuid());
        var user = Guid.NewGuid();
        repo.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        repo.Setup(r => r.IsRecipeFavoriteAsync(recipe.Id, user, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.AddFavoriteAsync(recipe.Id, user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

        var handler = new ToggleFavoriteCommandHandler(repo.Object);
        var add = await handler.Handle(new ToggleFavoriteCommand(recipe.Id, true, user), CancellationToken.None);
        add.IsSuccess.Should().BeTrue();
        repo.Verify();

        // Now mark as favorite already and remove
        repo.Reset();
        repo.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        repo.Setup(r => r.IsRecipeFavoriteAsync(recipe.Id, user, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.RemoveFavoriteAsync(recipe.Id, user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

        var remove = await handler.Handle(new ToggleFavoriteCommand(recipe.Id, false, user), CancellationToken.None);
        remove.IsSuccess.Should().BeTrue();
        repo.Verify();
    }
}

