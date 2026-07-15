using System.Linq;
using System.Text.Json.Serialization;

namespace BizSrt.Model.Semantic;

public class FacetName
{
    [JsonPropertyName("key")]
    public short Key { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("values")]
    public FacetValue[] Values { get; set; } = Array.Empty<FacetValue>();
}

public class FacetValue
{
    [JsonPropertyName("key")]
    public int Key { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class Facet
{
    [JsonPropertyName("name")]
    public int Name { get; set; }

    [JsonPropertyName("nameText")]
    public string NameText { get; set; } = "";

    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("valueText")]
    public string ValueText { get; set; } = "";

    [JsonPropertyName("exclude")]
    public bool Exclude { get; set; }
}

public class FacetFilter
{
    public FacetFilter() : this(null) { }

    public FacetFilter(Facet[]? facets)
    {
        if (facets != null && facets.Length > 0)
        {
            NoFilters = facets.Length;
            FilterNames = facets.Select(f => f.Name).ToArray();
            FilterValues = facets.Select(f => f.Value).ToArray();
        }
        else
        {
            NoFilters = 0;
            FilterNames = Array.Empty<int>();
            FilterValues = Array.Empty<int>();
        }
    }

    public FacetFilter(Facet[]? facets, bool excluded)
    {
        if (facets != null)
        {
            var filters = facets.Where(f => f.Exclude == excluded).ToArray();

            if (filters.Length > 0)
            {
                NoFilters = filters.Length;
                FilterNames = filters.Select(f => f.Name).ToArray();
                FilterValues = filters.Select(f => f.Value).ToArray();
                return;
            }
        }

        NoFilters = 0;
        FilterNames = Array.Empty<int>();
        FilterValues = Array.Empty<int>();
    }

    [JsonPropertyName("noFilters")]
    public int NoFilters { get; set; }

    [JsonPropertyName("filterNames")]
    public int[] FilterNames { get; set; } = Array.Empty<int>();

    [JsonPropertyName("filterValues")]
    public int[] FilterValues { get; set; } = Array.Empty<int>();
}
