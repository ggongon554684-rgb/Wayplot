using System.Text.Json;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Repositories;
using ParcelResult = WayPlot.domain.Common.ServiceResult<WayPlot.domain.Contracts.ParcelDto>;
using OwnerResult = WayPlot.domain.Common.ServiceResult<WayPlot.domain.Contracts.OwnerDto>;

namespace WayPlot.domain.Services.DataCollection;

/// <summary>Parcels and their owners. A parcel always has an owner: a landowner, or the company itself as an owner row.</summary>
public sealed class ParcelService(ITenantUnitOfWorkFactory uowFactory, TenantAccessPolicy access)
{
    public async Task<OwnerResult> CreateOwnerAsync(CreateOwnerRequest request)
    {
        var permission = access.RequireDataMutation("create parcel owners");
        if (!permission.Ok)
            return OwnerResult.Fail(permission.Message);

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return OwnerResult.Fail("Enter the owner's name.");

        var phoneText = request.Phone?.Trim() ?? string.Empty;
        var phone = PhoneNormalizer.Normalize(phoneText) ?? phoneText;
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        await using var uow = uowFactory.Create();
        var owner = new Owner
        {
            Name = name,
            Phone = phone,
            Email = email,
            ListingAgreementOnFile = request.ListingAgreementOnFile
        };
        uow.Owners.Add(owner);
        await uow.SaveAsync();

        return OwnerResult.Success(ToDto(owner), "Owner saved.");
    }

    public async Task<ServiceResult<IReadOnlyList<OwnerDto>>> ListOwnersAsync()
    {
        var permission = access.RequireParcelRead();
        if (!permission.Ok)
            return ServiceResult<IReadOnlyList<OwnerDto>>.Fail(permission.Message);

        await using var uow = uowFactory.Create();
        IReadOnlyList<OwnerDto> owners = (await uow.Owners.ListAsync()).Select(ToDto).ToList();
        return ServiceResult<IReadOnlyList<OwnerDto>>.Success(owners);
    }

    public async Task<ServiceResult<IReadOnlyList<ParcelAgentOption>>> ListActiveAgentsAsync()
    {
        var permission = access.CanReassignParcel();
        if (!permission.Ok)
            return ServiceResult<IReadOnlyList<ParcelAgentOption>>.Fail(permission.Message);

        await using var uow = uowFactory.Create();
        IReadOnlyList<ParcelAgentOption> agents = (await uow.Staff.ListActiveAgentsAsync())
            .Select(staff => new ParcelAgentOption(staff.Id, staff.FullName))
            .ToList();
        return ServiceResult<IReadOnlyList<ParcelAgentOption>>.Success(agents);
    }

    public async Task<ParcelResult> CreateAsync(CreateParcelRequest request)
    {
        var permission = access.CanPlotParcel();
        if (!permission.Ok)
            return ParcelResult.Fail(permission.Message);

        var label = NormalizeLabel(request.Label);
        var fieldValidation = ValidateFields(label, request.AreaSqm, request.Zoning, request.Price, request.ReservationFee);
        if (!fieldValidation.Ok)
            return ParcelResult.Fail(fieldValidation.Message);

        var geometry = NormalizeGeometry(request.GeoJson, request.Latitude, request.Longitude);
        if (!geometry.Ok)
            return ParcelResult.Fail(geometry.Message);

        await using var uow = uowFactory.Create();
        var owner = await uow.Owners.GetAsync(request.OwnerId);
        if (owner is null)
            return ParcelResult.Fail("Choose an active owner for this parcel.");

        if (await uow.Parcels.LabelExistsAsync(label))
            return ParcelResult.Fail(DuplicateLabelMessage(label));

        Guid? assignedStaffId;
        if (access.IsAgent)
        {
            assignedStaffId = access.CurrentStaffId;
        }
        else
        {
            assignedStaffId = request.AssignedStaffId;
            if (assignedStaffId is { } selectedAgent
                && !await uow.Staff.IsActiveAgentAsync(selectedAgent))
                return ParcelResult.Fail("The assigned agent was not found or is inactive.");
        }

        var parcel = new Parcel
        {
            Label = label,
            Description = request.Description?.Trim() ?? string.Empty,
            AreaSqm = request.AreaSqm,
            Zoning = request.Zoning.Trim(),
            Price = request.Price,
            ReservationFee = request.ReservationFee,
            Status = ParcelStatus.Available,
            GeoJson = geometry.GeoJson,
            Latitude = geometry.Latitude,
            Longitude = geometry.Longitude,
            TaxDeclarationNo = request.TaxDeclarationNo?.Trim() ?? string.Empty,
            TitleNo = request.TitleNo?.Trim() ?? string.Empty,
            Utilities = request.Utilities?.Trim() ?? string.Empty,
            RoadAccess = request.RoadAccess?.Trim() ?? string.Empty,
            OwnerId = owner.Id,
            Owner = owner,
            AssignedStaffId = assignedStaffId,
            BranchId = access.CurrentBranchId
        };

        try
        {
            uow.Parcels.Add(parcel);
            await uow.SaveAsync();
        }
        catch (ParcelLabelConflictException)
        {
            return ParcelResult.Fail(DuplicateLabelMessage(label));
        }

        return ParcelResult.Success(ToDto(parcel, owner), "Parcel created.");
    }

