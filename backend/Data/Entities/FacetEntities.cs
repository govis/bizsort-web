using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace BizSrt.Api.Data.Entities;

[Table("CompanyFacets")]
[Keyless]
public class CompanyFacet
{
    public int Company { get; set; }
    public int FacetValue { get; set; }
}

[Table("CompanyFacetValues")]
public class CompanyFacetValue
{
    public int Id { get; set; }
    public short Name { get; set; }
    public string Text { get; set; } = string.Empty;
}

[Table("CompanyProductFacets")]
[Keyless]
public class CompanyProductFacet
{
    public long Product { get; set; }
    public int FacetValue { get; set; }
}

[Table("ProductFacetValues")]
public class ProductFacetValue
{
    public int Id { get; set; }
    public short Name { get; set; }
    public string Text { get; set; } = string.Empty;
}
