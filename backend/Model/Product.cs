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

    [Flags]
    public enum ItemType : short
    {
        Multiproduct = 0,
        Product = 1,
        Service = 2,
        Product_or_Service = 3,
        MarketplaceProduct = 4,
        PromotionProduct = 8,
        Job = 16,
        Other = 16384
    }
}

public enum UnlistedType : byte
{
    Listed = 0,
    Unlisted = 1
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

    [JsonExtensionData]
    public Dictionary<string, object> Properties { get; set; } = new();

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

    [JsonPropertyName("company")]
    public Account? Company
    {
        get => Properties.TryGetValue("company", out var val) ? val as Account : null;
        set { if (value != null) Properties["company"] = value; else Properties.Remove("company"); }
    }

    [JsonIgnore]
    public string? Distance
    {
        get => Properties.TryGetValue("distance", out var val) ? val as string : null;
        set { if (value != null) Properties["distance"] = value; else Properties.Remove("distance"); }
    }

    [JsonIgnore]
    public UnlistedType UnlistedType
    {
        get => Properties.TryGetValue("unlistedType", out var val) && val is UnlistedType utVal ? utVal : UnlistedType.Listed;
        set => Properties["unlistedType"] = value;
    }

    [JsonIgnore]
    public Status Status
    {
        get => Properties.TryGetValue("status", out var val) && val is Status sVal ? sVal : Status.Pending;
        set => Properties["status"] = value;
    }
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
