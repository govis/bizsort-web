using System;
using System.Text.Json.Serialization;
using BizSrt.Api.Model.Legacy;

namespace BizSrt.Api.Model.Job;

public enum JobType : short
{
    Full_time = 1,
    Part_time = 2
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

    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; set; }

    [JsonPropertyName("status")]
    public Status Status { get; set; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; }
}

public class Preview : IdName<long>
{
    [JsonPropertyName("company")]
    public Account? Company { get; set; }

    [JsonPropertyName("category")]
    public Category? Category { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("image")]
    public Image<long>? Image { get; set; }

    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; set; }
}
