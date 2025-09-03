using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class RecipeRepository : IRecipeRepository
{
    private readonly RecipeAppDbContext _context;
    private readonly ILogger<RecipeRepository> _logger;

    public RecipeRepository(RecipeAppDbContext context, ILogger<RecipeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GetByIdAsync called - RecipeId: {RecipeId}", id);
        
        try
        {
            var recipe = await _context.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (recipe != null)
            {
                _logger.LogInformation("Recipe retrieved successfully - RecipeId: {RecipeId}, Title: {Title}, IngredientsCount: {IngredientsCount}, StepsCount: {StepsCount}", 
                    recipe.Id, recipe.Title, recipe.Ingredients.Count, recipe.Steps.Count);
            }
            else
            {
                _logger.LogWarning("Recipe not found in GetByIdAsync - RecipeId: {RecipeId}", id);
            }

            return recipe;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByIdAsync - RecipeId: {RecipeId}", id);
            throw;
        }
    }

    public async Task<PagedResult<Recipe>> GetRecipesAsync(
        string? category, 
        string? search, 
        int page, 
        int limit, 
        Guid? userId = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GetRecipesAsync called - Category: {Category}, Search: {Search}, Page: {Page}, Limit: {Limit}, UserId: {UserId}", 
            category, search, page, limit, userId);

        try
        {
            var query = _context.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Steps)
                .AsQueryable();

            _logger.LogDebug("Base query created, applying filters...");

            // Apply filters
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(r => r.Category.ToLower() == category.ToLower());
                _logger.LogDebug("Applied category filter: {Category}", category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.ToLower();
                query = query.Where(r => 
                    r.Title.ToLower().Contains(searchTerm) ||
                    r.Description.ToLower().Contains(searchTerm));
                _logger.LogDebug("Applied search filter: {Search}", search);
            }

            // filter by user
            query = query.Where(r => r.UserId == userId);

            // Get total count
            var total = await query.CountAsync(cancellationToken);
            _logger.LogInformation("Total recipes found after filters: {Total}", total);

            // Apply pagination
            var recipes = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} recipes for page {Page}", recipes.Count, page);

            var result = new PagedResult<Recipe>
            {
                Items = recipes,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    Limit = limit,
                    Total = total,
                    TotalPages = (int)Math.Ceiling((double)total / limit)
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetRecipesAsync");
            throw;
        }
    }

    public async Task<int> GetTotalRecipeCountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting total recipe count from database");
        
        try
        {
            var count = await _context.Recipes.CountAsync(cancellationToken);
            _logger.LogInformation("Total recipe count in database: {Count}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total recipe count");
            throw;
        }
    }

    public async Task<bool> RecipeExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if recipe exists - RecipeId: {RecipeId}", id);
        
        try
        {
            var exists = await _context.Recipes.AnyAsync(r => r.Id == id, cancellationToken);
            _logger.LogInformation("Recipe existence check - RecipeId: {RecipeId}, Exists: {Exists}", id, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recipe existence - RecipeId: {RecipeId}", id);
            throw;
        }
    }

    public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding recipe: {Title}, UserId: {UserId}, Id: {Id}", recipe.Title, recipe.UserId, recipe.Id);
        
        try
        {
            await _context.Recipes.AddAsync(recipe, cancellationToken);
            var changes = await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Recipe saved successfully. Changes: {Changes}, RecipeId: {RecipeId}", changes, recipe.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving recipe: {Title}, UserId: {UserId}", recipe.Title, recipe.UserId);
            throw;
        }
    }

    public async Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("UpdateAsync called - RecipeId: {RecipeId}, Title: {Title}", recipe.Id, recipe.Title);
        
        try
        {
            // Get the existing recipe with related entities
            var existingRecipe = await _context.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.Id == recipe.Id, cancellationToken);

            if (existingRecipe == null)
            {
                _logger.LogError("Recipe not found for update - RecipeId: {RecipeId}", recipe.Id);
                throw new InvalidOperationException($"Recipe with ID {recipe.Id} not found");
            }

            // Update basic properties
            _context.Entry(existingRecipe).CurrentValues.SetValues(recipe);

            // Handle ingredients - remove old ones and add new ones
            _context.Ingredients.RemoveRange(existingRecipe.Ingredients);
            foreach (var ingredient in recipe.Ingredients)
            {
                _context.Ingredients.Add(ingredient);
            }

            // Handle steps - remove old ones and add new ones
            _context.Steps.RemoveRange(existingRecipe.Steps);
            foreach (var step in recipe.Steps)
            {
                _context.Steps.Add(step);
            }

            var changes = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Recipe updated successfully - RecipeId: {RecipeId}, Changes: {Changes}", recipe.Id, changes);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict during recipe update - RecipeId: {RecipeId}", recipe.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating recipe - RecipeId: {RecipeId}", recipe.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsRecipeFavoriteAsync(Guid recipeId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRecipeFavorites
            .AnyAsync(f => f.RecipeId == recipeId && f.UserId == userId, cancellationToken);
    }

    public async Task AddFavoriteAsync(Guid recipeId, Guid userId, CancellationToken cancellationToken = default)
    {
        var favorite = new UserRecipeFavorite(userId, recipeId);
        await _context.UserRecipeFavorites.AddAsync(favorite, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFavoriteAsync(Guid recipeId, Guid userId, CancellationToken cancellationToken = default)
    {
        var favorite = await _context.UserRecipeFavorites
            .FirstOrDefaultAsync(f => f.RecipeId == recipeId && f.UserId == userId, cancellationToken);
        
        if (favorite != null)
        {
            _context.UserRecipeFavorites.Remove(favorite);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}