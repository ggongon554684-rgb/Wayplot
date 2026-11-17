using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.Presenters;

/// <summary>What the presenter needs from the Parcel map screen. Every method may be called after the screen was closed,
/// so the view ignores calls once it is disposed.</summary>
public interface IParcelMapView
{
    bool IsDisposed { get; }

    /// <summary>Text drawn in the middle of the map (loading, errors, empty). Null clears it.</summary>
    void ShowMessage(string? text);

    void ShowShapes(IReadOnlyList<MapShape> shapes, int unplotted);

    /// <summary>Fills the inspector. leads is null while they are still loading.</summary>
    void ShowParcel(ParcelDto parcel, IReadOnlyList<LeadDto>? leads);

    void ShowLeadsError(string message);

    void ClearParcel();
}

/// <summary>Loads the parcels, turns them into map shapes, and fills the inspector when one is selected.
/// All data comes from the services; the map and the inspector only draw what they are given.</summary>
public sealed class ParcelMapPresenter(
    IParcelMapView view,
    ParcelService parcels,
    LeadService leads,
    ContactService contacts)
{
    private readonly Dictionary<Guid, ParcelDto> _byId = [];
    private int _selectionToken;
    private int _loadToken;
    private Guid? _selectedParcelId;

    public async Task LoadAsync()
    {
        var token = ++_loadToken;
        if (view.IsDisposed) return;
        view.ShowMessage("Loading parcels...");
        try
        {
            var list = await parcels.ListAsync();
            if (token != _loadToken || view.IsDisposed) return;

            _byId.Clear();
            var shapes = new List<MapShape>();
            var unplotted = 0;
            foreach (var parcel in list)
            {
                _byId[parcel.Id] = parcel;
                var shape = ParcelShapeBuilder.Build(parcel);
                if (shape is null) unplotted++;
                else shapes.Add(shape);
            }

            view.ShowShapes(shapes, unplotted);
            view.ShowMessage(shapes.Count == 0 ? "No parcels have a map position yet." : null);
        }
        catch (Exception ex)
        {
            if (token == _loadToken && !view.IsDisposed)
                view.ShowMessage($"Could not load the parcels. {ex.Message}");
        }
    }

    /// <summary>Called when the agent clicks a parcel (or clicks empty map, id = null).</summary>
    public async Task SelectAsync(Guid? id)
    {
        var token = ++_selectionToken;
        _selectedParcelId = id;

        if (view.IsDisposed) return;
        if (id is null || !_byId.TryGetValue(id.Value, out var parcel))
        {
            view.ClearParcel();
            return;
        }

        // The parcel's own facts are already loaded, so show them at once. The leads follow.
        view.ShowParcel(parcel, null);

        try
        {
            var list = await leads.ListByParcelAsync(parcel.Id);
            if (token != _selectionToken || view.IsDisposed) return;   // the agent clicked something else meanwhile
            view.ShowParcel(parcel, list);
        }
        catch (Exception ex)
        {
            if (token != _selectionToken || view.IsDisposed) return;
            view.ShowLeadsError($"Could not load the leads. {ex.Message}");
        }
    }

    public async Task<ServiceResult<IReadOnlyList<ContactListItem>>> SearchContactsAsync(string? search)
    {
        try
        {
            return await contacts.ListAsync(search);
        }
        catch (Exception)
        {
            return ServiceResult<IReadOnlyList<ContactListItem>>.Fail(
                "Could not search contacts. Check the database connection and try again.");
        }
    }

    public async Task<ServiceResult<LeadDto>> AttachLeadAsync(Guid contactId, Guid parcelId)
    {
        try
        {
            var result = await leads.AttachContactToParcelAsync(new AttachLeadRequest(contactId, parcelId));
            if (result.Ok)
                await RefreshSelectedAsync();
            return result;
        }
        catch (Exception)
        {
            return ServiceResult<LeadDto>.Fail("The lead could not be attached. Check the database connection and try again.");
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
            return ServiceResult<LeadFileDto>.Fail("Could not load the lead file. Check the database connection and try again.");
        }
    }

    public async Task<ServiceResult<LeadDto>> MoveStageAsync(MoveStageRequest request)
    {
        try
        {
            var result = await leads.MoveStageAsync(request);
            if (result.Ok)
                await RefreshSelectedAsync();
            return result;
        }
        catch (Exception)
        {
            return ServiceResult<LeadDto>.Fail("The stage could not be changed. Check the database connection and try again.");
        }
    }

    private async Task RefreshSelectedAsync()
    {
        var selected = _selectedParcelId;
        var selectionToken = _selectionToken;
        await LoadAsync();

        if (view.IsDisposed || selectionToken != _selectionToken || selected != _selectedParcelId)
            return;
        if (selected is not null)
            await SelectAsync(selected);
    }
}