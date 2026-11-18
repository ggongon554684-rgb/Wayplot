using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.Presenters;

public interface IInteractionsView
{
    bool IsDisposed { get; }
    void ShowInteractions(IReadOnlyList<InteractionListItem> interactions);
    void ShowStatus(string text, bool isError);
}

/// <summary>Loads interaction history and delegates logging and lead search to domain services.</summary>
public sealed class InteractionsPresenter(
    IInteractionsView view,
    InteractionService interactions,
    LeadService leads,
    TenantAccessPolicy access)
{
    private int _latest;

    public bool CanLogInteractions => access.CanMutateData;
    public string MutationUnavailableReason => access.MutationUnavailableReason;

    public async Task LoadAsync(InteractionType? type, string? notice = null)
    {
        var ticket = ++_latest;
        if (view.IsDisposed) return;
        view.ShowStatus("Loading interactions...", false);

        try
        {
            var result = await interactions.ListAsync(null, type);
            if (ticket != _latest || view.IsDisposed) return;
            if (!result.Ok || result.Value is null)
            {
                view.ShowStatus(result.Message, true);
                return;
            }

            view.ShowInteractions(result.Value);
            view.ShowStatus(notice ?? Summary(result.Value.Count, type), false);
        }
        catch (Exception)
        {
            if (ticket == _latest && !view.IsDisposed)
                view.ShowStatus("Could not load interactions. Check the database connection and try again.", true);
        }
    }

    public async Task<ServiceResult<IReadOnlyList<LeadListItem>>> SearchLeadsAsync(string? search)
    {
        try
        {
            return await leads.ListAsync(null, search);
        }
        catch (Exception)
        {
            return ServiceResult<IReadOnlyList<LeadListItem>>.Fail(
                "Could not search leads. Check the database connection and try again.");
        }
    }

    public async Task<ServiceResult<InteractionListItem>> LogAsync(LogInteractionRequest request)
    {
        try
        {
            return await interactions.LogAsync(request);
        }
        catch (Exception)
        {
            return ServiceResult<InteractionListItem>.Fail(
                "The interaction could not be logged. Check the database connection and try again.");
        }
    }

    private static string Summary(int count, InteractionType? type)
    {
        if (count == 0)
            return type is { } selectedType
                ? $"No {selectedType} interactions."
                : "No interactions yet.";
        return count == 1 ? "1 interaction" : $"{count} interactions";
    }
}
