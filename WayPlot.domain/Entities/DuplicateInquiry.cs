using WayPlot.domain.Common;

namespace WayPlot.domain.Entities;

public class DuplicateInquiry : BaseEntity
{
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    public Guid? ParcelId { get; set; }
    public Parcel? Parcel { get; set; }
    public LeadSource Source { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid? RecordedByStaffId { get; set; }
    public string Details { get; set; } = string.Empty;
}
