using WayPlot.domain.Common;

namespace WayPlot.domain.Entities;

public class Owner : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool ListingAgreementOnFile { get; set; }
}

public class Parcel : BaseEntity
{
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal AreaSqm { get; set; }
    public string Zoning { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal ReservationFee { get; set; }
    public ParcelStatus Status { get; set; } = ParcelStatus.Available;
    /// <summary>Boundary as GeoJSON text. A single marker uses Latitude/Longitude instead.</summary>
    public string? GeoJson { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string TaxDeclarationNo { get; set; } = string.Empty;
    public string TitleNo { get; set; } = string.Empty;
    public string Utilities { get; set; } = string.Empty;
    public string RoadAccess { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public Owner Owner { get; set; } = null!;
    public Guid? AssignedStaffId { get; set; }
    public List<ParcelPhoto> Photos { get; set; } = [];
}

public class ParcelPhoto : BaseEntity
{
    public Guid ParcelId { get; set; }
    public Parcel Parcel { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    /// <summary>Database-backed locator: "db:" followed by this photo's Id.</summary>
    public string StoragePath { get; set; } = string.Empty;
}

public class ParcelPhotoData : BaseEntity
{
    public Guid ParcelPhotoId { get; set; }
    public string ContentType { get; set; } = "image/jpeg";
    public int SizeBytes { get; set; }
    public byte[] ImageBytes { get; set; } = [];
    public byte[] ThumbnailBytes { get; set; } = [];
}

public class Contact : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    /// <summary>Digits only, format 09XXXXXXXXX. Used for the duplicate check. See PhoneNormalizer.</summary>
    public string? PhoneNormalized { get; set; }
    public string? Email { get; set; }
    public LeadSource Source { get; set; } = LeadSource.Other;
    public ContactType Type { get; set; } = ContactType.Customer;
    public bool ConsentSms { get; set; }
    public bool ConsentEmail { get; set; }
    public DateTime? ConsentAtUtc { get; set; }
    public Guid? ReferrerContactId { get; set; }
    public Guid? ReferrerStaffId { get; set; }
    public string? ReferrerName { get; set; }
    public string? ReferrerPhone { get; set; }
    public Guid? AssignedStaffId { get; set; }
}