    public async Task<ParcelResult> UpdateAsync(UpdateParcelRequest request)
    {
        await using var uow = uowFactory.Create();
        var parcel = await uow.Parcels.GetAsync(request.ParcelId);
        if (parcel is null)
            return ParcelResult.Fail("That parcel was not found or has been archived.");

        var permission = access.CanEditParcel(parcel);
        if (!permission.Ok)
            return ParcelResult.Fail(permission.Message);

        var label = NormalizeLabel(request.Label);
        var fieldValidation = ValidateFields(label, request.AreaSqm, request.Zoning, parcel.Price, parcel.ReservationFee);
        if (!fieldValidation.Ok)
            return ParcelResult.Fail(fieldValidation.Message);

        GeometryValues? geometry = null;
        if (!string.IsNullOrWhiteSpace(request.GeoJson) || request.Latitude.HasValue || request.Longitude.HasValue)
        {
            var validation = NormalizeGeometry(request.GeoJson, request.Latitude, request.Longitude);
            if (!validation.Ok)
                return ParcelResult.Fail(validation.Message);
            geometry = validation;
        }

        var owner = await uow.Owners.GetAsync(request.OwnerId);
        if (owner is null)
            return ParcelResult.Fail("Choose an active owner for this parcel.");

        if (await uow.Parcels.LabelExistsAsync(label, parcel.Id))
            return ParcelResult.Fail(DuplicateLabelMessage(label));

        parcel.Label = label;
        parcel.Description = request.Description?.Trim() ?? string.Empty;
        parcel.AreaSqm = request.AreaSqm;
        parcel.Zoning = request.Zoning.Trim();
        if (geometry is { } updatedGeometry)
        {
            parcel.GeoJson = updatedGeometry.GeoJson;
            parcel.Latitude = updatedGeometry.Latitude;
            parcel.Longitude = updatedGeometry.Longitude;
        }
        parcel.TaxDeclarationNo = request.TaxDeclarationNo?.Trim() ?? string.Empty;
        parcel.TitleNo = request.TitleNo?.Trim() ?? string.Empty;
        parcel.Utilities = request.Utilities?.Trim() ?? string.Empty;
        parcel.RoadAccess = request.RoadAccess?.Trim() ?? string.Empty;
        parcel.OwnerId = owner.Id;
        parcel.Owner = owner;

        try
        {
            await uow.SaveAsync();
        }
        catch (ParcelLabelConflictException)
        {
            return ParcelResult.Fail(DuplicateLabelMessage(label));
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ParcelResult.Fail("This parcel changed while you were editing it. Reload it and try again.");
        }

        return ParcelResult.Success(ToDto(parcel, owner), "Parcel details saved.");
    }

    public async Task<ParcelResult> ChangePriceAsync(ChangeParcelPriceRequest request)
    {
        var permission = access.CanChangeParcelPrice();
        if (!permission.Ok)
            return ParcelResult.Fail(permission.Message);
        if (request.Price < 0)
            return ParcelResult.Fail("The price cannot be negative.");
        if (request.ReservationFee < 0)
            return ParcelResult.Fail("The reservation fee cannot be negative.");
        if (request.ReservationFee > request.Price)
            return ParcelResult.Fail("The reservation fee cannot be higher than the price.");

        await using var uow = uowFactory.Create();
        var parcel = await uow.Parcels.GetAsync(request.ParcelId);
        if (parcel is null)
            return ParcelResult.Fail("That parcel was not found or has been archived.");

        parcel.Price = request.Price;
        parcel.ReservationFee = request.ReservationFee;
        try
        {
            await uow.SaveAsync();
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ParcelResult.Fail("This parcel changed while you were editing it. Reload it and try again.");
        }

        return ParcelResult.Success(ToDto(parcel, parcel.Owner), "Parcel price and reservation fee saved.");
    }

