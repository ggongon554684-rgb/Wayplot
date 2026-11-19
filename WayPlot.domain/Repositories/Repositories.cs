using WayPlot.domain.Entities;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.domain.Repositories;

// Only the methods the Data Collection services need. Steps 5 and 6 add list/search methods when the screens need them.
// Entities come back TRACKED, so a service can change them and call SaveAsync on the unit of work.

public interface IOwnerRepository
{
    Task<Owner?> GetAsync(Guid id);
    Task<IReadOnlyList<Owner>> ListAsync();
    Task<IReadOnlyList<Owner>> ListForVisibleParcelsAsync(TenantDataScope scope);
    void Add(Owner owner);
}

public interface IParcelRepository
{
    /// <summary>The parcel with its Owner loaded.</summary>
    Task<Parcel?> GetAsync(Guid id);
    /// <summary>All parcels with Owner loaded, ordered by Label.</summary>
    Task<IReadOnlyList<Parcel>> ListAsync();
    Task<IReadOnlyList<Parcel>> ListVisibleToAsync(TenantDataScope scope);
    /// <summary>Checks labels across active and archived parcels, optionally excluding the edited parcel.</summary>
    Task<bool> LabelExistsAsync(string label, Guid? exceptParcelId = null);
    void Add(Parcel parcel);
    void Touch(Parcel parcel);
}

