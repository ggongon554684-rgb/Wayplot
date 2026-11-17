using Microsoft.EntityFrameworkCore;
using WayPlot.domain.Common;
using WayPlot.domain.Entities;
using WayPlot.domain.Repositories;
using WayPlot.infrastructure.Data;

namespace WayPlot.infrastructure.Repositories;

internal sealed class OwnerRepository(TenantDbContext db) : IOwnerRepository
{
    public Task<Owner?> GetAsync(Guid id) =>
        db.Owners.FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IReadOnlyList<Owner>> ListAsync() =>
        await db.Owners.AsNoTracking().OrderBy(o => o.Name).ToListAsync();

    public void Add(Owner owner) => db.Owners.Add(owner);
}

internal sealed class ParcelRepository(TenantDbContext db) : IParcelRepository
{
    public Task<Parcel?> GetAsync(Guid id) =>
        db.Parcels.Include(p => p.Owner).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<Parcel>> ListAsync() =>
        await db.Parcels.AsNoTracking().Include(p => p.Owner).OrderBy(p => p.Label).ToListAsync();

    public Task<bool> LabelExistsAsync(string label) =>
        db.Parcels.AnyAsync(p => p.Label == label);

    public void Add(Parcel parcel) => db.Parcels.Add(parcel);
}

internal sealed class ContactRepository(TenantDbContext db) : IContactRepository
{
    public Task<Contact?> GetAsync(Guid id) =>
        db.Contacts.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Contact?> FindByPhoneOrEmailAsync(string? phoneNormalized, string? email)
    {
        // Phone is the stronger match, so it is tried first.
        if (phoneNormalized is not null)
        {
            var byPhone = await db.Contacts.FirstOrDefaultAsync(c => c.PhoneNormalized == phoneNormalized);
            if (byPhone is not null) return byPhone;
        }

        if (email is not null)
            return await db.Contacts.FirstOrDefaultAsync(c => c.Email == email);

        return null;
    }

    public async Task<IReadOnlyList<Contact>> SearchAsync(string? text)
    {
        var query = db.Contacts.AsNoTracking();

        var term = text?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var digits = new string(term.Where(char.IsDigit).ToArray());

            // Three or more digits is treated as a phone search too, so "0917 123" finds "09171234567".
            if (digits.Length >= 3)
                query = query.Where(c => c.FullName.Contains(term)
                                      || (c.Email != null && c.Email.Contains(term))
                                      || (c.PhoneNormalized != null && c.PhoneNormalized.Contains(digits)));
            else
                query = query.Where(c => c.FullName.Contains(term)
                                      || (c.Email != null && c.Email.Contains(term)));
        }

        return await query.OrderBy(c => c.FullName).Take(500).ToListAsync();
    }

    public void Add(Contact contact) => db.Contacts.Add(contact);


}

internal sealed class LeadRepository(TenantDbContext db) : ILeadRepository
{
    public Task<Lead?> GetAsync(Guid id) =>
        db.Leads.Include(l => l.Contact).Include(l => l.Parcel).FirstOrDefaultAsync(l => l.Id == id);

    public Task<Lead?> GetForDisplayAsync(Guid id) =>
        db.Leads.AsNoTracking().Include(l => l.Contact).Include(l => l.Parcel)
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<IReadOnlyList<Lead>> ListByParcelAsync(Guid parcelId) =>
        await db.Leads.AsNoTracking()
            .Include(l => l.Contact).Include(l => l.Parcel)
            .Where(l => l.ParcelId == parcelId)
            .OrderByDescending(l => l.LastActivityAtUtc)
            .ToListAsync();

    public Task<Lead?> GetLatestActiveForContactAsync(Guid contactId) =>
        db.Leads
            .Where(l => l.ContactId == contactId && l.Stage != LeadStage.Lost)
            .OrderByDescending(l => l.LastActivityAtUtc)
            .FirstOrDefaultAsync();

    public Task<bool> ActiveLeadExistsAsync(Guid contactId, Guid parcelId) =>
        db.Leads.AnyAsync(l => l.ContactId == contactId && l.ParcelId == parcelId && l.Stage != LeadStage.Lost);

    public Task<bool> AnotherLeadInNegotiationAsync(Guid parcelId, Guid exceptLeadId) =>
        db.Leads.AnyAsync(l => l.ParcelId == parcelId && l.Id != exceptLeadId && l.Stage == LeadStage.Negotiation);

    public void Add(Lead lead) => db.Leads.Add(lead);
}

internal sealed class InteractionRepository(TenantDbContext db) : IInteractionRepository
{
    public async Task<IReadOnlyList<Interaction>> ListByLeadAsync(Guid leadId) =>
        await db.Interactions.AsNoTracking()
            .Where(i => i.LeadId == leadId)
            .OrderByDescending(i => i.OccurredAtUtc)
            .ThenByDescending(i => i.Id)
            .ToListAsync();

    public void Add(Interaction interaction) => db.Interactions.Add(interaction);
}

internal sealed class StageHistoryRepository(TenantDbContext db) : IStageHistoryRepository
{
    public async Task<IReadOnlyList<StageHistory>> ListByLeadAsync(Guid leadId) =>
        await db.StageHistories.AsNoTracking()
            .Where(h => h.LeadId == leadId)
            .OrderBy(h => h.ChangedAtUtc)
            .ThenBy(h => h.Id)
            .ToListAsync();

    public void Add(StageHistory history) => db.StageHistories.Add(history);
}