using Microsoft.EntityFrameworkCore;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Repositories;
using WayPlot.domain.Services.DataCollection;
using WayPlot.infrastructure.Data;

namespace WayPlot.infrastructure.Repositories;

internal sealed class OwnerRepository(TenantDbContext db) : IOwnerRepository
{
    public Task<Owner?> GetAsync(Guid id) =>
        db.Owners.FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IReadOnlyList<Owner>> ListAsync() =>
        await db.Owners.AsNoTracking().OrderBy(o => o.Name).ToListAsync();

    public async Task<IReadOnlyList<Owner>> ListForVisibleParcelsAsync(TenantDataScope scope)
    {
        if (scope.Kind == TenantDataScopeKind.Tenant)
            return await db.Owners.AsNoTracking().OrderBy(owner => owner.Name).ToListAsync();

        var parcels = ParcelRepository.ApplyParcelScope(db.Parcels.AsNoTracking(), scope);
        return await db.Owners.AsNoTracking()
            .Where(owner => parcels.Any(parcel => parcel.OwnerId == owner.Id))
            .OrderBy(owner => owner.Name)
            .ToListAsync();
    }

    public void Add(Owner owner) => db.Owners.Add(owner);
}

internal sealed class ParcelRepository(TenantDbContext db) : IParcelRepository
{
    public Task<Parcel?> GetAsync(Guid id) =>
        db.Parcels.Include(p => p.Owner).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<Parcel>> ListAsync() =>
        await db.Parcels.AsNoTracking().Include(p => p.Owner).OrderBy(p => p.Label).ToListAsync();

    public async Task<IReadOnlyList<Parcel>> ListVisibleToAsync(TenantDataScope scope) =>
        await ApplyParcelScope(db.Parcels.AsNoTracking().Include(parcel => parcel.Owner), scope)
            .OrderBy(parcel => parcel.Label)
            .ToListAsync();

    public Task<bool> LabelExistsAsync(string label, Guid? exceptParcelId = null)
    {
        var normalizedLabel = label.Trim().ToUpper();
        var query = db.Parcels.IgnoreQueryFilters()
            .Where(parcel => parcel.Label.Trim().ToUpper() == normalizedLabel);
        if (exceptParcelId is { } parcelId)
            query = query.Where(parcel => parcel.Id != parcelId);
        return query.AnyAsync();
    }

    public void Add(Parcel parcel) => db.Parcels.Add(parcel);

    public void Touch(Parcel parcel) =>
        db.Entry(parcel).Property(p => p.Version).IsModified = true;

    internal static IQueryable<Parcel> ApplyParcelScope(IQueryable<Parcel> query, TenantDataScope scope) =>
        scope.Kind switch
        {
            TenantDataScopeKind.Agent when scope.StaffId is { } staffId =>
                query.Where(parcel => parcel.AssignedStaffId == staffId),
            TenantDataScopeKind.Branch when scope.BranchId is { } branchId =>
                query.Where(parcel => parcel.BranchId == branchId),
            _ => query
        };
}

internal sealed class ParcelPhotoRepository(TenantDbContext db) : IParcelPhotoRepository
{
    public Task<int> CountByParcelAsync(Guid parcelId, CancellationToken cancellationToken = default) =>
        db.ParcelPhotos.CountAsync(photo => photo.ParcelId == parcelId, cancellationToken);

    public async Task<IReadOnlyList<ParcelPhotoThumbnailDto>> ListByParcelAsync(
        Guid parcelId,
        CancellationToken cancellationToken = default) =>
        await (from photo in db.ParcelPhotos.AsNoTracking()
               join data in db.ParcelPhotoData.AsNoTracking() on photo.Id equals data.ParcelPhotoId
               where photo.ParcelId == parcelId
               orderby photo.CreatedAtUtc, photo.Id
               select new ParcelPhotoThumbnailDto(photo.Id, photo.FileName, data.SizeBytes, data.ThumbnailBytes))
            .ToListAsync(cancellationToken);

