using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.Presenters;

public interface ILeadsView
{
    bool IsDisposed { get; }
    void ShowLeads(IReadOnlyList<LeadListItem> leads);
    void ShowStatus(string text, bool isError);
}

/// <summary>Loads the filtered lead list and delegates lead-file operations to LeadService.</summary>
public sealed class LeadsPresenter(ILeadsView view, LeadService leads, InteractionService interactions)
{
    private int _latest;

    public async Task LoadAsync(LeadStage? stage, string? search)
    {
        var ticket = ++_latest;
        if (view.IsDisposed) return;
        view.ShowStatus("Loading leads...", false);

        try
        {
            var result = await leads.ListAsync(stage, search);
            if (ticket != _latest || view.IsDisposed) return;

            if (!result.Ok || result.Value is null)
            {
                view.ShowStatus(result.Message, true);
                return;
            }

            view.ShowLeads(result.Value);
            view.ShowStatus(Summary(result.Value.Count, stage, search), false);
        }
        catch (Exception)
        {
            if (ticket == _latest && !view.IsDisposed)
                view.ShowStatus("Could not load leads. Check the database connection and try again.", true);
        }
    }

    public async Task<ServiceResult<LeadFileDto>> GetLeadFileAsync(Guid leadId)
    {
        try
        {
            return await leads.GetFileAsync(leadId);
        }
        catch (Exception)
        {
            return ServiceResult<LeadFileDto>.Fail(
                "Could not load the lead file. Check the database connection and try again.");
        }
    }

    public async Task<ServiceResult<LeadDto>> MoveStageAsync(MoveStageRequest request)
    {
        try
        {
            return await leads.MoveStageAsync(request);
        }
        catch (Exception)
        {
            return ServiceResult<LeadDto>.Fail(
                "The stage could not be changed. Check the database connection and try again.");
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

    public async Task<ServiceResult<InteractionListItem>> LogInteractionAsync(LogInteractionRequest request)
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

    private static string Summary(int count, LeadStage? stage, string? search)
    {
        if (count == 0)
        {
            if (!string.IsNullOrWhiteSpace(search))
                return $"No leads match \"{search.Trim()}\".";
            return stage is { } selectedStage
                ? $"No leads match the {selectedStage} stage."
                : "No leads yet.";
        }

        return count == 1 ? "1 lead" : $"{count} leads";
    }
}
