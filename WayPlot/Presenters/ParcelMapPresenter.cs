using System.Text.Json;
﻿using WayPlot.domain.Common;
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

    void ShowParcels(string featureCollectionJson, int featureCount, int unplotted);

    /// <summary>Fills the inspector. leads is null while they are still loading.</summary>
    void ShowParcel(ParcelDto parcel, IReadOnlyList<LeadDto>? leads);

    void ShowLeadsError(string message);

    void ClearParcel();
}

/// <summary>Loads the parcels, turns them into GeoJSON features, and fills the inspector when one is selected.
/// All data comes from the services; the map and the inspector only draw what they are given.</summary>
public sealed class ParcelMapPresenter(
    IParcelMapView view,
    ParcelService parcels,
    LeadService leads,
    ContactService contacts,
    InteractionService interactions,
    ParcelPhotoService photos,
    TenantAccessPolicy access)
{
    private readonly Dictionary<Guid, ParcelDto> _byId = [];
    private int _selectionToken;
    private int _loadToken;
    private Guid? _selectedParcelId;

    public bool CanShowAttachLead => access.CanMutateData;
    public bool CanAttachLeads => access.CanMutateData && _selectedParcelId is not null;
    public string? AttachUnavailableReason => !access.CanMutateData
        ? access.MutationUnavailableReason
        : _selectedParcelId is null ? "Select a parcel first." : null;
    public bool CanMutateLeads => access.CanMutateData;
    public string MutationUnavailableReason => access.MutationUnavailableReason;
    public bool CanShowParcelActions => access.CanMutateData;
    public bool CanShowAdminParcelActions => access.CanChangeParcelPrice().Ok;
    public RuleResult PlotPermission => access.CanPlotParcel();
    public bool CanPlotParcel => PlotPermission.Ok;
    public bool CanShowPhotoActions => access.CanMutateData;

    public RuleResult GetPhotoMutationPermission(Guid? parcelId)
    {
        if (parcelId is null || !_byId.TryGetValue(parcelId.Value, out var parcel))
            return RuleResult.Fail("Select a parcel first.");
        return access.CanEditParcel(parcel.AssignedStaffId);
    }

    public Task<ServiceResult<IReadOnlyList<ParcelPhotoThumbnailDto>>> ListPhotosAsync(
        Guid parcelId,
        CancellationToken cancellationToken = default) =>
        photos.ListPhotosAsync(parcelId, cancellationToken);

    public Task<ServiceResult<ParcelPhotoThumbnailDto>> AddPhotoAsync(
        Guid parcelId,
        string fileName,
        byte[] imageBytes,
        byte[] thumbnailBytes,
        CancellationToken cancellationToken = default) =>
        photos.AddPhotoAsync(parcelId, fileName, imageBytes, thumbnailBytes, cancellationToken);

    public Task<ServiceResult<bool>> RemovePhotoAsync(
        Guid parcelId,
        Guid photoId,
        CancellationToken cancellationToken = default) =>
        photos.RemovePhotoAsync(parcelId, photoId, cancellationToken);

    public Task<ServiceResult<ParcelPhotoImageDto>> GetPhotoImageAsync(
        Guid photoId,
        CancellationToken cancellationToken = default) =>
        photos.GetPhotoImageAsync(photoId, cancellationToken);

    public RuleResult GetEditPermission(Guid? parcelId)
    {
        if (parcelId is null || !_byId.TryGetValue(parcelId.Value, out var parcel))
            return RuleResult.Fail("Select a parcel first.");
        return access.CanEditParcel(parcel.AssignedStaffId);
    }

    public RuleResult GetShapeEditPermission(Guid? parcelId)
    {
        var permission = GetEditPermission(parcelId);
        if (!permission.Ok || parcelId is null || !_byId.TryGetValue(parcelId.Value, out var parcel))
            return permission;

        var geometry = GetEditableGeometry(parcel);
        return geometry.Ok
            ? RuleResult.Success()
            : RuleResult.Fail(geometry.Message);
    }

    public async Task<ServiceResult<IReadOnlyList<OwnerDto>>> ListOwnersAsync() =>
        await parcels.ListOwnersAsync();

    public async Task<ServiceResult<IReadOnlyList<ParcelAgentOption>>> ListActiveAgentsAsync() =>
        await parcels.ListActiveAgentsAsync();

    public async Task<ServiceResult<OwnerDto>> CreateOwnerAsync(CreateOwnerRequest request) =>
        await parcels.CreateOwnerAsync(request);

    public async Task<ServiceResult<ParcelDto>> CreateParcelAsync(CreateParcelRequest request)
    {
        var result = await parcels.CreateAsync(request);
        if (result.Ok && result.Value is { } parcel)
            await ReloadAndSelectSavedAsync(parcel.Id);
        return result;
    }

    public async Task<ServiceResult<ParcelDto>> UpdateParcelAsync(UpdateParcelRequest request)
    {
        var result = await parcels.UpdateAsync(request);
        if (result.Ok && result.Value is { } parcel)
            await ReloadAndSelectSavedAsync(parcel.Id);
        return result;
    }

    public async Task<ServiceResult<ParcelDto>> ChangeParcelPriceAsync(ChangeParcelPriceRequest request)
    {
        var result = await parcels.ChangePriceAsync(request);
        if (result.Ok && result.Value is { } parcel)
            await ReloadAndSelectSavedAsync(parcel.Id);
        return result;
    }

    public async Task<ServiceResult<ParcelDto>> ReassignParcelAsync(ReassignParcelRequest request)
    {
        var result = await parcels.ReassignAsync(request);
        if (result.Ok && result.Value is { } parcel)
            await ReloadAndSelectSavedAsync(parcel.Id);
        return result;
    }

    public async Task<ServiceResult<bool>> ArchiveParcelAsync(Guid parcelId)
    {
        var result = await parcels.ArchiveAsync(parcelId);
        if (result.Ok)
        {
            await LoadAsync();
            await SelectAsync(null);
        }
        return result;
    }

    public async Task<ServiceResult<ParcelDto>> SaveDrawnParcelAsync(
        CreateParcelRequest request,
        string kind,
        IReadOnlyList<ParcelCoordinate> points)
    {
        if (kind != "polygon")
            return ServiceResult<ParcelDto>.Fail("New parcels must be drawn as polygons; marker geometry is not supported.");

        var validation = ParcelGeometry.Validate(kind, points);
        if (!validation.Ok)
            return ServiceResult<ParcelDto>.Fail(validation.Message);
        return await CreateParcelAsync(request);
    }

    public async Task<ServiceResult<ParcelDto>> SaveEditedGeometryAsync(
        Guid parcelId,
        string kind,
        IReadOnlyList<ParcelCoordinate> points)
    {
        var validation = ParcelGeometry.Validate(kind, points);
        if (!validation.Ok)
            return ServiceResult<ParcelDto>.Fail(validation.Message);
        if (!_byId.TryGetValue(parcelId, out var parcel))
            return ServiceResult<ParcelDto>.Fail("That parcel is no longer available. Reload the map.");

        var permission = access.CanEditParcel(parcel.AssignedStaffId);
        if (!permission.Ok)
            return ServiceResult<ParcelDto>.Fail(permission.Message);

        var geoJson = kind == "polygon" ? ParcelGeometry.BuildPolygonGeoJson(points) : null;
        var marker = kind == "marker" ? points[0] : (ParcelCoordinate?)null;
        var areaSqm = kind == "polygon" ? ParcelGeometry.ComputeAreaSqm(points) : parcel.AreaSqm;
        return await UpdateParcelAsync(new UpdateParcelRequest(
            parcel.Id,
            parcel.Label,
            parcel.Description,
            areaSqm,
            parcel.Zoning,
            geoJson,
            marker?.Latitude,
            marker?.Longitude,
            parcel.TaxDeclarationNo,
            parcel.TitleNo,
            parcel.Utilities,
            parcel.RoadAccess,
            parcel.OwnerId));
    }

    public async Task LoadAsync()
    {
        var token = ++_loadToken;
        if (view.IsDisposed) return;
        view.ShowMessage("Loading parcels...");
        try
        {
            var result = await parcels.ListAsync();
            if (token != _loadToken || view.IsDisposed) return;
            if (!result.Ok || result.Value is null)
            {
                ClearAfterFailedLoad(token, "Could not load parcels. Refresh and try again.");
                return;
            }
            var list = result.Value;

            _byId.Clear();
            foreach (var parcel in list)
                _byId[parcel.Id] = parcel;

            var features = ParcelGeoJsonBuilder.Build(list);
            view.ShowParcels(features.FeatureCollection, features.FeatureCount, features.Unplotted);
            view.ShowMessage(features.FeatureCount == 0 ? "No parcels have a map position yet." : null);
        }
        catch (Exception ex)
        {
            if (token == _loadToken && !view.IsDisposed)
            {
                WayPlot.UI.AppLog.Error("ParcelMapPresenter.LoadAsync", ex);
                ClearAfterFailedLoad(token, "Could not load parcels. Refresh and try again.");
            }
        }
    }

    private void ClearAfterFailedLoad(int token, string message)
    {
        if (token != _loadToken || view.IsDisposed)
            return;

        _byId.Clear();
        _selectedParcelId = null;
        _selectionToken++;
        view.ClearParcel();
        view.ShowMessage(message);
    }

    /// <summary>Called when the agent clicks a parcel (or clicks empty map, id = null).</summary>
    public async Task SelectAsync(Guid? id)
    {
        var token = ++_selectionToken;
        _selectedParcelId = id;

        if (view.IsDisposed) return;
        if (id is null)
        {
            view.ClearParcel();
            return;
        }

        if (!_byId.TryGetValue(id.Value, out var parcel))
        {
            // The map can still display features from the previous load while a refresh is in flight.
            // Refresh once before treating that click as a missing parcel.
            await LoadAsync();
            if (token != _selectionToken || view.IsDisposed)
                return;
            if (!_byId.TryGetValue(id.Value, out parcel))
            {
                view.ClearParcel();
                view.ShowMessage("That parcel is no longer available. Reload the map and try again.");
                return;
            }
        }

        // The parcel's own facts are already loaded, so show them at once. The leads follow.
        view.ShowParcel(parcel, null);

        try
        {
            var result = await leads.ListByParcelAsync(parcel.Id);
            if (token != _selectionToken || view.IsDisposed) return;   // the agent clicked something else meanwhile
            if (!result.Ok || result.Value is null)
            {
                view.ShowLeadsError(result.Message);
                return;
            }
            view.ShowParcel(parcel, result.Value);
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

    private async Task ReloadAndSelectSavedAsync(Guid parcelId)
    {
        await LoadAsync();
        if (view.IsDisposed || !_byId.ContainsKey(parcelId))
            return;

        await SelectAsync(parcelId);
    }

    private static EditableGeometryResult GetEditableGeometry(ParcelDto parcel)
    {
        if (!string.IsNullOrWhiteSpace(parcel.GeoJson))
        {
            try
            {
                using var document = JsonDocument.Parse(parcel.GeoJson);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("type", out var type)
                    || type.ValueKind != JsonValueKind.String
                    || type.GetString() != "Polygon"
                    || !root.TryGetProperty("coordinates", out var coordinates)
                    || coordinates.ValueKind != JsonValueKind.Array
                    || coordinates.GetArrayLength() != 1
                    || coordinates[0].ValueKind != JsonValueKind.Array)
                    return EditableGeometryResult.Fail("This boundary cannot be edited here because it must be a Polygon with one ring.");

                var ring = coordinates[0];
                var points = new List<ParcelCoordinate>(ring.GetArrayLength());
                foreach (var position in ring.EnumerateArray())
                {
                    if (position.ValueKind != JsonValueKind.Array
                        || position.GetArrayLength() != 2
                        || position[0].ValueKind != JsonValueKind.Number
                        || position[1].ValueKind != JsonValueKind.Number
                        || !position[0].TryGetDouble(out var longitude)
                        || !position[1].TryGetDouble(out var latitude))
                        return EditableGeometryResult.Fail("This boundary contains an invalid coordinate.");
                    points.Add(new ParcelCoordinate(longitude, latitude));
                }

                if (points.Count > 1 && points[0] == points[^1])
                    points.RemoveAt(points.Count - 1);
                var validation = ParcelGeometry.Validate("polygon", points);
                return validation.Ok
                    ? EditableGeometryResult.Success()
                    : EditableGeometryResult.Fail(validation.Message);
            }
            catch (JsonException)
            {
                return EditableGeometryResult.Fail("This boundary is not valid GeoJSON.");
            }
        }

        if (parcel.Latitude is not { } markerLatitude || parcel.Longitude is not { } markerLongitude)
            return EditableGeometryResult.Fail("This parcel has no boundary or marker to edit.");

        var markerValidation = ParcelGeometry.Validate(
            "marker",
            [new ParcelCoordinate(markerLongitude, markerLatitude)]);
        return markerValidation.Ok
            ? EditableGeometryResult.Success()
            : EditableGeometryResult.Fail(markerValidation.Message);
    }

    private readonly record struct EditableGeometryResult(bool Ok, string Message)
    {
        public static EditableGeometryResult Success() => new(true, string.Empty);
        public static EditableGeometryResult Fail(string message) => new(false, message);
    }
}
