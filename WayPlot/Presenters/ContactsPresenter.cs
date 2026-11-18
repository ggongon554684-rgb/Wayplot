using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Services.DataCollection;
using WayPlot.domain.Services.Admin;
using WayPlot.domain.Services.MainTransaction;

namespace WayPlot.Presenters;

public interface IContactsView
{
    void ShowContacts(IReadOnlyList<PeopleListItemDto> people);
    void ShowStatus(string text, bool isError);
    bool IsDisposed { get; }
}

/// <summary>Loads and searches the contact list and saves new contacts. All rules stay in ContactService.</summary>
public sealed class ContactsPresenter(
    IContactsView view,
    ContactService contacts,
    ContactImportService imports,
    LeadService leads,
    ParcelService parcels,
    StaffService staff,
    DealService deals,
    ICurrentUser user,
    TenantAccessPolicy access)
{
    private int _latest;

    public bool CanImport => (user.Role is Roles.Agent or Roles.TenantAdmin) && user.UserId is not null;
    public bool CanAssignImportedContacts => user.Role == Roles.TenantAdmin;
    public bool CanCapture => access.CanMutateData;
    public string CaptureUnavailableReason => access.MutationUnavailableReason;
    public bool IsAdmin => user.Role == Roles.TenantAdmin;
    public bool IsAgent => user.Role == Roles.Agent;
    public bool CanAddOrSwitchLeads => access.CanMutateData;
    public string LeadMutationUnavailableReason => access.MutationUnavailableReason;

    private IReadOnlySet<PersonKind> _kinds = new HashSet<PersonKind>();
    private Guid? _agentFilter;

    public void SetKindFilter(IReadOnlySet<PersonKind> kinds) => _kinds = kinds;
    public void SetAgentFilter(Guid? staffId) => _agentFilter = staffId;

    /// <summary>notice replaces the usual "N contacts" line (used to show the result of a save).</summary>
    public async Task LoadAsync(string? search = null, string? notice = null)
    {
        var ticket = ++_latest;
        try
        {
            if (view.IsDisposed) return;
            var result = await contacts.ListPeopleAsync(new PeopleFilter(
                _kinds,
                search,
                IsAdmin ? _agentFilter : null,
                200));
            if (ticket != _latest || view.IsDisposed) return;   // a newer search is running; ignore this older answer

            if (!result.Ok || result.Value is null)
            {
                view.ShowStatus(result.Message ?? "Could not load contacts.", true);
                return;
            }

            view.ShowContacts(result.Value);
            view.ShowStatus(notice ?? Summary(result.Value.Count, search), false);
        }
        catch (Exception)
        {
            if (ticket == _latest && !view.IsDisposed)
                view.ShowStatus("Could not load contacts. Check the database connection and try again.", true);
        }
    }

    public async Task<ServiceResult<IReadOnlyList<StaffListItem>>> ListStaffAsync() =>
        await staff.ListStaffAsync();

    public async Task<ServiceResult<ContactListItem>> GetContactAsync(Guid contactId) =>
        await contacts.GetAsync(contactId);

    public async Task<ServiceResult<IReadOnlyList<ContactLeadCardDto>>> ListByContactAsync(Guid contactId) =>
        await leads.ListByContactAsync(contactId);

    public async Task<ServiceResult<IReadOnlyList<ParcelDto>>> ListParcelsAsync() =>
        await parcels.ListAsync();

    public async Task<ServiceResult<Guid>> AddLeadAsync(Guid contactId, Guid parcelId, string? note, Guid? agentId) =>
        await leads.AddLeadForContactAsync(contactId, parcelId, note, agentId);

    public async Task<ServiceResult<Guid>> SwitchLotAsync(Guid leadId, Guid parcelId, string? reason) =>
        await leads.SwitchLotAsync(leadId, parcelId, reason);

    public async Task<ServiceResult<IReadOnlyList<DealSummaryDto>>> GetDealSummariesAsync(Guid contactId) =>
        await deals.GetDealSummariesForContactAsync(contactId);

    public async Task<ServiceResult<IReadOnlyList<DealPaymentDto>>> GetPaymentsAsync(Guid dealId) =>
        await deals.GetPaymentsAsync(dealId);

    public async Task<ServiceResult<IReadOnlyList<LeadMoveOptionDto>>> GetAllowedMovesAsync(Guid leadId) =>
        await leads.GetAllowedMovesAsync(leadId);

    public async Task<ServiceResult<IReadOnlyList<DealCandidateItem>>> GetDealCandidatesAsync() =>
        await deals.ListCandidatesAsync();

    public async Task<ServiceResult<DealListItem>> RequestClosedAsync(CreateDealRequest request) =>
        await deals.RequestClosedAsync(request);

    public async Task<ServiceResult<DealListItem>> ConfirmClosedAsync(Guid dealId, ParcelStatus status) =>
        await deals.ConfirmClosedAsync(dealId, status);

    public async Task<ServiceResult<DealListItem>> ReturnRequestAsync(Guid dealId) =>
        await deals.ReturnRequestAsync(dealId);

    public async Task<ServiceResult<PaymentListItem>> RecordPaymentAsync(RecordPaymentRequest request) =>
        await deals.RecordPaymentAsync(request);

    public async Task<ServiceResult<PaymentListItem>> ConfirmPaymentAsync(Guid paymentId) =>
        await deals.ConfirmPaymentAsync(paymentId);

    public async Task<ServiceResult<PaymentListItem>> ReturnPaymentAsync(Guid paymentId, string? note) =>
        await deals.ReturnPaymentAsync(paymentId, note);

    public async Task<ServiceResult<DealListItem>> CancelDealAsync(Guid dealId, string reason,
        DealMoneyOutcome? outcome, decimal? refundedAmount) =>
        await deals.CancelDealAsync(dealId, reason, outcome, refundedAmount);

    public async Task<ServiceResult<DealListItem>> DefaultDealAsync(Guid dealId, string reason,
        DealMoneyOutcome? outcome, decimal? refundedAmount) =>
        await deals.DefaultDealAsync(dealId, reason, outcome, refundedAmount);

    public async Task<ServiceResult<DealTerminationSummary>> GetTerminationSummaryAsync(Guid dealId) =>
        await deals.GetTerminationSummaryAsync(dealId);

    public async Task RefreshDealStatusesAsync()
    {
        try { await deals.RefreshDealStatusesAsync(); }
        catch { }
    }

    public async Task<ServiceResult<CaptureContactResult>> CaptureAsync(CaptureContactRequest request)
    {
        try
        {
            return await contacts.CaptureAsync(request);
        }
        catch (Exception)
        {
            // For example two people saving the same phone at once: the database refuses the second one.
            return ServiceResult<CaptureContactResult>.Fail("The contact could not be saved. Nothing was changed. Try again.");
        }
    }

    public async Task<ServiceResult<ContactReferralOptions>> GetReferralOptionsAsync()
    {
        try
        {
            return await contacts.ListReferralOptionsAsync();
        }
        catch (Exception)
        {
            return ServiceResult<ContactReferralOptions>.Fail(
                "Could not load referrer choices. Check the database connection and try again.");
        }
    }

    public ServiceResult<IReadOnlyList<ImportCsvColumn>> ReadImportColumns(string csvContent)
    {
        try
        {
            return imports.ReadColumns(csvContent);
        }
        catch (Exception)
        {
            return ServiceResult<IReadOnlyList<ImportCsvColumn>>.Fail("Could not read the CSV file.");
        }
    }

    public async Task<ServiceResult<ContactImportPreview>> PreviewImportAsync(
        string csvContent,
        ContactImportMapping mapping)
    {
        try
        {
            return await imports.PreviewAsync(csvContent, mapping);
        }
        catch (Exception)
        {
            return ServiceResult<ContactImportPreview>.Fail("Could not preview the import. Check the database connection and try again.");
        }
    }

    public async Task<ServiceResult<ContactImportSummary>> ImportCsvAsync(
        string csvContent,
        ContactImportMapping mapping)
    {
        try
        {
            return await imports.ImportAsync(csvContent, mapping);
        }
        catch (Exception)
        {
            return ServiceResult<ContactImportSummary>.Fail("The import could not be completed. Check the database connection and try again.");
        }
    }

    private static string Summary(int count, string? search)
    {
        var searching = !string.IsNullOrWhiteSpace(search);
        if (count == 0)
            return searching ? $"No contacts match \"{search!.Trim()}\"." : "No contacts yet. Use New contact to add one.";
        if (count == 200)
            return "Showing the first 200. Refine the search.";
        var noun = count == 1 ? "1 person" : $"{count} people";
        return searching ? noun + " found" : noun;
    }
}
