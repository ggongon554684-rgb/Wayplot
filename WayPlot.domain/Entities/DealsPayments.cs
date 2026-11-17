using WayPlot.domain.Common;

namespace WayPlot.domain.Entities;

public class Deal : BaseEntity
{
    public Guid LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public Guid ParcelId { get; set; }
    public Parcel Parcel { get; set; } = null!;
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    public Guid AgentStaffId { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal ReservationFee { get; set; }
    public decimal DownPayment { get; set; }
    public bool IsInstallment { get; set; }
    public int InstallmentCount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public int DueDay { get; set; }
    public decimal LatePenaltyPercentPerMonth { get; set; }
    public DealStatus Status { get; set; } = DealStatus.Current;
    /// <summary>True while the deal holds the parcel. A filtered unique index allows only one active deal per parcel.</summary>
    public bool IsActive { get; set; } = true;
    public DateTime? ConfirmedAtUtc { get; set; }
    public Guid? ConfirmedByStaffId { get; set; }
    public List<Payment> Payments { get; set; } = [];
    public List<Commission> Commissions { get; set; } = [];
}

public class Payment : BaseEntity
{
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
    public DateTime PaidAtUtc { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public Guid? RecordedByStaffId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.AwaitingAdmin;
    public string? ReviewNote { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public Guid? ConfirmedByStaffId { get; set; }
}

public class Commission : BaseEntity
{
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
    public Guid? RecipientStaffId { get; set; }
    public string RecipientRole { get; set; } = string.Empty;
    public decimal? Rate { get; set; }
    public decimal Amount { get; set; }
    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;
    public DateTime? PaidAtUtc { get; set; }
}

public class ReferralBonus : BaseEntity
{
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
    public Guid? ReferrerContactId { get; set; }
    public Guid? ReferrerStaffId { get; set; }
    public string ReferrerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;
    public DateTime? PaidAtUtc { get; set; }
}

public class Document : BaseEntity
{
    public Guid? DealId { get; set; }
    public Deal? Deal { get; set; }
    public Guid? ParcelId { get; set; }
    public Parcel? Parcel { get; set; }
    public DocumentType Type { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public Guid? UploadedByStaffId { get; set; }
}

public class LandownerPayout : BaseEntity
{
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
    public Guid OwnerId { get; set; }
    public Owner Owner { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; }
    public string Reference { get; set; } = string.Empty;
}
