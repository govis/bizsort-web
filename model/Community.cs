using System;
using System.Text.Json.Serialization;

namespace BizSrt.Model.Community;

public class Option
{
    [Flags]
    public enum Flags : int
    {
        Publish_Membership = 1,
        Allow_Unlisted = 2,
        Require_Moderation = 4,
        Exclusive = 8,
        Auto_Publish_Products = 16,
        Auto_Publish_Jobs = 32,
        Auto_Publish_Projects = 64
    }

    public class Set 
    {
        [JsonPropertyName("value")]
        public Flags Value { get; set; }

        public bool Publish_Membership => (Value & Flags.Publish_Membership) > 0;
        public bool Allow_Unlisted => (Value & Flags.Allow_Unlisted) > 0;
        public bool Require_Moderation => (Value & Flags.Require_Moderation) > 0;
        public bool Exclusive => (Value & Flags.Exclusive) > 0;
        public bool Auto_Publish_Products => (Value & Flags.Auto_Publish_Products) > 0;
        public bool Auto_Publish_Jobs => (Value & Flags.Auto_Publish_Jobs) > 0;
        public bool Auto_Publish_Projects => (Value & Flags.Auto_Publish_Projects) > 0;
    }
}

public enum UnlistedType : byte
{
    Listed = 0,
    Unlisted = 1,
    Private = 2
}

public class Preview
{
    [JsonPropertyName("type")]
    public UnlistedType Type { get; set; }
    
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("location")]
    public Location? Location { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    
    [JsonPropertyName("image")]
    public Image<int>? Image { get; set; }
    
    [JsonPropertyName("options")]
    public Option.Set? Options { get; set; }
}

public class Profile : IdName<int>
{
    [JsonPropertyName("owner")]
    public byte Owner { get; set; } // Account_Type (e.g. from Company or Personal)

    [JsonPropertyName("location")]
    public Location? Location { get; set; }

    [JsonPropertyName("image")]
    public Image<int>? Image { get; set; }

    [JsonPropertyName("richText")]
    public string? RichText { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("options")]
    public Option.Set? Options { get; set; }

    [JsonPropertyName("defaultCategory")]
    public string? DefaultCategory { get; set; }
}
