using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace BizSrt.Data.Entities;

[Table("CompanyFacets")]
public class CompanyFacet
{
    public long Id { get; set; }
    public int Company { get; set; }
    public int FacetValue { get; set; }
    public bool UserDefined { get; set; }
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

    [ForeignKey("Set")]
    public CompanyFacetSet CompanyFacetSet { get; set; } = null!;
}

[Table("FacetSetCompanies")]
[PrimaryKey(nameof(FacetSet), nameof(Company))]
public class FacetSetCompany
{
    public int FacetSet { get; set; }
    public int Company { get; set; }
}

[Table("CompanyOfferingFacets")]
public class CompanyOfferingFacet
{
    public long Id { get; set; }
    public long Offering { get; set; }
    public int FacetValue { get; set; }
    public bool UserDefined { get; set; }
}

[Table("CompanyOfferingFacetNames")]
public class CompanyOfferingFacetName : BizSrt.Foundation.Cache.IKey<short>
{
    public short Id { get; set; }
    [NotMapped] short BizSrt.Foundation.Cache.IKey<short>.Key => Id;
    public string Name { get; set; } = string.Empty;
}

[Table("CompanyOfferingFacetValues")]
public class CompanyOfferingFacetValue : BizSrt.Foundation.Cache.IKey<int>
{
    public int Id { get; set; }
    [NotMapped] int BizSrt.Foundation.Cache.IKey<int>.Key => Id;
    public short Name { get; set; }
    public byte ValueType { get; set; }
    public byte[] Value { get; set; } = Array.Empty<byte>();
    public string Text { get; set; } = string.Empty;
}

[Table("CompanyOfferingFacetSets")]
public class CompanyOfferingFacetSet : BizSrt.Foundation.Cache.IKey<int>
{
    public int Id { get; set; }
    [NotMapped] int BizSrt.Foundation.Cache.IKey<int>.Key => Id;
    public byte[] Key { get; set; } = Array.Empty<byte>();
    public byte InclFacets { get; set; }
    public int UseCount { get; set; }
    public DateTime LastUsed { get; set; }
    public DateTime? Indexed { get; set; }
}

[Table("CompanyOfferingFacetSetDetails")]
[Keyless]
public class CompanyOfferingFacetSetDetail
{
    public int Set { get; set; }
    public int Value { get; set; }
    public bool Exclude { get; set; }

    [ForeignKey("Set")]
    public CompanyOfferingFacetSet CompanyOfferingFacetSet { get; set; } = null!;
}

[Table("FacetSetCompanyOfferings")]
[PrimaryKey(nameof(FacetSet), nameof(Offering))]
public class FacetSetCompanyOffering
{
    public int FacetSet { get; set; }
    public long Offering { get; set; }
}
