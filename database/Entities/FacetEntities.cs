using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace BizSrt.Data.Entities;

[Table("CompanyFacets")]
[Keyless]
public class CompanyFacet
{
    public int Company { get; set; }
    public int FacetValue { get; set; }
}

[Table("CompanyFacetNames")]
public class CompanyFacetName : BizSrt.Foundation.Cache.IKey<short>
{
    public short Id { get; set; }
    [NotMapped] short BizSrt.Foundation.Cache.IKey<short>.Key => Id;
    public string Name { get; set; } = string.Empty;
}

[Table("CompanyFacetValues")]
public class CompanyFacetValue : BizSrt.Foundation.Cache.IKey<int>
{
    public int Id { get; set; }
    [NotMapped] int BizSrt.Foundation.Cache.IKey<int>.Key => Id;
    public short Name { get; set; }
    public byte ValueType { get; set; }
    public byte[] Value { get; set; } = Array.Empty<byte>();
    public string Text { get; set; } = string.Empty;
}

[Table("CompanyFacetSets")]
public class CompanyFacetSet : BizSrt.Foundation.Cache.IKey<int>
{
    public int Id { get; set; }
    [NotMapped] int BizSrt.Foundation.Cache.IKey<int>.Key => Id;
    public byte[] Key { get; set; } = Array.Empty<byte>();
    public byte InclFacets { get; set; }
    public int UseCount { get; set; }
    public DateTime LastUsed { get; set; }
    public DateTime? Indexed { get; set; }
}

[Table("CompanyFacetSetDetails")]
[Keyless]
public class CompanyFacetSetDetail
{
    public int Set { get; set; }
    public int Value { get; set; }
    public bool Exclude { get; set; }
}

[Table("CompanyProductFacets")]
[Keyless]
public class CompanyProductFacet
{
    public long Product { get; set; }
    public int FacetValue { get; set; }
}

[Table("CompanyProductFacetNames")]
public class CompanyProductFacetName : BizSrt.Foundation.Cache.IKey<short>
{
    public short Id { get; set; }
    [NotMapped] short BizSrt.Foundation.Cache.IKey<short>.Key => Id;
    public string Name { get; set; } = string.Empty;
}

[Table("CompanyProductFacetValues")]
public class CompanyProductFacetValue : BizSrt.Foundation.Cache.IKey<int>
{
    public int Id { get; set; }
    [NotMapped] int BizSrt.Foundation.Cache.IKey<int>.Key => Id;
    public short Name { get; set; }
    public byte ValueType { get; set; }
    public byte[] Value { get; set; } = Array.Empty<byte>();
    public string Text { get; set; } = string.Empty;
}

[Table("CompanyProductFacetSets")]
public class CompanyProductFacetSet : BizSrt.Foundation.Cache.IKey<int>
{
    public int Id { get; set; }
    [NotMapped] int BizSrt.Foundation.Cache.IKey<int>.Key => Id;
    public byte[] Key { get; set; } = Array.Empty<byte>();
    public byte InclFacets { get; set; }
    public int UseCount { get; set; }
    public DateTime LastUsed { get; set; }
    public DateTime? Indexed { get; set; }
}

[Table("CompanyProductFacetSetDetails")]
[Keyless]
public class CompanyProductFacetSetDetail
{
    public int Set { get; set; }
    public int Value { get; set; }
    public bool Exclude { get; set; }
}
