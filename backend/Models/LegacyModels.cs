using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BizSrt.Api.Models.Legacy
{
    public enum Status : byte
    {
        Guest = 0,
        Pending = 1,
        Active = 2,
        Rejected = 3,
        Disabled = 4,
        Locked = 5,
        Deleted = 6
    }

    public enum AccountType : byte
    {
        Person = 1,
        Company = 2,
        Community = 3,
        Organization = 4
    }

    public class EntityId<T>
    {
        [JsonPropertyName("id")]
        public T Id { get; set; } = default!;
    }

    public class IdName<T> : EntityId<T>
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class AccountId
    {
        [JsonPropertyName("accountType")]
        public AccountType AccountType { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class AccountName : AccountId
    {
        public AccountName() : base() { }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class Account : AccountName
    {
        [JsonPropertyName("image")]
        public Image<int>? Image { get; set; }
    }

    public enum ImageEntity : byte
    {
        Person = 1,
        Company = 2,
        Community = 3,
        Product = 4,
        Project = 5,
        Job = 6,
        Promotion = 7
    }

    public enum ImageSizeType : byte
    {
        None = 0,
        Icon = 1,
        List = 2,
        Card = 3,
        View = 4
    }

    public class Image<T> where T : IComparable
    {
        [JsonPropertyName("entity")]
        public ImageEntity Entity { get; set; }
        [JsonPropertyName("maxImageSize")]
        public ImageSizeType MaxImageSize { get; set; }
        [JsonPropertyName("imageId")]
        public T ImageId { get; set; } = default!;
    }

    public class Geolocation
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }
        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    public class Location
    {
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;
        [JsonPropertyName("geoLocation")]
        public Geolocation? GeoLocation { get; set; }
    }

    public class Category : IdName<short>
    {
        public const short Uncategorized = 0;
        public const short AllCategories = -1;
    }

    public enum ProductsView : byte
    {
        NoProducts = 0,
        Multiproduct = 1,
        ProductList = 2,
        Marketplace = 3
    }

    namespace List
    {
        public class QueryInput
        {
            [JsonPropertyName("startIndex")]
            public int StartIndex { get; set; }
            [JsonPropertyName("length")]
            public int Length { get; set; }
        }

        public class QueryOutput<T>
        {
            [JsonPropertyName("startIndex")]
            public int StartIndex { get; set; }
            [JsonPropertyName("series")]
            public T[] Series { get; set; } = Array.Empty<T>();
            [JsonPropertyName("totalCount")]
            public int TotalCount { get; set; }
        }

        public class SliceInput
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }
            [JsonPropertyName("length")]
            public int Length { get; set; }
        }

        public class SliceOutput<T>
        {
            [JsonPropertyName("series")]
            public T[] Series { get; set; } = Array.Empty<T>();
            [JsonPropertyName("index")]
            public int Index { get; set; }

            public SliceOutput(T[] series, int index)
            {
                Series = series;
                Index = index;
            }
        }
    }
}