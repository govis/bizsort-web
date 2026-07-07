using System;
using System.Text.Json.Serialization;
using BizSrt.Api.Model;

namespace BizSrt.Api.Model.Product;

public class ProductType
{
    [JsonPropertyName("itemKey")]
    public short ItemKey { get; set; }
    [JsonPropertyName("itemText")]
    public string ItemText { get; set; } = string.Empty;
}

public class Profile
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("richText")]
    public string? RichText { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; set; }
    
    [JsonPropertyName("type")]
    public ProductType? Type { get; set; }

    [JsonPropertyName("status")]
    public Status Status { get; set; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; }
}

public class Preview : IdName<long>
{
    [JsonPropertyName("type")]
    public ProductType? Type { get; set; }

    [JsonPropertyName("category")]
    public Category? Category { get; set; }

    [JsonPropertyName("image")]
    public Image<long>? Image { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
}

public class SearchInput : BizSrt.Api.Model.List.QueryInput
{
    [JsonPropertyName("productType")]
    public short ProductType { get; set; }

    [JsonPropertyName("category")]
    public short Category { get; set; }

    [JsonPropertyName("location")]
    public int Location { get; set; }

    [JsonPropertyName("searchNear")]
    public Geolocation? SearchNear { get; set; }

    [JsonPropertyName("inclFacets")]
    public BizSrt.Api.Model.Semantic.FacetFilter? InclFacets { get; set; }

    [JsonPropertyName("exclFacets")]
    public BizSrt.Api.Model.Semantic.FacetFilter? ExclFacets { get; set; }
}

public class SearchItem : EntityId<long>
{
    [JsonPropertyName("distance")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public float Distance { get; set; }
}
