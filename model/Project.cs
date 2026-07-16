using System;
using System.Text.Json.Serialization;
using BizSrt.Model;

namespace BizSrt.Model.Project;

public enum ProjectType : short
{
    Commercial = 1,
    Consumer = 2,
    Tender = 4
}

public enum TenderType : byte
{
    Complete = 0,
    Tender = 1
}

public enum UnlistedType : byte
{
    Listed = 0,
    Unlisted = 1,
    Private = 2
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

    [JsonPropertyName("tenderType")]
    public byte TenderType { get; set; }

    [JsonPropertyName("status")]
    public BizSrt.Model.Product.Status Status { get; set; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; }
}

public class Preview : IdName<long>
{
    [JsonPropertyName("category")]
    public Category? Category { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
}