    public Task<ParcelPhoto?> GetAsync(Guid photoId, CancellationToken cancellationToken = default) =>
        db.ParcelPhotos.FirstOrDefaultAsync(photo => photo.Id == photoId, cancellationToken);

    public Task<ParcelPhotoImageDto?> GetImageAsync(Guid photoId, CancellationToken cancellationToken = default) =>
        (from photo in db.ParcelPhotos.AsNoTracking()
         join data in db.ParcelPhotoData.AsNoTracking() on photo.Id equals data.ParcelPhotoId
         where photo.Id == photoId
         select new ParcelPhotoImageDto(photo.FileName, data.ContentType, data.ImageBytes))
        .FirstOrDefaultAsync(cancellationToken);

    public void Add(ParcelPhoto photo) => db.ParcelPhotos.Add(photo);

    public void AddData(ParcelPhotoData data) => db.ParcelPhotoData.Add(data);

    public void Remove(ParcelPhoto photo) => db.ParcelPhotos.Remove(photo);

    public Task<int> DeleteDataAsync(Guid photoId, CancellationToken cancellationToken = default) =>
        db.ParcelPhotoData.Where(data => data.ParcelPhotoId == photoId).ExecuteDeleteAsync(cancellationToken);
}

internal sealed class ContactRepository(TenantDbContext db) : IContactRepository
{
    public Task<Contact?> GetAsync(Guid id, TenantDataScope? scope = null) =>
        ApplyScope(db.Contacts, scope).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IReadOnlyList<Contact>> ListReferralContactsAsync(TenantDataScope? scope = null) =>
        await ApplyScope(db.Contacts.AsNoTracking(), scope)
            .OrderBy(c => c.FullName)
            .Take(500)
            .ToListAsync();

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

    public async Task<IReadOnlyList<Contact>> FindMatchesAsync(
        IReadOnlyCollection<string> phoneNumbers,
        IReadOnlyCollection<string> emails,
        TenantDataScope? scope = null)
    {
        var phones = phoneNumbers.Distinct().ToArray();
        var addresses = emails.Distinct().ToArray();
        if (phones.Length == 0 && addresses.Length == 0)
            return Array.Empty<Contact>();

        return await ApplyScope(db.Contacts.AsNoTracking(), scope)
            .Where(c =>
                (c.PhoneNormalized != null && phones.Contains(c.PhoneNormalized))
                || (c.Email != null && addresses.Contains(c.Email)))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Contact>> SearchAsync(string? text, TenantDataScope? scope = null)
    {
        var query = ApplyScope(db.Contacts.AsNoTracking(), scope);

        var term = text?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var digits = new string(term.Where(char.IsDigit).ToArray());

            // Three or more digits is treated as a phone search too, so "0917 123" finds "09171234567".
            if (digits.Length >= 3)
            {
                var normalizedPhone = PhoneNormalizer.Normalize(term);
                var phoneTerm = normalizedPhone ?? digits;
                query = query.Where(c => c.FullName.Contains(term)
                                      || (c.Email != null && c.Email.Contains(term))
                                      || (c.PhoneNormalized != null && c.PhoneNormalized.Contains(phoneTerm)));
            }
            else
                query = query.Where(c => c.FullName.Contains(term)
                                      || (c.Email != null && c.Email.Contains(term)));
        }

        return await query.OrderBy(c => c.FullName).Take(500).ToListAsync();
    }

    public void Add(Contact contact) => db.Contacts.Add(contact);