    public async Task<ParcelResult> ReassignAsync(ReassignParcelRequest request)
    {
        var permission = access.CanReassignParcel();
        if (!permission.Ok)
            return ParcelResult.Fail(permission.Message);

        await using var uow = uowFactory.Create();
        var parcel = await uow.Parcels.GetAsync(request.ParcelId);
        if (parcel is null)
            return ParcelResult.Fail("That parcel was not found or has been archived.");
        if (!await uow.Staff.IsActiveAgentAsync(request.AssignedStaffId))
            return ParcelResult.Fail("Choose an active Agent to reassign this parcel.");

        parcel.AssignedStaffId = request.AssignedStaffId;
        try
        {
            await uow.SaveAsync();
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ParcelResult.Fail("This parcel changed while you were editing it. Reload it and try again.");
        }

        return ParcelResult.Success(ToDto(parcel, parcel.Owner), "Parcel reassigned. Existing leads were not changed.");
    }

    public async Task<ServiceResult<bool>> ArchiveAsync(Guid parcelId)
    {
        var permission = access.CanArchiveParcel();
        if (!permission.Ok)
            return ServiceResult<bool>.Fail(permission.Message);

        await using var uow = uowFactory.Create();
        var parcel = await uow.Parcels.GetAsync(parcelId);
        if (parcel is null)
            return ServiceResult<bool>.Fail("That parcel was not found or has already been archived.");

        var blockingDeal = (await uow.Deals.ListStatusesByParcelAsync(parcelId))
            .Where(status => status is not (DealStatus.Cancelled or DealStatus.Defaulted))
            .Select(status => (DealStatus?)status)
            .FirstOrDefault();
        if (blockingDeal is { } dealStatus)
            return ServiceResult<bool>.Fail(
                $"Cannot archive parcel \"{parcel.Label}\": it has a deal with status {dealStatus}. Only Cancelled or Defaulted deals permit archiving.");

        var blockingLead = (await uow.Leads.ListStagesByParcelAsync(parcelId))
            .Where(stage => stage != LeadStage.Lost)
            .Select(stage => (LeadStage?)stage)
            .FirstOrDefault();
        if (blockingLead is { } leadStage)
            return ServiceResult<bool>.Fail(
                $"Cannot archive parcel \"{parcel.Label}\": it has a lead at stage {leadStage}. Every lead must be Lost first.");

        parcel.IsDeleted = true;
        try
        {
            await uow.SaveAsync();
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ServiceResult<bool>.Fail("This parcel changed while you were archiving it. Reload it and try again.");
        }

        return ServiceResult<bool>.Success(true, $"Parcel \"{parcel.Label}\" archived. Its label remains reserved.");
    }

    public async Task<ServiceResult<IReadOnlyList<ParcelDto>>> ListAsync()
    {
        var permission = access.RequireParcelRead();
        if (!permission.Ok)
            return ServiceResult<IReadOnlyList<ParcelDto>>.Fail(permission.Message);

        await using var uow = uowFactory.Create();
        return ServiceResult<IReadOnlyList<ParcelDto>>.Success(
            (await uow.Parcels.ListAsync()).Select(parcel => ToDto(parcel, parcel.Owner)).ToList());
    }

    public async Task<ParcelResult> GetAsync(Guid id)
    {
        var permission = access.RequireParcelRead();
        if (!permission.Ok)
            return ParcelResult.Fail(permission.Message);

        await using var uow = uowFactory.Create();
        var parcel = await uow.Parcels.GetAsync(id);
        return parcel is null
            ? ParcelResult.Fail("That parcel was not found or has been archived.")
            : ParcelResult.Success(ToDto(parcel, parcel.Owner));
    }

    private static RuleResult ValidateFields(
        string label,
        decimal areaSqm,
        string? zoning,
        decimal price,
        decimal reservationFee)
    {
        if (label.Length == 0)
            return RuleResult.Fail("Enter a parcel label.");
        if (label.Length > 60)
            return RuleResult.Fail("The label is too long (60 characters at most).");
        if (areaSqm <= 0)
            return RuleResult.Fail("The area must be more than zero square meters.");
        if (string.IsNullOrWhiteSpace(zoning))
            return RuleResult.Fail("Enter the zoning (for example R-1).");
        if (price < 0)
            return RuleResult.Fail("The price cannot be negative.");
        if (reservationFee < 0)
            return RuleResult.Fail("The reservation fee cannot be negative.");
        if (reservationFee > price)
            return RuleResult.Fail("The reservation fee cannot be higher than the price.");
        return RuleResult.Success();
    }

