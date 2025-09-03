using Core.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Core.Domain.Tests;

public class IngredientAndStepTests
{
    [Fact]
    public void Ingredient_Update_ShouldModifyFields_AndTimestamp()
    {
        var rId = Guid.NewGuid();
        var ing = new Ingredient("Salt", "1", "tsp", rId);
        ing.UpdatedAt.Should().BeNull();

        ing.Update("Sugar", "2", "tbsp");

        ing.Name.Should().Be("Sugar");
        ing.Quantity.Should().Be("2");
        ing.Unit.Should().Be("tbsp");
        ing.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Step_Update_ShouldModifyFields_AndTimestamp()
    {
        var rId = Guid.NewGuid();
        var step = new Step(1, "Prep", rId);
        step.UpdatedAt.Should().BeNull();

        step.Update(2, "Mix");

        step.StepNumber.Should().Be(2);
        step.InstructionText.Should().Be("Mix");
        step.UpdatedAt.Should().NotBeNull();
    }
}