    private IQueryable<Contact> ApplyScope(IQueryable<Contact> query, TenantDataScope? scope) =>
        scope?.Kind switch
        {
            TenantDataScopeKind.Agent when scope.Value.StaffId is { } staffId => query.Where(contact =>
                contact.AssignedStaffId == staffId
                || db.Leads.Any(lead => lead.ContactId == contact.Id && lead.AssignedStaffId == staffId)),
            TenantDataScopeKind.Branch when scope.Value.BranchId is { } branchId => query.Where(contact =>
                contact.BranchId == branchId
                || db.Leads.Any(lead => lead.ContactId == contact.Id && lead.BranchId == branchId)),
            _ => query
        };
}

internal sealed class StaffRepository(TenantDbContext db) : IStaffRepository
{
    public Task<Staff?> GetAsync(Guid staffId) =>
        db.Staffs.FirstOrDefaultAsync(staff => staff.Id == staffId);

    public async Task<IReadOnlyList<Staff>> ListAsync(TenantDataScope? scope = null)
    {
        var query = db.Staffs.AsNoTracking();
        if (scope?.Kind == TenantDataScopeKind.Branch && scope.Value.BranchId is { } branchId)
            query = query.Where(staff => staff.BranchId == branchId);
        else if (scope?.Kind == TenantDataScopeKind.Agent)
            return Array.Empty<Staff>();
        return await query.OrderBy(staff => staff.FullName).ToListAsync();
    }

    public async Task<IReadOnlyList<Staff>> ListActiveAgentsAsync() =>
        await db.Staffs.AsNoTracking()
            .Where(staff => staff.IsActive && staff.Role == Roles.Agent)
            .OrderBy(staff => staff.FullName)
            .ToListAsync();

    public Task<bool> IsActiveAgentAsync(Guid staffId) =>
        db.Staffs.AnyAsync(staff => staff.Id == staffId && staff.IsActive && staff.Role == Roles.Agent);

    public async Task<IReadOnlyList<Staff>> ListActiveReferralStaffAsync() =>
        await db.Staffs.AsNoTracking()
            .Where(staff => staff.IsActive
                && (staff.Role == Roles.Agent || staff.Role == Roles.BranchManager || staff.Role == Roles.TenantAdmin))
            .OrderBy(staff => staff.FullName)
            .Take(500)
            .ToListAsync();

    public async Task<IReadOnlyList<Staff>> ListActiveBranchManagersAsync(Guid branchId) =>
        await db.Staffs.AsNoTracking()
            .Where(staff => staff.IsActive && staff.Role == Roles.BranchManager && staff.BranchId == branchId)
            .OrderBy(staff => staff.FullName)
            .ToListAsync();

    public async Task<IReadOnlyList<Staff>> ListByIdsAsync(IReadOnlyCollection<Guid> staffIds)
    {
        if (staffIds.Count == 0) return Array.Empty<Staff>();
        return await db.Staffs.AsNoTracking()
            .Where(staff => staffIds.Contains(staff.Id))
            .ToListAsync();
    }

    public Task<bool> IsActiveReferralStaffAsync(Guid staffId) =>
        db.Staffs.AnyAsync(staff => staff.Id == staffId && staff.IsActive
            && (staff.Role == Roles.Agent || staff.Role == Roles.BranchManager || staff.Role == Roles.TenantAdmin));

    public Task<int> CountActiveTenantAdminsAsync() =>
        db.Staffs.CountAsync(staff => staff.IsActive && staff.Role == Roles.TenantAdmin);
}

internal sealed class BranchRepository(TenantDbContext db) : IBranchRepository
{
    public async Task<IReadOnlyList<Branch>> ListAsync() =>
        await db.Branches.AsNoTracking().OrderBy(branch => branch.Name).ToListAsync();

    public Task<bool> ExistsAsync(Guid branchId) =>
        db.Branches.AnyAsync(branch => branch.Id == branchId);
}

internal sealed class SettingRepository(TenantDbContext db) : ISettingRepository
{
    public Task<Setting?> GetForDisplayAsync(string key) =>
        db.Settings.AsNoTracking().FirstOrDefaultAsync(setting => setting.Key == key);

