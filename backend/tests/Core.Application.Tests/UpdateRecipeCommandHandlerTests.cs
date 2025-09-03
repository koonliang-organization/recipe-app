using Core.Application.Commands.Recipe;
using Core.Application.DTOs;
using Core.Application.Handlers.Recipe;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Core.Application.Tests;

public class UpdateRecipeCommandHandlerTests
{
    private static (Mock<IRecipeRepository> repo, Mock<IImageStorageService> img, UpdateRecipeCommandHandler handler, Recipe recipe, Guid userId)
        SetupWithExistingRecipe(string? photo = null)
    {
        var repo = new Mock<IRecipeRepository>();
        var img = new Mock<IImageStorageService>();
        var logger = Mock.Of<ILogger<UpdateRecipeCommandHandler>>();
        var handler = new UpdateRecipeCommandHandler(repo.Object, img.Object, logger);

        var userId = Guid.NewGuid();
        var recipe = new Recipe("T", "D", "C", userId, photo);
        repo.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        repo.Setup(r => r.IsRecipeFavoriteAsync(recipe.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.UpdateAsync(recipe, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return (repo, img, handler, recipe, userId);
    }

    private static UpdateRecipeCommand NewCmd(Guid id, Guid userId,
        string? title = "T",
        string? category = "C",
        string? description = "D",
        string? photo = null,
        List<UpdateIngredientDto>? ingredients = null,
        List<UpdateStepDto>? steps = null)
    {
        return new UpdateRecipeCommand(
            id,
            title ?? string.Empty,
            description ?? string.Empty,
            category ?? string.Empty,
            photo,
            ingredients ?? new List<UpdateIngredientDto> { new() { Name = "n", Quantity = "1", Unit = "u" } },
            steps ?? new List<UpdateStepDto> { new() { StepNumber = 1, InstructionText = "s" } },
            userId);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRecipeNotFound()
    {
        var repo = new Mock<IRecipeRepository>();
        var img = new Mock<IImageStorageService>();
        var logger = Mock.Of<ILogger<UpdateRecipeCommandHandler>>();
        var handler = new UpdateRecipeCommandHandler(repo.Object, img.Object, logger);

        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe?)null);

        var res = await handler.Handle(NewCmd(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
        res.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAccessDenied()
    {
        var (repo, img, handler, recipe, userId) = SetupWithExistingRecipe();
        var otherUser = Guid.NewGuid();

        var res = await handler.Handle(NewCmd(recipe.Id, otherUser), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
        res.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_ShouldValidateRequiredFields()
    {
        var (repo, img, handler, recipe, userId) = SetupWithExistingRecipe();

        (await handler.Handle(NewCmd(recipe.Id, userId, title: " "), CancellationToken.None)).IsFailure.Should().BeTrue();
        (await handler.Handle(NewCmd(recipe.Id, userId, category: ""), CancellationToken.None)).IsFailure.Should().BeTrue();
        (await handler.Handle(NewCmd(recipe.Id, userId, ingredients: new()), CancellationToken.None)).IsFailure.Should().BeTrue();
        (await handler.Handle(NewCmd(recipe.Id, userId, steps: new()), CancellationToken.None)).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldValidateInvalidIngredientsAndSteps()
    {
        var (repo, img, handler, recipe, userId) = SetupWithExistingRecipe();

        var invalidIngredients = new List<UpdateIngredientDto> { new() { Name = "", Quantity = "1", Unit = "u" } };
        var invalidSteps = new List<UpdateStepDto> { new() { StepNumber = 0, InstructionText = "" } };

        var res1 = await handler.Handle(NewCmd(recipe.Id, userId, ingredients: invalidIngredients), CancellationToken.None);
        res1.IsFailure.Should().BeTrue();

        var res2 = await handler.Handle(NewCmd(recipe.Id, userId, steps: invalidSteps), CancellationToken.None);
        res2.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldAutoAssignStepNumbers_WhenInvalid()
    {
        var (repo, img, handler, recipe, userId) = SetupWithExistingRecipe();

        var steps = new List<UpdateStepDto>
        {
            new() { StepNumber = 0, InstructionText = "A" },
            new() { StepNumber = -1, InstructionText = "B" }
        };

        var res = await handler.Handle(NewCmd(recipe.Id, userId, steps: steps), CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
        res.Value.Steps.Select(s => s.StepNumber).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_ShouldUploadAndReplacePhoto_WhenProvided()
    {
        var (repo, img, handler, recipe, userId) = SetupWithExistingRecipe(photo: "old");
        img.Setup(i => i.UploadImageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("http://new")
            .Verifiable();
        img.Setup(i => i.DeleteImageAsync("old"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var res = await handler.Handle(NewCmd(recipe.Id, userId, photo: "base64"), CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
        res.Value.PhotoUrl.Should().Be("http://new");
        img.Verify();
    }

    [Fact]
    public async Task Handle_ShouldContinue_WhenImageUploadFails()
    {
        var (repo, img, handler, recipe, userId) = SetupWithExistingRecipe(photo: "old");
        img.Setup(i => i.UploadImageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("fail"));

        var res = await handler.Handle(NewCmd(recipe.Id, userId, photo: "base64"), CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
        res.Value.PhotoUrl.Should().Be("old");
    }

    private class DbUpdateConcurrencyException : Exception { public DbUpdateConcurrencyException(){} }

    [Fact]
    public async Task Handle_ShouldMapConcurrencyException_ToFailureMessage()
    {
        var (repo, img, handler, recipe, userId) = SetupWithExistingRecipe();
        repo.Setup(r => r.UpdateAsync(recipe, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var res = await handler.Handle(NewCmd(recipe.Id, userId), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
        res.Error.Should().Contain("modified by another user");
    }
}

