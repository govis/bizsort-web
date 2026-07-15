using System.ComponentModel.DataAnnotations;

namespace BizSrt.Data.Entities;

public class ServiceType
{
    [Key]
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class TransactionType
{
    [Key]
    public short Id { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class Industry
{
    [Key]
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class ProductType
{
    [Key]
    public short Id { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class ProductAttributeType
{
    [Key]
    public short Id { get; set; }
    public bool Primitive { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte EditorType { get; set; }
    public byte ValueType { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
    public string? ValueOptions { get; set; }
}

public class Currency
{
    [Key]
    public byte Id { get; set; }
    public string ISOCode { get; set; } = string.Empty;
    public string CountryPriceFormat { get; set; } = string.Empty;
    public string PriceFormat { get; set; } = string.Empty;
}