    public Task<Setting?> GetByKeyAsync(string key) =>
        db.Settings.FirstOrDefaultAsync(setting => setting.Key == key);

    public void Add(Setting setting) => db.Settings.Add(setting);
}

internal sealed class CommissionRepository(TenantDbContext db) : ICommissionRepository
{
    public Task<Commission?> GetAsync(Guid id) =>
        db.Commissions.Include(commission => commission.Deal).ThenInclude(deal => deal.Payments)
            .FirstOrDefaultAsync(commission => commission.Id == id);

    public async Task<IReadOnlyList<Commission>> ListAsync(
        Guid? recipientStaffId, Guid? branchId, bool includeCompanyShares)
    {
        IQueryable<Commission> query = db.Commissions.AsNoTracking()
            .Include(commission => commission.Deal).ThenInclude(deal => deal.Lead).ThenInclude(lead => lead.Contact)
            .Include(commission => commission.Deal).ThenInclude(deal => deal.Lead).ThenInclude(lead => lead.Parcel)
            .Include(commission => commission.Deal).ThenInclude(deal => deal.Payments);

        if (recipientStaffId is { } staffId && branchId is { } selectedBranchId)
            query = query.Where(commission => commission.RecipientStaffId == staffId
                || commission.BranchId == selectedBranchId);
        else if (recipientStaffId is { } selectedStaffId)
            query = query.Where(commission => commission.RecipientStaffId == selectedStaffId);
        else if (branchId is { } selectedOnlyBranchId)
            query = query.Where(commission => commission.BranchId == selectedOnlyBranchId);

        if (!includeCompanyShares)
            query = query.Where(commission => commission.RecipientRole != "Company");

        return await query
            .OrderByDescending(commission => commission.CreatedAtUtc)
            .ThenByDescending(commission => commission.Id)
            .Take(500)
            .ToListAsync();
    }

    public Task<bool> ExistsForDealAsync(Guid dealId) =>
        db.Commissions.AnyAsync(commission => commission.DealId == dealId);

    public async Task<IReadOnlyList<Commission>> ListByDealAsync(Guid dealId) =>
        await db.Commissions.Where(commission => commission.DealId == dealId).ToListAsync();

    public void Add(Commission commission) => db.Commissions.Add(commission);
}

internal sealed class ReferralBonusRepository(TenantDbContext db) : IReferralBonusRepository
{
    public Task<ReferralBonus?> GetAsync(Guid id) =>
        db.ReferralBonuses.Include(bonus => bonus.Deal).ThenInclude(deal => deal.Payments)
            .FirstOrDefaultAsync(bonus => bonus.Id == id);

    public async Task<IReadOnlyList<ReferralBonus>> ListAsync(Guid? referrerStaffId, Guid? branchId)
    {
        IQueryable<ReferralBonus> query = db.ReferralBonuses.AsNoTracking()
            .Include(bonus => bonus.Deal).ThenInclude(deal => deal.Lead).ThenInclude(lead => lead.Contact)
            .Include(bonus => bonus.Deal).ThenInclude(deal => deal.Lead).ThenInclude(lead => lead.Parcel)
            .Include(bonus => bonus.Deal).ThenInclude(deal => deal.Payments);

        if (referrerStaffId is { } staffId && branchId is { } selectedBranchId)
            query = query.Where(bonus => bonus.ReferrerStaffId == staffId || bonus.BranchId == selectedBranchId);
        else if (referrerStaffId is { } selectedStaffId)
            query = query.Where(bonus => bonus.ReferrerStaffId == selectedStaffId);
        else if (branchId is { } selectedOnlyBranchId)
            query = query.Where(bonus => bonus.BranchId == selectedOnlyBranchId);

        return await query
            .OrderByDescending(bonus => bonus.CreatedAtUtc)
            .ThenByDescending(bonus => bonus.Id)
            .Take(500)
            .ToListAsync();
    }

