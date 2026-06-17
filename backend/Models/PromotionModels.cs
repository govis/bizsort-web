using System;
using System.Text.Json.Serialization;
using BizSrt.Api.Models.Legacy;

namespace BizSrt.Api.Models.Promotion;

public class Profile : IdName<long>
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class Preview : IdName<long>
{
    [JsonPropertyName("image")]
    public Image<int>? Image { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
