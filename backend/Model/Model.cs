using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BizSrt.Api.Model
{
    public enum DictionaryType { SecurityProfile, ProductAttributeType }
    public class DictionaryItem { }
    public class DictionaryItem<T> : DictionaryItem { public T ItemKey { get; set; } = default!; }
    public class SecurityProfile : DictionaryItem<SecurityProfile.Type>
    {
        public int Id { get; set; }
        public SecurityProfile? ParentProfile { get; set; }
        public enum Type { None }
        public bool this[SecurityPriviledge key] { get { return false; } set { } }
        public string Name { get; set; } = string.Empty;
    }
    public enum SecurityPriviledge { None }
    public class SecurityProfilePriviledge { public int Priviledge { get; set; } public bool Restricted { get; set; } }

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

    public interface IRichText
    {
        string? RichText { get; set; }
        string? Text { get; set; }
    }

    public class IdName<T> : EntityId<T>, IEntityRef<T>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class AccountId
    {
        public AccountId() { }
        
        public AccountId(AccountType type)
        {
            AccountType = type;
        }

        [JsonPropertyName("accountType")]
        public AccountType AccountType { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class AccountEntity<T> : AccountId
    {
        public AccountEntity() : base() { }

        public AccountEntity(AccountType type) : base(type)
        { }

        [JsonPropertyName("entity")]
        public T? Entity { get; set; }
    }

    public class AccountName : AccountId
    {
        public AccountName() : base() { }

        public AccountName(AccountType type) : base(type)
        { }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class Account : AccountName
    {
        public Account() : base() { }

        public Account(AccountType type) : base(type)
        { }

        [JsonPropertyName("image")]
        public Image<int>? Image { get; set; }
    }

    public enum ImageEntity : byte
    {
        Company = 1,
        Product = 2,
        Service = 3,
        Project = 4,
        Job = 5,
        Community = 6,
        CommunityArticle = 7,
        Person = 8,
        Organization = 9
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

    public class Location : LocationRef, BizSrt.Api.Model.Group.IChild<Location>
    {
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;
        [JsonPropertyName("geoLocation")]
        public Geolocation? GeoLocation { get; set; }
        public new Location? Parent { get; set; }
    }

    public class ResolvedLocation : Location
    {
        [JsonPropertyName("partial")]
        public bool Partial { get; set; }
    }

    [Flags]
    public enum MediaType : byte
    {
        Image = 1,
        Default = 2,
        Default_Image = Image + Default,
        Content = 4,
        Content_Image = Image + Content
    }

    public enum ImageType : byte
    {
        Jpeg = 1,
        Png = 2,
        Gif = 3
    }

    namespace Geocoder
    {
        public class City
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
            [JsonPropertyName("country")]
            public string Country { get; set; } = string.Empty;
            [JsonPropertyName("state")]
            public string State { get; set; } = string.Empty;
            [JsonPropertyName("county")]
            public string County { get; set; } = string.Empty;
            [JsonPropertyName("area")]
            public string Area { get; set; } = string.Empty;
        }

        public interface IGeolocation
        {
            float Lat { get; set; }
            float Lng { get; set; }
        }

        public class Geolocation : IGeolocation
        {
            [JsonPropertyName("lat")]
            public float Lat { get; set; }
            [JsonPropertyName("lng")]
            public float Lng { get; set; }
        }

        public class Address
        {
            [JsonPropertyName("properties")]
            internal System.Collections.Generic.Dictionary<string, string> _properties;

            public Address() { _properties = new System.Collections.Generic.Dictionary<string, string>(); }

            public string this[string name]
            {
                get { return _properties.TryGetValue(name, out string value) ? value : null; }
                set
                {
                    if (!string.IsNullOrWhiteSpace(value)) _properties[name] = value;
                    else if (_properties.ContainsKey(name)) _properties.Remove(name);
                }
            }

            public string Country { get { return this["country"]; } set { this["country"] = value; } }
            public string State { get { return this["state"]; } set { this["state"] = value; } }
            public string County { get { return this["county"]; } set { this["county"] = value; } }
            public string City { get { return this["city"]; } set { this["city"] = value; } }
            public string StreetNumber { get { return this["streetNumber"]; } set { this["streetNumber"] = value; } }
            public string StreetName { get { return this["streetName"]; } set { this["streetName"] = value; } }
            public string PostalCode { get { return this["postalCode"]; } set { this["postalCode"] = value; } }
            public string Address1 { get { return this["address1"]; } set { this["address1"] = value; } }
            public bool IsEmpty { get { return _properties.Count == 0; } }
        }

        public partial class Location
        {
            public Location()
            {
                Address = new Address();
                Geolocation = new Geolocation();
            }
            [JsonPropertyName("id")]
            public int Id { get; set; }
            [JsonPropertyName("address")]
            public Address Address { get; set; }
            [JsonPropertyName("text")]
            public string Text { get; set; }
            [JsonPropertyName("geoLocation")]
            public Geolocation Geolocation { get; set; }
        }
    }

    public class Address : Geocoder.IGeolocation
    {
        public Address(int location, string postalCode, string streetNumber, int? streetName, string? address1 = null, NetTopologySuite.Geometries.Geometry? geoLocation = null)
        {
            Location = location;
            if (!string.IsNullOrWhiteSpace(postalCode)) PostalCode = postalCode;
            if (streetName != null && streetName.HasValue)
            {
                Street = streetName.Value;
                if (!string.IsNullOrWhiteSpace(streetNumber)) StreetNumber = streetNumber;
            }
            if (!string.IsNullOrWhiteSpace(address1)) Address1 = address1;
            if (geoLocation != null)
            {
                Lat = (float)((NetTopologySuite.Geometries.Point)geoLocation).Y;
                Lng = (float)((NetTopologySuite.Geometries.Point)geoLocation).X;
            }
        }
        public Address() { }
        [JsonPropertyName("locationPath")]
        public Location? LocationPath { get; set; }
        [JsonPropertyName("location")]
        public int Location { get; set; }
        [JsonPropertyName("postalCode")]
        public string PostalCode { get; set; } = string.Empty;
        [JsonPropertyName("street")]
        public int Street { get; set; }
        [JsonPropertyName("streetNumber")]
        public string StreetNumber { get; set; } = string.Empty;
        [JsonPropertyName("address1")]
        public string Address1 { get; set; } = string.Empty;
        [JsonPropertyName("lat")]
        public float Lat { get; set; }
        [JsonPropertyName("lng")]
        public float Lng { get; set; }
    }

    public class Category : BizSrt.Api.Model.Group.IdName<short>
    {
        public enum MemberType : byte { Company = 1, Product = 2 }
        public byte MemberTypeVal { get; set; }
        public long Service { get; set; }
        public long Product { get; set; }
        public long Transaction { get; set; }
        public long Industry { get; set; }
        public ProductAttribute[]? ProductAttributes { get; set; }
        
        public class ProductAttribute
        {
            public byte Type { get; set; }
            public string Name { get; set; } = string.Empty;
            public byte EditorType { get; set; }
            public byte ValueType { get; set; }
            public string DefaultValue { get; set; } = string.Empty;
            public string[]? ValueOptions { get; set; }
            public BizSrt.Api.Model.Product.Attribute.Requirement Requirement { get; set; }
        }
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
            [JsonPropertyName("searchQuery")]
            public string? SearchQuery { get; set; }
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

        public class SearchOutput<T> : QueryOutput<T>
        {
            [JsonPropertyName("facets")]
            public BizSrt.Api.Model.Semantic.FacetName[]? Facets { get; set; }
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

    public interface IEntityId<T>
    {
        T Id { get; set; }
    }

    public enum LocationType : byte
    {
        Unknown = 0,
        World = 1,
        Country = 2,
        State = 3,
        County = 4,
        City = 5,
        Street = 6,
        Neighborhood = 7
    }

    public class LocationSettings
    {
        public int Id { get; set; }
        public bool CountyRequired { get; set; }
    }



    public class LocationRef : BizSrt.Api.Model.Group.NodeRef<int>
    {
        public LocationType Type { get; set; }
    }
}



namespace BizSrt.Api.Model
{
    public interface IEntityRef<T> : IEntityId<T>
    {
        string Name { get; set; }
    }
}

namespace BizSrt.Api.Model.Product
{
    public class Attribute
    {
        public class Type : BizSrt.Api.Model.DictionaryItem<byte>
        {
            public byte EditorType { get; set; }
            public byte ValueType { get; set; }
            public string DefaultValue { get; set; } = string.Empty;
            public string[]? ValueOptions { get; set; }
        }
        public enum Requirement : byte { None }
        public class ProductAttribute { }
    }
}

namespace BizSrt.Api.Model
{
    public enum SubType : byte
    {
        None = 0,
        Option1 = 1
    }
    
    
}
