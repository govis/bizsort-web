using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using BizSrt.Api.Data.Cache;
using BizSrt.Model;
using BizSrt.Model.Group;
using System;

namespace BizSrt.Api.Service.Master;

public interface ICategoryService
{
    Task<Autocomplete<short>[]> AutocompleteAsync(short parentCategory, string name, string? scopeInput);
    Task<BizSrt.Model.Group.IdName<short>> GetAsync(short category);
    Task<BizSrt.Model.Group.Node<short>> PopulateWithChildrenAsync(short parentCategory, BizSrt.Model.Group.SubType type, BizSrt.Model.Category.MemberType memberType);
    Task<BizSrt.Model.Group.NodeRef<short>[]> GetChildrenAsync(short parentCategory, short lookupCategory);
    Task<BizSrt.Model.Group.IdName<short>[]> GetPathAsync(short category, string? scopeInput);
}

public class CategoryService : ICategoryService
{
    public Task<Autocomplete<short>[]> AutocompleteAsync(short parentCategory, string name, string? scopeInput)
    {
        BizSrt.Model.Group.IdName<short>? scope = null;
        if (!string.IsNullOrWhiteSpace(scopeInput))
        {
            try { scope = JsonSerializer.Deserialize<BizSrt.Model.Group.IdName<short>>(scopeInput, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); } catch { }
        }

        if (parentCategory >= 0 && !string.IsNullOrWhiteSpace(name))
        {
            var categorySearchCacheKey = new BizSrt.Foundation.Cache.GroupSearchCache<short> { Parent = parentCategory, Name = name };
            var categories = LegacyCache.CategorySearch[categorySearchCacheKey].Take(15);
            
            var result = (from c in categories
                    let category = LegacyCache.Categories[c]
                    select new Autocomplete<short>
                    {
                        Id = c,
                        Name = category.Name,
                        Path = category.AutocompletePath(scope != null ? scope.Id : (short)0),
                        NodeType = category.NodeType(0),
                        HasChildren = LegacyCache.Categories.HasChildren(c, 0)
                    }).ToArray();
                    
            return Task.FromResult(result);
        }
        else
        {
            throw new ArgumentException("Invalid parent category or name.");
        }
    }

    public Task<BizSrt.Model.Group.IdName<short>> GetAsync(short category)
    {
        if (category > 0)
            return Task.FromResult(LegacyCache.Categories[category].IdName);
        else
            throw new ArgumentException("Invalid category.");
    }

    public Task<BizSrt.Model.Group.Node<short>> PopulateWithChildrenAsync(short parentCategory, BizSrt.Model.Group.SubType type, BizSrt.Model.Category.MemberType memberType)
    {
        if (parentCategory >= 0)
            return Task.FromResult(BizSrt.Foundation.Cache.CachedNode<short>.PopulateWithChildren(parentCategory, type, (byte)memberType, LegacyCache.Categories));
        else
            throw new ArgumentException("Invalid parent category.");
    }

    public Task<BizSrt.Model.Group.NodeRef<short>[]> GetChildrenAsync(short parentCategory, short lookupCategory)
    {
        return Task.FromResult(LegacyCache.Categories.GetChildren(parentCategory, lookupCategory));
    }

    public Task<BizSrt.Model.Group.IdName<short>[]> GetPathAsync(short category, string? scopeInput)
    {
        BizSrt.Model.Group.IdName<short>? scope = null;
        if (!string.IsNullOrWhiteSpace(scopeInput))
        {
            try { scope = JsonSerializer.Deserialize<BizSrt.Model.Group.IdName<short>>(scopeInput, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); } catch { }
        }

        if (category > 0)
            return Task.FromResult(LegacyCache.Categories[category].GetPath(scope));
        else
            throw new ArgumentException("Invalid category.");
    }
}

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/category").WithTags("Category");

        group.MapGet("/autocomplete", async ([FromQuery] short parent, [FromQuery] string name, [FromQuery] string? scope, ICategoryService categoryService) =>
        {
            try
            {
                var result = await categoryService.AutocompleteAsync(parent, name, scope);
                return Results.Ok(result);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }
        });

        group.MapGet("/get", async ([FromQuery] short category, ICategoryService categoryService) =>
        {
            try
            {
                var result = await categoryService.GetAsync(category);
                return Results.Ok(result);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }
        });

        group.MapGet("/populate_Children", async ([FromQuery] short parent, [FromQuery] BizSrt.Model.Group.SubType type, [FromQuery] BizSrt.Model.Category.MemberType memberType, ICategoryService categoryService) =>
        {
            try
            {
                var result = await categoryService.PopulateWithChildrenAsync(parent, type, memberType);
                return Results.Ok(result);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }
        });

        group.MapGet("/getChildren", async ([FromQuery] short parentCategory, [FromQuery] short lookupCategory, ICategoryService categoryService) =>
        {
            var result = await categoryService.GetChildrenAsync(parentCategory, lookupCategory);
            return Results.Ok(result);
        });

        group.MapGet("/getPath", async ([FromQuery] short category, [FromQuery] string? scope, ICategoryService categoryService) =>
        {
            try
            {
                var result = await categoryService.GetPathAsync(category, scope);
                return Results.Ok(result);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }
        });
    }
}
