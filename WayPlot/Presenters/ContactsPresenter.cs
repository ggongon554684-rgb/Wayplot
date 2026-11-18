using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.Presenters;

public interface IContactsView
{
    void ShowContacts(IReadOnlyList<ContactListItem> contacts);
    void ShowStatus(string text, bool isError);
    bool IsDisposed { get; }
}

/// <summary>Loads and searches the contact list and saves new contacts. All rules stay in ContactService.</summary>
public sealed class ContactsPresenter(
    IContactsView view,
    ContactService contacts,
    ContactImportService imports,
    ICurrentUser user,
    TenantAccessPolicy access)
{
    private int _latest;

    public bool CanImport => (user.Role is Roles.Agent or Roles.TenantAdmin) && user.UserId is not null;
    public bool CanAssignImportedContacts => user.Role == Roles.TenantAdmin;
    public bool CanCapture => access.CanMutateData;
    public string CaptureUnavailableReason => access.MutationUnavailableReason;

    /// <summary>notice replaces the usual "N contacts" line (used to show the result of a save).</summary>
    public async Task LoadAsync(string? search = null, string? notice = null)
    {
        var ticket = ++_latest;
        try
        {
            if (view.IsDisposed) return;
            var result = await contacts.ListAsync(search);
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
        var noun = count == 1 ? "1 contact" : $"{count} contacts";
        return searching ? noun + " found" : noun;
    }
}