    private static string NormalizeLabel(string? label) => label?.Trim() ?? string.Empty;

    private static string DuplicateLabelMessage(string label) =>
        $"A parcel labeled \"{label}\" already exists. Labels remain reserved after a parcel is archived.";

    private static GeometryValues NormalizeGeometry(string? geoJson, double? latitude, double? longitude)
    {
        if (!string.IsNullOrWhiteSpace(geoJson))
        {
            if (latitude.HasValue || longitude.HasValue)
                return GeometryValues.Fail("Use either a polygon boundary or a marker, not both.");

            try
            {
                using var document = JsonDocument.Parse(geoJson);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("type", out var type)
                    || type.ValueKind != JsonValueKind.String
                    || type.GetString() != "Polygon"
                    || !root.TryGetProperty("coordinates", out var coordinates)
                    || coordinates.ValueKind != JsonValueKind.Array
                    || coordinates.GetArrayLength() != 1
                    || coordinates[0].ValueKind != JsonValueKind.Array)
                    return GeometryValues.Fail("The boundary must be a GeoJSON Polygon with one ring.");

                var ring = coordinates[0];
                var points = new List<ParcelCoordinate>(ring.GetArrayLength());
                foreach (var position in ring.EnumerateArray())
                {
                    if (position.ValueKind != JsonValueKind.Array || position.GetArrayLength() != 2
                        || position[0].ValueKind != JsonValueKind.Number
                        || position[1].ValueKind != JsonValueKind.Number
                        || !position[0].TryGetDouble(out var pointLongitude)
                        || !position[1].TryGetDouble(out var pointLatitude))
                        return GeometryValues.Fail("The boundary contains an invalid coordinate.");
                    points.Add(new ParcelCoordinate(pointLongitude, pointLatitude));
                }

                if (points.Count > 1 && points[0] == points[^1])
                    points.RemoveAt(points.Count - 1);
                var validation = ParcelGeometry.Validate("polygon", points);
                return validation.Ok
                    ? GeometryValues.Polygon(ParcelGeometry.BuildPolygonGeoJson(points))
                    : GeometryValues.Fail(validation.Message);
            }
            catch (JsonException)
            {
                return GeometryValues.Fail("The boundary is not valid GeoJSON.");
            }
        }

        if (latitude.HasValue != longitude.HasValue)
            return GeometryValues.Fail("Enter both latitude and longitude for a marker.");
        if (latitude is not { } markerLatitude || longitude is not { } markerLongitude)
            return GeometryValues.Fail("Draw a parcel boundary or place a marker.");

        var marker = new[] { new ParcelCoordinate(markerLongitude, markerLatitude) };
        var markerValidation = ParcelGeometry.Validate("marker", marker);
        return markerValidation.Ok
            ? GeometryValues.Marker(markerLatitude, markerLongitude)
            : GeometryValues.Fail(markerValidation.Message);
    }

    private static ParcelDto ToDto(Parcel parcel, Owner owner)
    {
        var pricePerSqm = parcel.AreaSqm > 0 ? Math.Round(parcel.Price / parcel.AreaSqm, 2) : 0m;
        var reservationPercent = parcel.Price > 0 ? Math.Round(parcel.ReservationFee / parcel.Price * 100m, 2) : 0m;

        return new ParcelDto(
            parcel.Id, parcel.Label, parcel.Description, parcel.AreaSqm, parcel.Zoning, parcel.Price, pricePerSqm,
            parcel.ReservationFee, reservationPercent, parcel.Status, parcel.GeoJson, parcel.Latitude, parcel.Longitude,
            parcel.TaxDeclarationNo, parcel.TitleNo, parcel.Utilities, parcel.RoadAccess,
            owner.Id, owner.Name, owner.Phone, owner.Email, owner.ListingAgreementOnFile, parcel.AssignedStaffId);
    }

    private static OwnerDto ToDto(Owner owner) =>
        new(owner.Id, owner.Name, owner.Phone, owner.Email, owner.ListingAgreementOnFile);

    private readonly record struct GeometryValues(
        bool Ok,
        string Message,
        string? GeoJson,
        double? Latitude,
        double? Longitude)
    {
        public static GeometryValues Polygon(string geoJson) => new(true, string.Empty, geoJson, null, null);
        public static GeometryValues Marker(double latitude, double longitude) =>
            new(true, string.Empty, null, latitude, longitude);
        public static GeometryValues Fail(string message) => new(false, message, null, null, null);
    }
}
