using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Services.DataCollection;
using WayPlot.domain.Services.MainTransaction;
using WayPlot.domain.Services.Admin;

namespace WayPlot.Presenters;

public interface IPipelineView
{
    bool IsDisposed { get; }
    void ShowBoard(PipelineBoardDto board);
    void ShowStatus(string text, bool isError);
}

public sealed class PipelinePresenter(
    IPipelineView view,
    LeadService leads,
    InteractionService interactions,
    ParcelService parcels,
    DealService deals,
    StaffService staff,
    IPipelineStageProvider stages,
    ICurrentUser user,
    TenantAccessPolicy access)
{
    private int _latest;

    public bool IsAdmin => user.Role == Roles.TenantAdmin;
    public bool CanMutate => access.CanMutateData;
    public string MutationUnavailableReason => access.MutationUnavailableReason;
    public IReadOnlyList<PipelineStageInfo> Stages => stages.GetStages();

    public async Task LoadAsync(PipelineFilter filter)
    {
        var ticket = ++_latest;
        if (view.IsDisposed) return;
        view.ShowStatus("Loading pipeline...", false);
        try
        {
            var result = await leads.GetPipelineBoardAsync(filter);
            if (ticket != _latest || view.IsDisposed) return;
            if (!result.Ok || result.Value is null)
            {
                view.ShowStatus(result.Message, true);
                return;
            }
            view.ShowBoard(result.Value);
            view.ShowStatus("", false);
        }
        catch (Exception)
        {
            if (ticket == _latest && !view.IsDisposed)
                view.ShowStatus("Could not load the pipeline. Try again.", true);
        }
    }

    public Task<ServiceResult<IReadOnlyList<LeadMoveOptionDto>>> AllowedMovesAsync(Guid leadId) =>
        leads.GetAllowedMovesAsync(leadId);

    public Task<ServiceResult<LeadDto>> MoveAsync(MoveStageRequest request) =>
        leads.MoveStageAsync(request);

    public Task<ServiceResult<IReadOnlyList<LeadMoveOptionDto>>> GetAllowedMovesAsync(Guid leadId) =>
        leads.GetAllowedMovesAsync(leadId);

    public Task<ServiceResult<IReadOnlyList<DealCandidateItem>>> DealCandidatesAsync() =>
        deals.ListCandidatesAsync();

    public Task<ServiceResult<Guid>> SwitchLotAsync(Guid leadId, Guid parcelId, string? reason) =>
        leads.SwitchLotAsync(leadId, parcelId, reason);

    public Task<ServiceResult<IReadOnlyList<TimelineItemDto>>> TimelineAsync(Guid leadId) =>
        leads.GetLeadTimelineAsync(leadId);

    public Task<ServiceResult<LeadFileDto>> LeadFileAsync(Guid leadId) =>
        leads.GetFileAsync(leadId);

    public Task<ServiceResult<IReadOnlyList<ParcelDto>>> ParcelsAsync() =>
        parcels.ListAsync();

    public Task<ServiceResult<IReadOnlyList<StaffListItem>>> StaffAsync() =>
        staff.ListStaffAsync();

    public Task<ServiceResult<IReadOnlyList<LeadListItem>>> SearchLeadsAsync(string? search) =>
        leads.ListAsync(null, search);

    public Task<ServiceResult<InteractionListItem>> LogAsync(LogInteractionRequest request) =>
        interactions.LogAsync(request);

    public Task<ServiceResult<DealListItem>> RequestClosedAsync(CreateDealRequest request) =>
        deals.RequestClosedAsync(request);
}
