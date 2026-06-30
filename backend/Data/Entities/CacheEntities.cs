using System;
using System.ComponentModel.DataAnnotations;

namespace BizSrt.Api.Data.Entities;

public class Category_Unwound
{
    public short Parent { get; set; }
    public short Child { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool HasChildren { get; set; }
}

public class Location_Unwound
{
    public int Parent { get; set; }
    public int Child { get; set; }
}

public class Location : BizSrt.Api.Foundation.Cache.IKey<int>
{
    [Key]
    public int Id { get; set; }
    int BizSrt.Api.Foundation.Cache.IKey<int>.Key => Id;
    public int? Parent { get; set; }
    public byte Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool UserDefined { get; set; }
    public DateTime Created { get; set; }
    public byte SortOrder { get; set; }
}

public class StreetName : BizSrt.Api.Foundation.Cache.IKey<int>
{
    [Key]
    public int Id { get; set; }
    int BizSrt.Api.Foundation.Cache.IKey<int>.Key => Id;
    public int City { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AreaName
{
    [Key]
    public int Id { get; set; }
    public int Location { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CategoryProductAttribute
{
    [Key]
    public int Id { get; set; }
    public short Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte Type { get; set; }
    public byte Requirement { get; set; }
    public string? Group { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
    public string ValueOptions { get; set; } = string.Empty;
}

public class SecurityProfile
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentProfile { get; set; }
}

public class SecurityProfilePriviledge
{
    [Key]
    public int Id { get; set; }
    public int Profile { get; set; }
    public int Priviledge { get; set; }
    public bool Restricted { get; set; }
}
