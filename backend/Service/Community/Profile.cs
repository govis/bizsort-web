using Microsoft.AspNetCore.Mvc;
using BizSrt.Model;
using BizSrt.Model.Community;
using System.Text.Json;

namespace BizSrt.Api.Service.Community;

public static class CommunityEndpoints
{
    public static void MapCommunityEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/community").WithTags("Community");

        group.MapGet("/profile/toPreview", async ([FromQuery] string? communities) =>
        {
            if (string.IsNullOrWhiteSpace(communities)) return Results.Ok(Array.Empty<Preview>());
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var input = JsonSerializer.Deserialize<EntityId<int>[]>(communities, options) ?? Array.Empty<EntityId<int>>();
            
            var result = input.Select(c => 
            {
                var cached = BizSrt.Api.Data.Cache.LegacyCache.Communities[c.Id];
                return cached?.ToPreview();
            }).Where(p => p != null).ToArray();

            return Results.Ok(result);
        });

        group.MapGet("/profile/view", ([FromQuery] int community) =>
        {
            if (community <= 0) return Results.BadRequest();

            var cachedCommunity = BizSrt.Api.Data.Cache.LegacyCache.Communities[community];
            if (cachedCommunity == null) return Results.NotFound();

            var model = new BizSrt.Model.Community.Profile
            {
                Id = cachedCommunity.Id,
                Name = cachedCommunity.Name,
                RichText = cachedCommunity.RichText,
                Text = cachedCommunity.Text,
                Options = cachedCommunity.Options,
                DefaultCategory = cachedCommunity.DefaultCategory,
                Image = cachedCommunity.Image,
                Location = cachedCommunity.Address,
                Owner = 0 // Placeholder, as Account_Type is not in modern CachedCompanyProfile
            };

            return Results.Ok(model);
        });
    }
}
