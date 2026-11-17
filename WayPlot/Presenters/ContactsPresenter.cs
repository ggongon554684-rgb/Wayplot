using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.Presenters;

public interface IContactsView
{
    void ShowContacts(IReadOnlyList<ContactListItem> contacts);
    void ShowStatus(string text, bool isError);
}

/// <summary>Loads and searches the contact list and saves new contacts. All rules stay in ContactService.</summary>
public sealed class ContactsPresenter(IContactsView view, ContactService contacts)
{
    private int _latest;

    /// <summary>notice replaces the usual "N contacts" line (used to show the result of a save).</summary>
    public async Task LoadAsync(string? search = null, string? notice = null)
    {
        var ticket = ++_latest;
        try
        {
            var result = await contacts.ListAsync(search);
            if (ticket != _latest) return;   // a newer search is running; ignore this older answer

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
            if (ticket == _latest)
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

    private static string Summary(int count, string? search)
    {
        var searching = !string.IsNullOrWhiteSpace(search);
        if (count == 0)
            return searching ? $"No contacts match \"{search!.Trim()}\"." : "No contacts yet. Use New contact to add one.";
        var noun = count == 1 ? "1 contact" : $"{count} contacts";
        return searching ? noun + " found" : noun;
    }
}