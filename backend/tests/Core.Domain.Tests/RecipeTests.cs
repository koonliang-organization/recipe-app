using Core.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Core.Domain.Tests;

public class RecipeTests
{
    private static Recipe NewRecipe(Guid? userId = null)
        => new("Pasta", "Tasty", "Dinner", userId ?? Guid.NewGuid());

    [Fact]
    public void UpdateBasicInfo_ShouldChangeFields_AndUpdatedAt()
    {
        var r = NewRecipe();
        r.UpdatedAt.Should().BeNull();

        r.UpdateBasicInfo("Pizza", "Nice", "Lunch", "img");

        r.Title.Should().Be("Pizza");
        r.Description.Should().Be("Nice");
        r.Category.Should().Be("Lunch");
        r.PhotoUrl.Should().Be("img");
        r.UpdatedAt.Should().NotBeNull();
        r.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddIngredient_ShouldAppend_AndSetUpdatedAt()
    {
        var r = NewRecipe();
        r.AddIngredient("Salt", "1", "tsp");

        r.Ingredients.Should().HaveCount(1);
        r.Ingredients[0].Name.Should().Be("Salt");
        r.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateIngredients_ShouldReplaceAll_AndSetUpdatedAt()
    {
        var r = NewRecipe();
        r.AddIngredient("Salt", "1", "tsp");

        var newList = new List<Ingredient>
        {
            new("Flour", "200", "g", r.Id),
            new("Water", "100", "ml", r.Id)
        };

        r.UpdateIngredients(newList);

        r.Ingredients.Should().HaveCount(2);
        r.Ingredients.Select(i => i.Name).Should().BeEquivalentTo(new[] {"Flour", "Water"});
        r.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AddStep_ShouldAppend_AndSetUpdatedAt()
    {
        var r = NewRecipe();
        r.AddStep(2, "Mix");
        r.AddStep(1, "Prepare");

        r.Steps.Should().HaveCount(2);
        r.Steps.Any(s => s.StepNumber == 1 && s.InstructionText == "Prepare").Should().BeTrue();
        r.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateSteps_ShouldReplaceAll_AndSetUpdatedAt()
    {
        var r = NewRecipe();
        r.AddStep(1, "Old");

        var newSteps = new List<Step>
        {
            new(2, "Mix", r.Id),
            new(1, "Prep", r.Id)
        };

        r.UpdateSteps(newSteps);

        r.Steps.Should().HaveCount(2);
        r.Steps.Select(s => s.StepNumber).Should().BeEquivalentTo(new[] {2,1});
        r.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void IsOwnedBy_ShouldReturnExpected()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var r = NewRecipe(owner);

        r.IsOwnedBy(owner).Should().BeTrue();
        r.IsOwnedBy(other).Should().BeFalse();
    }
}

