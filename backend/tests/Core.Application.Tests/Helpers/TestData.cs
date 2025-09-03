using Core.Domain.Entities;

namespace Core.Application.Tests.Helpers;

internal static class TestData
{
    public static Recipe NewRecipe(
        string title = "Title",
        string description = "Desc",
        string category = "Cat",
        Guid? userId = null,
        string? photo = null)
    {
        return new Recipe(title, description, category, userId ?? Guid.NewGuid(), photo);
    }
}