    public Task<bool> ExistsForDealAsync(Guid dealId) =>
        db.ReferralBonuses.AnyAsync(bonus => bonus.DealId == dealId);

    public async Task<IReadOnlyList<ReferralBonus>> ListByDealAsync(Guid dealId) =>
        await db.ReferralBonuses.Where(bonus => bonus.DealId == dealId).ToListAsync();

    public void Add(ReferralBonus bonus) => db.ReferralBonuses.Add(bonus);
}

internal sealed class DuplicateInquiryRepository(TenantDbContext db) : IDuplicateInquiryRepository
{
    public void Add(DuplicateInquiry inquiry) => db.DuplicateInquiries.Add(inquiry);
}

internal sealed class LeadRepository(TenantDbContext db) : ILeadRepository
{
    public Task<Lead?> GetAsync(Guid id, TenantDataScope? scope = null) =>
        ApplyScope(db.Leads.Include(l => l.Contact).Include(l => l.Parcel), scope)
            .FirstOrDefaultAsync(l => l.Id == id);

    public Task<Lead?> GetForDisplayAsync(Guid id, TenantDataScope? scope = null) =>
        ApplyScope(db.Leads.AsNoTracking().Include(l => l.Contact).Include(l => l.Parcel), scope)
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<IReadOnlyList<Lead>> ListAsync(
        LeadStage? stage,
        string? search,
        Guid? assignedStaffId = null,
        TenantDataScope? scope = null)
    {
        IQueryable<Lead> query = ApplyScope(db.Leads.AsNoTracking()
            .Include(l => l.Contact)
            .Include(l => l.Parcel), scope);

        if (assignedStaffId is { } staffId)
            query = query.Where(l => l.AssignedStaffId == staffId);

        if (stage is { } selectedStage)
            query = query.Where(l => l.Stage == selectedStage);

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
            query = query.Where(l => l.Contact.FullName.Contains(term) || l.Parcel.Label.Contains(term));

        return await query
            .OrderByDescending(l => l.LastActivityAtUtc)
            .ThenByDescending(l => l.Id)
            .Take(500)
            .ToListAsync();
    }

    public Task<int> CountOpenByStageAsync(LeadStage stage) =>
        db.Leads.CountAsync(lead => lead.Stage == stage
            && lead.Stage != LeadStage.Closed
            && lead.Stage != LeadStage.Lost);

    public async Task<IReadOnlyList<Lead>> ListByParcelAsync(Guid parcelId, TenantDataScope? scope = null) =>
        await ApplyScope(db.Leads.AsNoTracking()
            .Include(l => l.Contact).Include(l => l.Parcel)
            .Where(l => l.ParcelId == parcelId), scope)
            .OrderByDescending(l => l.LastActivityAtUtc)
            .ToListAsync();

    public async Task<IReadOnlyList<LeadStage>> ListStagesByParcelAsync(Guid parcelId) =>
        await db.Leads.IgnoreQueryFilters()
            .Where(lead => lead.ParcelId == parcelId)
            .Select(lead => lead.Stage)
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

    public async Task<IReadOnlyList<Lead>> ListByContactAsync(Guid contactId, TenantDataScope? scope = null) =>
        await ApplyScope(db.Leads.AsNoTracking()
            .Include(l => l.Contact).Include(l => l.Parcel)
            .Where(l => l.ContactId == contactId), scope)
            .OrderByDescending(l => l.LastActivityAtUtc)
            .ToListAsync();

    public void Add(Lead lead) => db.Leads.Add(lead);

    private static IQueryable<Lead> ApplyScope(IQueryable<Lead> query, TenantDataScope? scope) =>
        scope?.Kind switch
        {
            TenantDataScopeKind.Agent when scope.Value.StaffId is { } staffId =>
                query.Where(lead => lead.AssignedStaffId == staffId),
            TenantDataScopeKind.Branch when scope.Value.BranchId is { } branchId =>
                query.Where(lead => lead.BranchId == branchId),
            _ => query
        };
}

