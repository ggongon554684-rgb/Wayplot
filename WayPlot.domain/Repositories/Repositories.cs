using WayPlot.domain.Entities;

namespace WayPlot.domain.Repositories;

// Only the methods the Data Collection services need. Steps 5 and 6 add list/search methods when the screens need them.
// Entities come back TRACKED, so a service can change them and call SaveAsync on the unit of work.

public interface IOwnerRepository
{
    Task<Owner?> GetAsync(Guid id);
    Task<IReadOnlyList<Owner>> ListAsync();
    void Add(Owner owner);
}

public interface IParcelRepository
{
    /// <summary>The parcel with its Owner loaded.</summary>
    Task<Parcel?> GetAsync(Guid id);
    /// <summary>All parcels with Owner loaded, ordered by Label.</summary>
    Task<IReadOnlyList<Parcel>> ListAsync();
    Task<bool> LabelExistsAsync(string label);
    void Add(Parcel parcel);
}

public interface IContactRepository
{
    Task<Contact?> GetAsync(Guid id);
    /// <summary>First contact whose PhoneNormalized equals the phone OR whose Email equals the email. Null arguments are ignored.</summary>
    Task<Contact?> FindByPhoneOrEmailAsync(string? phoneNormalized, string? email);
    /// <summary>Contacts ordered by name. Matches the text against name, email and (for 3+ digits) phone. Blank returns everyone.</summary>
    Task<IReadOnlyList<Contact>> SearchAsync(string? text);
    void Add(Contact contact);
}

public interface ILeadRepository
{
    /// <summary>The lead with Contact and Parcel loaded.</summary>
    Task<Lead?> GetAsync(Guid id);
    /// <summary>The lead with Contact and Parcel loaded for display, without tracking.</summary>
    Task<Lead?> GetForDisplayAsync(Guid id);
    /// <summary>Leads on the parcel with Contact and Parcel loaded, newest activity first.</summary>
    Task<IReadOnlyList<Lead>> ListByParcelAsync(Guid parcelId);
    /// <summary>The contact's most recent lead that is not Lost, or null.</summary>
    Task<Lead?> GetLatestActiveForContactAsync(Guid contactId);
    /// <summary>True when the contact already has a lead on the parcel that is not Lost.</summary>
    Task<bool> ActiveLeadExistsAsync(Guid contactId, Guid parcelId);
    /// <summary>True when a different lead on the parcel is in Negotiation.</summary>
    Task<bool> AnotherLeadInNegotiationAsync(Guid parcelId, Guid exceptLeadId);
    void Add(Lead lead);
}

public interface IInteractionRepository
{
    /// <summary>Interactions for one lead, newest first, without tracking.</summary>
    Task<IReadOnlyList<Interaction>> ListByLeadAsync(Guid leadId);
    void Add(Interaction interaction);
}

public interface IStageHistoryRepository
{
    /// <summary>Stage changes for one lead, oldest first, without tracking.</summary>
    Task<IReadOnlyList<StageHistory>> ListByLeadAsync(Guid leadId);
    void Add(StageHistory history);
}