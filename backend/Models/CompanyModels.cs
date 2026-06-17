using System.Text.Json.Serialization;
using BizSrt.Api.Models.Legacy;

namespace BizSrt.Api.Models.Company;

public class Page_Offerings
{
    [JsonPropertyName("view")]
    public ProductsView View { get; set; }
    
    [JsonPropertyName("multiProduct")]
    public string? MultiProduct { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("hideOfferings")]
    public bool HideOfferings { get; set; }
}

public class Page_Projects
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }
}

public class Page_Promotions
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }
}

public class Page_News
{
    [JsonPropertyName("community")]
    public int Community { get; set; }
    [JsonPropertyName("label")]
    public string? Label { get; set; }
}

public class Page_Articles
{
    [JsonPropertyName("community")]
    public int Community { get; set; }
    [JsonPropertyName("label")]
    public string? Label { get; set; }
    [JsonPropertyName("defaultCategory")]
    public string? DefaultCategory { get; set; }
}

public class Page_Jobs
{
    [JsonPropertyName("organization")]
    public int Organization { get; set; }
    [JsonPropertyName("label")]
    public string? Label { get; set; }
    [JsonPropertyName("defaultDepartment")]
    public string? DefaultDepartment { get; set; }
}

public class Page_Marketplace
{
    [JsonPropertyName("marketplace")]
    public int Marketplace { get; set; }
    [JsonPropertyName("label")]
    public string? Label { get; set; }
}

public class Office
{
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;
    [JsonPropertyName("phone1")]
    public string? Phone1 { get; set; }
    [JsonPropertyName("fax")]
    public string? Fax { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("location")]
    public Location? Location { get; set; }
}

public class Profile : Account
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    [JsonPropertyName("webSite")]
    public string? WebSite { get; set; }
    [JsonPropertyName("richText")]
    public string? RichText { get; set; }
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("description")]
    public string? Description => Text;

    [JsonPropertyName("category")]
    public Category? Category { get; set; }

    [JsonPropertyName("offices")]
    public Office[] Offices { get; set; } = Array.Empty<Office>();

    [JsonPropertyName("offerings")]
    public Page_Offerings? Offerings { get; set; }
    
    [JsonPropertyName("projects")]
    public Page_Projects? Projects { get; set; }
    
    [JsonPropertyName("promotions")]
    public Page_Promotions? Promotions { get; set; }
    
    [JsonPropertyName("hasAffiliations")]
    public bool HasAffiliations { get; set; }
    
    [JsonPropertyName("hasCommunities")]
    public bool HasCommunities { get; set; }

    [JsonPropertyName("news")]
    public Page_News? News { get; set; }
    [JsonPropertyName("articles")]
    public Page_Articles? Articles { get; set; }
    [JsonPropertyName("jobs")]
    public Page_Jobs? Jobs { get; set; }
    [JsonPropertyName("marketplace")]
    public Page_Marketplace? Marketplace { get; set; }

    [JsonPropertyName("appUri")]
    public string? AppUri { get; set; }
}

public class SearchInput : BizSrt.Api.Models.Legacy.List.QueryInput
{
    [JsonPropertyName("transactionType")]
    public short TransactionType { get; set; }

    [JsonPropertyName("category")]
    public short Category { get; set; }

    [JsonPropertyName("location")]
    public int Location { get; set; }

    [JsonPropertyName("searchNear")]
    public Geolocation? SearchNear { get; set; }

    [JsonPropertyName("searchQuery")]
    public string? SearchQuery { get; set; }
}

public class SearchItem : EntityId<int>
{

    [JsonPropertyName("office")]
    public int? Office { get; set; }
    [JsonPropertyName("distance")]
    public float Distance { get; set; }
}

public class Preview : IdName<int>
{
    [JsonPropertyName("image")]
    public Image<int>? Image { get; set; }
    [JsonPropertyName("location")]
    public Location? Location { get; set; }
    [JsonPropertyName("webSite")]
    public string? WebSite { get; set; }
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    [JsonPropertyName("productsView")]
    public ProductsView ProductsView { get; set; }
    [JsonPropertyName("category")]
    public Category? Category { get; set; }
}