internal sealed class InteractionRepository(TenantDbContext db) : IInteractionRepository
{
    public async Task<IReadOnlyList<Interaction>> ListAsync(
        Guid? leadId,
        InteractionType? type,
        TenantDataScope? scope = null)
    {
        IQueryable<Interaction> query = ApplyScope(db.Interactions.AsNoTracking()
            .Include(i => i.Lead).ThenInclude(l => l.Contact)
            .Include(i => i.Lead).ThenInclude(l => l.Parcel), scope);

        if (leadId is { } selectedLeadId)
            query = query.Where(i => i.LeadId == selectedLeadId);
        if (type is { } selectedType)
            query = query.Where(i => i.Type == selectedType);

        return await query
            .OrderByDescending(i => i.OccurredAtUtc)
            .ThenByDescending(i => i.Id)
            .Take(500)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Interaction>> ListByLeadAsync(Guid leadId, TenantDataScope? scope = null) =>
        await ApplyScope(db.Interactions.AsNoTracking()
            .Include(i => i.Lead).ThenInclude(l => l.Contact)
            .Include(i => i.Lead).ThenInclude(l => l.Parcel)
            .Where(i => i.LeadId == leadId), scope)
            .OrderByDescending(i => i.OccurredAtUtc)
            .ThenByDescending(i => i.Id)
            .ToListAsync();

    public void Add(Interaction interaction) => db.Interactions.Add(interaction);

    private static IQueryable<Interaction> ApplyScope(IQueryable<Interaction> query, TenantDataScope? scope) =>
        scope?.Kind switch
        {
            TenantDataScopeKind.Agent when scope.Value.StaffId is { } staffId =>
                query.Where(interaction => interaction.Lead.AssignedStaffId == staffId),
            TenantDataScopeKind.Branch when scope.Value.BranchId is { } branchId =>
                query.Where(interaction => interaction.Lead.BranchId == branchId),
            _ => query
        };
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

internal sealed class DealRepository(TenantDbContext db) : IDealRepository
{
    public Task<Deal?> GetAsync(Guid id) =>
        db.Deals
            .Include(d => d.Lead).ThenInclude(l => l.Contact)
            .Include(d => d.Lead).ThenInclude(l => l.Parcel)
            .Include(d => d.Payments)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<IReadOnlyList<Deal>> ListAsync(Guid? agentStaffId, Guid? branchId)
    {
        IQueryable<Deal> query = db.Deals.AsNoTracking()
            .Include(d => d.Lead).ThenInclude(l => l.Contact)
            .Include(d => d.Lead).ThenInclude(l => l.Parcel)
            .Include(d => d.Payments);

        if (agentStaffId is { } staffId)
            query = query.Where(d => d.AgentStaffId == staffId);
        if (branchId is { } selectedBranchId)
            query = query.Where(d => d.BranchId == selectedBranchId);

        return await query
            .OrderByDescending(d => d.CreatedAtUtc)
            .ThenByDescending(d => d.Id)
            .Take(500)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Deal>> ListFullyPaidMissingCommissionAsync() =>
        await db.Deals.AsNoTracking()
            .Include(deal => deal.Lead).ThenInclude(lead => lead.Contact)
            .Include(deal => deal.Lead).ThenInclude(lead => lead.Parcel)
            .Where(deal => deal.Status == DealStatus.FullyPaid
                && deal.ConfirmedAtUtc != null
                && !db.Commissions.Any(commission => commission.DealId == deal.Id))
            .OrderBy(deal => deal.ConfirmedAtUtc)
            .Take(500)
            .ToListAsync();

    public Task<bool> ActiveForParcelExistsAsync(Guid parcelId) =>
        db.Deals.AnyAsync(d => d.ParcelId == parcelId && d.IsActive);

    public Task<Deal?> GetActiveForParcelAsync(Guid parcelId) =>
        db.Deals
            .Include(deal => deal.Lead).ThenInclude(lead => lead.Contact)
            .Include(deal => deal.Lead).ThenInclude(lead => lead.Parcel)
            .Include(deal => deal.Payments)
            .FirstOrDefaultAsync(deal => deal.ParcelId == parcelId && deal.IsActive);

    public Task<bool> HasOtherLiveDealForContactAsync(Guid contactId, Guid exceptDealId) =>
        db.Deals.AnyAsync(deal => deal.ContactId == contactId && deal.Id != exceptDealId
            && (deal.Status == DealStatus.Current
                || deal.Status == DealStatus.Overdue
                || deal.Status == DealStatus.FullyPaid));

    public async Task<IReadOnlyList<DealStatus>> ListStatusesByParcelAsync(Guid parcelId) =>
        await db.Deals.IgnoreQueryFilters()
            .Where(deal => deal.ParcelId == parcelId)
            .Select(deal => deal.Status)
            .ToListAsync();

    public async Task<IReadOnlyList<Deal>> ListByLeadAsync(Guid leadId) =>
        await db.Deals.AsNoTracking()
            .Include(deal => deal.Lead).ThenInclude(lead => lead.Contact)
            .Include(deal => deal.Lead).ThenInclude(lead => lead.Parcel)
            .Include(deal => deal.Payments)
            .Include(deal => deal.Parcel)
            .Where(deal => deal.LeadId == leadId)
            .OrderByDescending(deal => deal.CreatedAtUtc)
            .ToListAsync();

    public async Task<IReadOnlyList<Deal>> ListByContactAsync(Guid contactId, TenantDataScope? scope = null)
    {
        IQueryable<Deal> query = db.Deals.AsNoTracking()
            .Include(deal => deal.Lead).ThenInclude(lead => lead.Contact)
            .Include(deal => deal.Lead).ThenInclude(lead => lead.Parcel)
            .Include(deal => deal.Payments)
            .Include(deal => deal.Parcel)
            .Where(deal => deal.ContactId == contactId);

        if (scope?.Kind == TenantDataScopeKind.Agent && scope.Value.StaffId is { } staffId)
            query = query.Where(deal => deal.AgentStaffId == staffId);
        else if (scope?.Kind == TenantDataScopeKind.Branch && scope.Value.BranchId is { } branchId)
            query = query.Where(deal => deal.BranchId == branchId);

        return await query
            .OrderByDescending(deal => deal.ConfirmedAtUtc ?? deal.CreatedAtUtc)
            .ThenByDescending(deal => deal.Id)
            .ToListAsync();
    }

    public void Add(Deal deal) => db.Deals.Add(deal);

    public void Touch(Deal deal) =>
        db.Entry(deal).Property(item => item.Version).IsModified = true;
}

internal sealed class PaymentRepository(TenantDbContext db) : IPaymentRepository
{
    public Task<Payment?> GetAsync(Guid id) =>
        db.Payments
            .Include(payment => payment.Deal).ThenInclude(deal => deal.Parcel)
            .Include(payment => payment.Deal).ThenInclude(deal => deal.Payments)
            .FirstOrDefaultAsync(payment => payment.Id == id);

    public async Task<IReadOnlyList<Payment>> ListByDealAsync(Guid dealId) =>
        await db.Payments.AsNoTracking()
            .Where(payment => payment.DealId == dealId)
            .OrderByDescending(payment => payment.PaidAtUtc)
            .ThenByDescending(payment => payment.Id)
            .Take(500)
            .ToListAsync();

    public Task<bool> ReceiptExistsAsync(string receiptNo) =>
        db.Payments.AnyAsync(payment => payment.ReceiptNo == receiptNo);

    public void Add(Payment payment) => db.Payments.Add(payment);
}
