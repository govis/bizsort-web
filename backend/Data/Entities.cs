using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace BizSrt.Api.Data.Entities;

public partial class Account
{
    public int Id { get; set; }
    public string? ExternalId { get; set; }
    public byte? ExternalProvider { get; set; }
    public int? Company { get; set; }
    public string Email { get; set; } = string.Empty;
    public byte[] PwdHash { get; set; } = Array.Empty<byte>();
    public byte[] PwdSalt { get; set; } = Array.Empty<byte>();
    public byte SecurityProfile { get; set; }
    public byte InvalidLogonCount { get; set; }
    public byte Status { get; set; }
    public byte PendingStatus { get; set; }
    public byte Options { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}

public partial class CompanyProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public short Category { get; set; }
    public long ServiceType { get; set; }
    public short TransactionType { get; set; }
    public long Industry { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? WebSite { get; set; }
    public string? Alias { get; set; }
    public string? Text { get; set; }
    public byte[]? RichText { get; set; }
    public byte MembershipType { get; set; }
    public byte Options { get; set; }
    public byte Status { get; set; }
    public byte PendingStatus { get; set; }
    public byte RejectReason { get; set; }
    public DateTime Created { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime Updated { get; set; }
    public DateTime? Indexed { get; set; }
    public short ProcessFlags { get; set; }

    public virtual ICollection<CompanyOffice> Offices { get; set; } = new List<CompanyOffice>();
}

public partial class CompanyOffice
{
    public int Id { get; set; }
    public int Company { get; set; }
    public int Location { get; set; }
    public string? PostalCode { get; set; }
    public string? StreetNumber { get; set; }
    public int? StreetName { get; set; }
    public string? Address1 { get; set; }
    public Geometry? GeoLocation { get; set; }
    public string? Phone { get; set; }
    public string? Phone1 { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public byte Order { get; set; }
    public string? WebUrl { get; set; }
    public string? PlaceId { get; set; }
    public DateTime? MetadataCheck { get; set; }
    public short ProcessFlags { get; set; }

    public virtual CompanyProfile CompanyProfile { get; set; } = null!;
}

public partial class Category
{
    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public short? Parent { get; set; }
    public short? QualifyingParent { get; set; }
    public long ServiceType { get; set; }
    public short ProductType { get; set; }
    public short TransactionType { get; set; }
    public long Industry { get; set; }
    public byte SortOrder { get; set; }
    public int? NAICSCode { get; set; }
    public string? Keywords { get; set; }
}

public partial class Product
{
    public long Id { get; set; }
    public short Type { get; set; }
    public string RichText { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? WebUrl { get; set; }
    public long? Master { get; set; }
    public byte Status { get; set; }
    public byte PendingStatus { get; set; }
    public byte RejectReason { get; set; }
    public DateTime Created { get; set; }
    public int CreatedBy { get; set; }
    public DateTime Updated { get; set; }
}

public partial class CompanyProduct
{
    public int Company { get; set; }
    public long Product { get; set; }
    public short Category { get; set; }
    public byte UnlistedType { get; set; }
    public short TransactionType { get; set; }
    public long? ServiceType { get; set; }
    public long? Industry { get; set; }
    public string? Tags { get; set; }
    public string? Alias { get; set; }
    public DateTime? Indexed { get; set; }
    public byte ImportStatus { get; set; }

    public virtual Product ProductNavigation { get; set; } = null!;
}

public partial class Project
{
    public long Id { get; set; }
    public short Category { get; set; }
    public byte TenderType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string? RichText { get; set; }
    public string? Thumbnail { get; set; }
    public int? Location { get; set; }
    public string? PostalCode { get; set; }
    public string? StreetNumber { get; set; }
    public int? StreetName { get; set; }
    public byte Status { get; set; }
    public DateTime Created { get; set; }
    public int CreatedBy { get; set; }
    public DateTime Updated { get; set; }
    public DateTime? Indexed { get; set; }
}

public partial class CompanyProject
{
    public int Company { get; set; }
    public long Project { get; set; }
    public byte UnlistedType { get; set; }

    public virtual Project ProjectNavigation { get; set; } = null!;
}

public partial class Job
{
    public long Id { get; set; }
    public int Company { get; set; }
    public int Organization { get; set; }
    public short Department { get; set; }
    public byte UnlistedType { get; set; }
    public short Type { get; set; }
    public DateTime? StartDate { get; set; }
    public int Duration { get; set; }
    public int? Location { get; set; }
    public string? PostalCode { get; set; }
    public string? StreetNumber { get; set; }
    public int? StreetName { get; set; }

    public virtual Product ProductNavigation { get; set; } = null!;
}

public interface IMedia
{
    byte[] Content { get; set; }
    byte[] Metadata { get; set; }
}

public partial class CompanyMedia : IMedia
{
    public int Id { get; set; }
    public int Company { get; set; }
    public byte Type { get; set; }
    public byte[] Content { get; set; } = null!;
    public byte[] Metadata { get; set; } = null!;
    public byte Order { get; set; }
}

public partial class ProductMedia : IMedia
{
    public long Id { get; set; }
    public long Product { get; set; }
    public byte Type { get; set; }
    public byte[] Content { get; set; } = null!;
    public byte[] Metadata { get; set; } = null!;
    public byte Order { get; set; }
}

public partial class ProjectMedia : IMedia
{
    public long Id { get; set; }
    public long Project { get; set; }
    public byte Type { get; set; }
    public byte[] Content { get; set; } = null!;
    public byte[] Metadata { get; set; } = null!;
    public byte Order { get; set; }
}

public partial class CommunityMedia : IMedia
{
    public int Id { get; set; }
    public int Community { get; set; }
    public byte Type { get; set; }
    public byte[] Content { get; set; } = null!;
    public byte[] Metadata { get; set; } = null!;
    public byte Order { get; set; }
}

public partial class CompanyAffiliation
{
    public int Id { get; set; }
    public int From { get; set; }
    public int To { get; set; }
    public DateTime Date { get; set; }
    public bool Pending { get; set; }
    public bool Declined { get; set; }
    public string? Text { get; set; }
}

public partial class CompanyCommunity
{
    public int Company { get; set; }
    public int Community { get; set; }
    public byte UnlistedType { get; set; }
}

public partial class Community
{
    public int Id { get; set; }
    public int Owner { get; set; }
    public byte Options { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string? Text { get; set; }
    public byte[]? RichText { get; set; }
    public int? Location { get; set; }
    public string? PostalCode { get; set; }
    public string? StreetNumber { get; set; }
    public int? StreetName { get; set; }
    public string? Address1 { get; set; }
    public Geometry? GeoLocation { get; set; }
    public string? DefaultCategory { get; set; }
    public string? Password { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}

public partial class Promotion
{
    public int Id { get; set; }
    public int Company { get; set; }
    public bool Active { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public virtual Community CommunityNavigation { get; set; } = null!;
}

