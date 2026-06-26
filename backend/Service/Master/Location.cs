using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using BizSrt.Api.Data.Cache;
using BizSrt.Api.Model;
using BizSrt.Api.Model.Group;
using System;

namespace BizSrt.Api.Service.Master;

public interface ILocationService
{
    Task<Autocomplete<int>[]> AutocompleteAsync(int parentLocation, string name, string? scopeInput);
    Task<BizSrt.Api.Model.ResolvedLocation> ResolveAsync(BizSrt.Api.Model.Geocoder.City city, string street, bool allowCreate);
    Task<BizSrt.Api.Model.Group.IdName<int>> GetAsync(int location);
    Task<BizSrt.Api.Model.LocationRef> GetRefAsync(int location, BizSrt.Api.Model.Group.DisplayType type);
    Task<BizSrt.Api.Model.Group.IdName<int>[]> GetPathAsync(int location, string? scopeInput);
    Task<BizSrt.Api.Model.Group.Node<int>> PopulateWithChildrenAsync(int parent, BizSrt.Api.Model.Group.SubType type);
    Task<BizSrt.Api.Model.Location> PopulateWithPathAsync(int location);
    Task<BizSrt.Api.Model.Location> PopulateWithPathAsync(int city, int street);
}

public class ResolveRequest
{
    public BizSrt.Api.Model.Geocoder.City City { get; set; } = new();
    public string Street { get; set; } = string.Empty;
    public bool AllowCreate { get; set; }
}

public class LocationService : ILocationService
{
    public Task<Autocomplete<int>[]> AutocompleteAsync(int parentLocation, string name, string? scopeInput)
    {
        BizSrt.Api.Model.Group.IdName<int>? scope = null;
        if (!string.IsNullOrWhiteSpace(scopeInput))
        {
            try { scope = JsonSerializer.Deserialize<BizSrt.Api.Model.Group.IdName<int>>(scopeInput, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); } catch { }
        }

        if (parentLocation >= 0 && !string.IsNullOrWhiteSpace(name))
        {
            var locationSearchCacheKey = new BizSrt.Api.Foundation.Cache.GroupSearchCache<int> { Parent = parentLocation, Name = name };
            var locations = LegacyCache.LocationSearch[locationSearchCacheKey].Take(15);
            
            var result = (from l in locations
                    let location = LegacyCache.Locations[l]
                    select new Autocomplete<int>
                    {
                        Id = l,
                        Name = location.Name,
                        Path = location.AutocompletePath(scope != null ? scope.Id : 0),
                        NodeType = location.NodeType(0),
                        HasChildren = LegacyCache.Locations.HasChildren(l, 0)
                    }).ToArray();
                    
            return Task.FromResult(result);
        }
        else
        {
            throw new ArgumentException("Invalid parent location or name.");
        }
    }

    public Task<BizSrt.Api.Model.ResolvedLocation> ResolveAsync(BizSrt.Api.Model.Geocoder.City city, string street, bool allowCreate)
    {
        if (city != null && !string.IsNullOrWhiteSpace(city.Country))
        {
            return Task.FromResult(BizSrt.Api.Data.Master.Location.Resolve(city, street, allowCreate));
        }
        else
            throw new ArgumentException("Invalid city.");
    }

    public Task<BizSrt.Api.Model.Group.IdName<int>> GetAsync(int location)
    {
        if (location > 0)
        {
            return Task.FromResult(LegacyCache.Locations[location].IdName);
        }
        else
            throw new ArgumentException("Invalid location.");
    }

    public Task<BizSrt.Api.Model.LocationRef> GetRefAsync(int location, BizSrt.Api.Model.Group.DisplayType type)
    {
        if (location > 0)
        {
            return Task.FromResult(LegacyCache.Locations[location].EntityRef(type));
        }
        else
            throw new ArgumentException("Invalid location.");
    }

    public Task<BizSrt.Api.Model.Group.IdName<int>[]> GetPathAsync(int location, string? scopeInput)
    {
        BizSrt.Api.Model.Group.IdName<int>? scope = null;
        if (!string.IsNullOrWhiteSpace(scopeInput))
        {
            try { scope = JsonSerializer.Deserialize<BizSrt.Api.Model.Group.IdName<int>>(scopeInput, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); } catch { }
        }

        if (location > 0)
        {
            return Task.FromResult(LegacyCache.Locations[location].GetPath(scope));
        }
        else
            throw new ArgumentException("Invalid location.");
    }

    public Task<BizSrt.Api.Model.Group.Node<int>> PopulateWithChildrenAsync(int parent, BizSrt.Api.Model.Group.SubType type)
    {
        return Task.FromResult(BizSrt.Api.Foundation.Cache.CachedNode<int>.PopulateWithChildren(parent, type, 0, LegacyCache.Locations));
    }

    public Task<BizSrt.Api.Model.Location> PopulateWithPathAsync(int location)
    {
        if (location > 0)
        {
            return Task.FromResult(BizSrt.Api.Data.Master.Location.PopulateWithPath(location));
        }
        else
            throw new ArgumentException("Invalid location.");
    }

    public Task<BizSrt.Api.Model.Location> PopulateWithPathAsync(int city, int street)
    {
        return Task.FromResult(BizSrt.Api.Data.Master.Location.PopulateWithPath(city, street));
    }
}

public static class LocationEndpoints
{
    public static void MapLocationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/location").WithTags("Location");

        group.MapGet("/autocomplete", async ([FromQuery] int parent, [FromQuery] string name, [FromQuery] string? scope, ILocationService locationService) =>
        {
            try
            {
                var result = await locationService.AutocompleteAsync(parent, name, scope);
                return Results.Ok(result);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }
        });

        group.MapPost("/resolve", async ([FromBody] ResolveRequest req, [FromServices] ILocationService locationService) =>
        {
            var res = await locationService.ResolveAsync(req.City, req.Street, req.AllowCreate);
            return Results.Ok(res);
        });

        group.MapGet("/get", async ([FromQuery] int location, ILocationService locationService) =>
        {
            try
            {
                var result = await locationService.GetAsync(location);
                return Results.Ok(result);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }
        });

        group.MapGet("/get_Ref", async ([FromQuery] int location, [FromQuery] BizSrt.Api.Model.Group.DisplayType type, ILocationService locationService) =>
        {
            try
            {
                var result = await locationService.GetRefAsync(location, type);
                return Results.Ok(result);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }
        });

        group.MapGet("/getPath", async ([FromQuery] int location, [FromQuery] string? scope, ILocationService locationService) =>
        {
            try
            {
                var result = await locationService.GetPathAsync(location, scope);
                return Results.Ok(result);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }
        });

        group.MapGet("/populate_Children", async ([FromQuery] int parent, [FromQuery] BizSrt.Api.Model.Group.SubType type, ILocationService locationService) =>
        {
            var result = await locationService.PopulateWithChildrenAsync(parent, type);
            return Results.Ok(result);
        });

        group.MapGet("/populate_Path", async ([FromQuery] int location, [FromServices] ILocationService locationService) =>
        {
            var res = await locationService.PopulateWithPathAsync(location);
            return Results.Ok(res);
        });

        group.MapGet("/populate_Path_Street", async ([FromQuery] int city, [FromQuery] int street, [FromServices] ILocationService locationService) =>
        {
            var res = await locationService.PopulateWithPathAsync(city, street);
            return Results.Ok(res);
        });
    }
}