public interface IParcelPhotoRepository
{
    Task<int> CountByParcelAsync(Guid parcelId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ParcelPhotoThumbnailDto>> ListByParcelAsync(Guid parcelId, CancellationToken cancellationToken = default);
    Task<ParcelPhoto?> GetAsync(Guid photoId, CancellationToken cancellationToken = default);
    Task<ParcelPhotoImageDto?> GetImageAsync(Guid photoId, CancellationToken cancellationToken = default);
    void Add(ParcelPhoto photo);
    void AddData(ParcelPhotoData data);
    void Remove(ParcelPhoto photo);
    Task<int> DeleteDataAsync(Guid photoId, CancellationToken cancellationToken = default);
}

public interface IContactRepository
{
    Task<Contact?> GetAsync(Guid id, TenantDataScope? scope = null);
    /// <summary>Contacts available as referrers, ordered by name, without tracking.</summary>
    Task<IReadOnlyList<Contact>> ListReferralContactsAsync(TenantDataScope? scope = null);
    /// <summary>First contact whose PhoneNormalized equals the phone OR whose Email equals the email. Null arguments are ignored.</summary>
    Task<Contact?> FindByPhoneOrEmailAsync(string? phoneNormalized, string? email);
    /// <summary>Contacts matching any normalized phone or email in the supplied batch, without tracking.</summary>
    Task<IReadOnlyList<Contact>> FindMatchesAsync(IReadOnlyCollection<string> phoneNumbers, IReadOnlyCollection<string> emails, TenantDataScope? scope = null);
    /// <summary>Contacts ordered by name. Matches the text against name, email and (for 3+ digits) phone. Blank returns everyone.</summary>
    Task<IReadOnlyList<Contact>> SearchAsync(string? text, TenantDataScope? scope = null);
    void Add(Contact contact);
}

public interface IStaffRepository
{
    Task<Staff?> GetAsync(Guid staffId);
    Task<IReadOnlyList<Staff>> ListAsync(TenantDataScope? scope = null);
    /// <summary>Active Agent staff, without tracking.</summary>
    Task<IReadOnlyList<Staff>> ListActiveAgentsAsync();
    Task<bool> IsActiveAgentAsync(Guid staffId);
    Task<IReadOnlyList<Staff>> ListActiveReferralStaffAsync();
    Task<IReadOnlyList<Staff>> ListActiveBranchManagersAsync(Guid branchId);
    Task<IReadOnlyList<Staff>> ListByIdsAsync(IReadOnlyCollection<Guid> staffIds);
    Task<bool> IsActiveReferralStaffAsync(Guid staffId);
    Task<int> CountActiveTenantAdminsAsync();
}

public interface IBranchRepository
{
    Task<IReadOnlyList<Branch>> ListAsync();
    Task<bool> ExistsAsync(Guid branchId);
}

public interface ISettingRepository
{
    Task<Setting?> GetForDisplayAsync(string key);
    Task<Setting?> GetByKeyAsync(string key);
    void Add(Setting setting);
}

public interface ICommissionRepository
{
    Task<Commission?> GetAsync(Guid id);
    Task<IReadOnlyList<Commission>> ListAsync(Guid? recipientStaffId, Guid? branchId, bool includeCompanyShares);
    Task<IReadOnlyList<Commission>> ListByDealAsync(Guid dealId);
    Task<bool> ExistsForDealAsync(Guid dealId);
    void Add(Commission commission);
}

public interface IReferralBonusRepository
{
    Task<ReferralBonus?> GetAsync(Guid id);
    Task<IReadOnlyList<ReferralBonus>> ListAsync(Guid? referrerStaffId, Guid? branchId);
    Task<IReadOnlyList<ReferralBonus>> ListByDealAsync(Guid dealId);
    Task<bool> ExistsForDealAsync(Guid dealId);
    void Add(ReferralBonus bonus);
}

public interface IDuplicateInquiryRepository
{
    void Add(DuplicateInquiry inquiry);
}

public interface ILeadRepository
{
    /// <summary>The lead with Contact and Parcel loaded.</summary>
    Task<Lead?> GetAsync(Guid id, TenantDataScope? scope = null);
    /// <summary>The lead with Contact and Parcel loaded for display, without tracking.</summary>
    Task<Lead?> GetForDisplayAsync(Guid id, TenantDataScope? scope = null);
    /// <summary>Leads matching the optional stage and contact/parcel search, newest activity first.</summary>
    Task<IReadOnlyList<Lead>> ListAsync(LeadStage? stage, string? search, Guid? assignedStaffId = null, TenantDataScope? scope = null);
    Task<int> CountOpenByStageAsync(LeadStage stage);
    /// <summary>Leads on the parcel with Contact and Parcel loaded, newest activity first.</summary>
    Task<IReadOnlyList<Lead>> ListByParcelAsync(Guid parcelId, TenantDataScope? scope = null);
    Task<IReadOnlyList<LeadStage>> ListStagesByParcelAsync(Guid parcelId);
    /// <summary>The contact's most recent lead that is not Lost, or null.</summary>
    Task<Lead?> GetLatestActiveForContactAsync(Guid contactId);
    /// <summary>True when the contact already has a lead on the parcel that is not Lost.</summary>
    Task<bool> ActiveLeadExistsAsync(Guid contactId, Guid parcelId);
    /// <summary>True when a different lead on the parcel is in Negotiation.</summary>
    Task<bool> AnotherLeadInNegotiationAsync(Guid parcelId, Guid exceptLeadId);
    Task<IReadOnlyList<Lead>> ListByContactAsync(Guid contactId, TenantDataScope? scope = null);
    void Add(Lead lead);
}

public interface IInteractionRepository
{
    /// <summary>Interactions with lead, contact and parcel loaded, newest first, without tracking.</summary>
    Task<IReadOnlyList<Interaction>> ListAsync(Guid? leadId, InteractionType? type, TenantDataScope? scope = null);
    /// <summary>Interactions for one lead, newest first, without tracking.</summary>
    Task<IReadOnlyList<Interaction>> ListByLeadAsync(Guid leadId, TenantDataScope? scope = null);
    void Add(Interaction interaction);
}

public interface IStageHistoryRepository
{
    /// <summary>Stage changes for one lead, oldest first, without tracking.</summary>
    Task<IReadOnlyList<StageHistory>> ListByLeadAsync(Guid leadId);
    void Add(StageHistory history);
}

public interface IDealRepository
{
    /// <summary>The tracked deal with its lead, contact, parcel and payments loaded.</summary>
    Task<Deal?> GetAsync(Guid id);
    /// <summary>Deals for the deal-management screen, without tracking.</summary>
    Task<IReadOnlyList<Deal>> ListAsync(Guid? agentStaffId, Guid? branchId);
    /// <summary>Confirmed FullyPaid deals without any commission rows, for idempotent backfill.</summary>
    Task<IReadOnlyList<Deal>> ListFullyPaidMissingCommissionAsync();
    /// <summary>True when a deal already holds the parcel.</summary>
    Task<bool> ActiveForParcelExistsAsync(Guid parcelId);
    /// <summary>The active deal holding a parcel, if one exists.</summary>
    Task<Deal?> GetActiveForParcelAsync(Guid parcelId);
    Task<bool> HasOtherLiveDealForContactAsync(Guid contactId, Guid exceptDealId);
    Task<IReadOnlyList<DealStatus>> ListStatusesByParcelAsync(Guid parcelId);
    Task<IReadOnlyList<Deal>> ListByLeadAsync(Guid leadId);
    Task<IReadOnlyList<Deal>> ListByContactAsync(Guid contactId, TenantDataScope? scope = null);
    void Add(Deal deal);
    void Touch(Deal deal);
}

public interface IPaymentRepository
{
    /// <summary>The tracked payment with its deal, parcel and deal payments loaded.</summary>
    Task<Payment?> GetAsync(Guid id);
    /// <summary>Payments for one deal, newest first, without tracking.</summary>
    Task<IReadOnlyList<Payment>> ListByDealAsync(Guid dealId);
    Task<bool> ReceiptExistsAsync(string receiptNo);
    void Add(Payment payment);
}
