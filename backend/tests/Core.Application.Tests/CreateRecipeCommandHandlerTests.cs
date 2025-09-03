using BuildingBlocks.Common;
using Core.Application.Commands.Recipe;
using Core.Application.DTOs;
using Core.Application.Handlers.Recipe;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace Core.Application.Tests;

public class CreateRecipeCommandHandlerTests
{
    private static CreateRecipeCommand NewCommand(
        string? title = "T",
        string? category = "C",
        string? description = "D",
        string? photo = null,
        Guid? userId = null)
    {
        return new CreateRecipeCommand(
            title ?? string.Empty,
            description ?? string.Empty,
            category ?? string.Empty,
            photo,
            new List<CreateIngredientDto> { new() { Name = "i", Quantity = "1", Unit = "u" } },
            new List<CreateStepDto> { new() { StepNumber = 1, InstructionText = "s" } },
            userId ?? Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTitleMissing()
    {
        var repo = new Mock<IRecipeRepository>(MockBehavior.Strict);
        var img = new Mock<IImageStorageService>(MockBehavior.Strict);
        var handler = new CreateRecipeCommandHandler(repo.Object, img.Object);

        var result = await handler.Handle(NewCommand(title: " "), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Title is required");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCategoryMissing()
    {
        var repo = new Mock<IRecipeRepository>(MockBehavior.Strict);
        var img = new Mock<IImageStorageService>(MockBehavior.Strict);
        var handler = new CreateRecipeCommandHandler(repo.Object, img.Object);

        var result = await handler.Handle(NewCommand(category: ""), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Category is required");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenNoIngredients()
    {
        var repo = new Mock<IRecipeRepository>(MockBehavior.Strict);
        var img = new Mock<IImageStorageService>(MockBehavior.Strict);
        var handler = new CreateRecipeCommandHandler(repo.Object, img.Object);

        var cmd = NewCommand();
        cmd.Ingredients.Clear();

        var result = await handler.Handle(cmd, CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("ingredient");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenNoSteps()
    {
        var repo = new Mock<IRecipeRepository>(MockBehavior.Strict);
        var img = new Mock<IImageStorageService>(MockBehavior.Strict);
        var handler = new CreateRecipeCommandHandler(repo.Object, img.Object);

        var cmd = NewCommand();
        cmd.Steps.Clear();

        var result = await handler.Handle(cmd, CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("step");
    }

    [Fact]
    public async Task Handle_ShouldCreateRecipe_WithoutPhoto()
    {
        var repo = new Mock<IRecipeRepository>();
        var img = new Mock<IImageStorageService>(MockBehavior.Strict);
        repo.Setup(r => r.AddAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var handler = new CreateRecipeCommandHandler(repo.Object, img.Object);

        var result = await handler.Handle(NewCommand(photo: null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        repo.Verify();
        img.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_ShouldUploadPhoto_WhenProvided()
    {
        var repo = new Mock<IRecipeRepository>();
        var img = new Mock<IImageStorageService>();

        repo.Setup(r => r.AddAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        img.Setup(i => i.UploadImageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("http://img")
            .Verifiable();

        var handler = new CreateRecipeCommandHandler(repo.Object, img.Object);
        var result = await handler.Handle(NewCommand(photo: "base64"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        img.Verify();
    }
}

