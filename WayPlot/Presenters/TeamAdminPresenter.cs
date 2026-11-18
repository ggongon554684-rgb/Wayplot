using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Services.Admin;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.Presenters;

public sealed record ReassignmentChoice(Guid Id, string Display, Guid? AssignedStaffId)
{
    public override string ToString() => Display;
}

public sealed record TeamAdminData(
    IReadOnlyList<StaffListItem> Staff,
    IReadOnlyList<BranchListItem> Branches,
    IReadOnlyList<ParcelAgentOption> ActiveAgents,
    IReadOnlyList<ReassignmentChoice> Leads,
    IReadOnlyList<ReassignmentChoice> Contacts,
    IReadOnlyList<ReassignmentChoice> Parcels);

public sealed class TeamAdminPresenter(
    StaffService staff,
    LeadService leads,
    ContactService contacts,
    ParcelService parcels,
    CompanyProfileService companyProfile,
    bool hasBranching,
    Guid? currentStaffId)
{
    public bool HasBranching => hasBranching;
    public Guid? CurrentStaffId => currentStaffId;

    public async Task<ServiceResult<TeamAdminData>> LoadAsync()
    {
        try
        {
            var staffResult = await staff.ListStaffAsync();
            if (!staffResult.Ok || staffResult.Value is null)
                return ServiceResult<TeamAdminData>.Fail(staffResult.Message);
            var branches = hasBranching
                ? await staff.ListBranchesAsync()
                : ServiceResult<IReadOnlyList<BranchListItem>>.Success(Array.Empty<BranchListItem>());
            if (!branches.Ok || branches.Value is null)
                return ServiceResult<TeamAdminData>.Fail(branches.Message);
            var agents = await parcels.ListActiveAgentsAsync();
            if (!agents.Ok || agents.Value is null)
                return ServiceResult<TeamAdminData>.Fail(agents.Message);
            var leadList = await leads.ListAsync(null, null);
            if (!leadList.Ok || leadList.Value is null)
                return ServiceResult<TeamAdminData>.Fail(leadList.Message);
            var people = await contacts.ListPeopleAsync(new PeopleFilter(
                new HashSet<PersonKind> { PersonKind.Customer, PersonKind.Client },
                Limit: 1000));
            if (!people.Ok || people.Value is null)
                return ServiceResult<TeamAdminData>.Fail(people.Message);
            var parcelList = await parcels.ListAsync();
            if (!parcelList.Ok || parcelList.Value is null)
                return ServiceResult<TeamAdminData>.Fail(parcelList.Message);

            var leadChoices = leadList.Value
                .Where(lead => lead.Stage is not (LeadStage.Closed or LeadStage.Lost))
                .Select(lead => new ReassignmentChoice(
                    lead.Id,
                    $"{lead.ContactName} — {lead.ParcelLabel}",
                    null))
                .ToList();
            var contactChoices = people.Value
                .Where(person => person.Kind is PersonKind.Customer or PersonKind.Client)
                .Select(person => new ReassignmentChoice(person.Id, person.Name, null))
                .ToList();
            var parcelChoices = parcelList.Value
                .Select(parcel => new ReassignmentChoice(
                    parcel.Id, $"{parcel.Label} — {parcel.Status}", parcel.AssignedStaffId))
                .ToList();

            return ServiceResult<TeamAdminData>.Success(new TeamAdminData(
                staffResult.Value,
                branches.Value,
                agents.Value,
                leadChoices,
                contactChoices,
                parcelChoices));
        }
        catch (Exception)
        {
            return ServiceResult<TeamAdminData>.Fail(
                "The Team screen could not be loaded. Check the connection and try again.");
        }
    }

    public Task<ServiceResult<bool>> DeactivateAsync(StaffListItem member) =>
        staff.DeactivateAsync(member.Id, member.Version);

    public Task<ServiceResult<bool>> ReactivateAsync(StaffListItem member) =>
        staff.ReactivateAsync(member.Id, member.Version);

    public Task<ServiceResult<bool>> ChangeBranchAsync(StaffListItem member, Guid branchId) =>
        staff.ChangeBranchAsync(member.Id, branchId, member.Version);

    public Task<ServiceResult<bool>> ReassignLeadAsync(Guid leadId, Guid agentId) =>
        leads.ReassignLeadAsync(leadId, agentId);

    public Task<ServiceResult<bool>> ReassignContactAsync(
        Guid contactId, Guid agentId, bool includeOpenLeads) =>
        leads.ReassignContactAsync(contactId, agentId, includeOpenLeads);

    public Task<ServiceResult<ParcelDto>> ReassignParcelAsync(Guid parcelId, Guid agentId) =>
        parcels.ReassignAsync(new ReassignParcelRequest(parcelId, agentId));

    public Task<ServiceResult<ReassignmentSummary>> PreviewReassignAllAsync(Guid from, Guid to) =>
        leads.PreviewReassignAllFromAgentAsync(from, to);

    public Task<ServiceResult<ReassignmentSummary>> ReassignAllAsync(Guid from, Guid to) =>
        leads.ReassignAllFromAgentAsync(from, to);

    public Task<ServiceResult<CompanyProfileSnapshot>> GetCompanyProfileAsync() =>
        companyProfile.GetAsync();

    public Task<ServiceResult<CompanyProfileSnapshot>> SaveCompanyProfileAsync(CompanyProfileRequest request) =>
        companyProfile.SaveAsync(request);
}
