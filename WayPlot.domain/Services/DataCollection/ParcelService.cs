using System.Text.Json;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Repositories;
using ParcelResult = WayPlot.domain.Common.ServiceResult<WayPlot.domain.Contracts.ParcelDto>;
using OwnerResult = WayPlot.domain.Common.ServiceResult<WayPlot.domain.Contracts.OwnerDto>;

namespace WayPlot.domain.Services.DataCollection;

/// <summary>Parcels and their owners. A parcel always has an owner: a landowner, or the company itself as an owner row.</summary>
public sealed class ParcelService(ITenantUnitOfWorkFactory uowFactory)
{
    public async Task<OwnerResult> CreateOwnerAsync(CreateOwnerRequest request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return OwnerResult.Fail("Enter the owner's name.");

        var phoneText = request.Phone?.Trim() ?? string.Empty;
        var phone = PhoneNormalizer.Normalize(phoneText) ?? phoneText;   // an office landline is kept as typed

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

    public async Task<IReadOnlyList<OwnerDto>> ListOwnersAsync()
    {
        await using var uow = uowFactory.Create();
        return (await uow.Owners.ListAsync()).Select(ToDto).ToList();
    }

    public async Task<ParcelResult> CreateAsync(CreateParcelRequest request)
    {
        var label = request.Label?.Trim() ?? string.Empty;
        if (label.Length == 0)
            return ParcelResult.Fail("Enter a parcel label.");
        if (label.Length > 60)
            return ParcelResult.Fail("The label is too long (60 characters at most).");
        if (request.AreaSqm <= 0)
            return ParcelResult.Fail("The area must be more than zero square meters.");
        if (string.IsNullOrWhiteSpace(request.Zoning))
            return ParcelResult.Fail("Enter the zoning (for example R-1).");
        if (request.Price <= 0)
            return ParcelResult.Fail("The price must be more than zero.");
        if (request.ReservationFee < 0)
            return ParcelResult.Fail("The reservation fee cannot be negative.");
        if (request.ReservationFee > request.Price)
            return ParcelResult.Fail("The reservation fee cannot be higher than the price.");

        if (!string.IsNullOrWhiteSpace(request.GeoJson) && !IsValidJson(request.GeoJson))
            return ParcelResult.Fail("The boundary is not valid GeoJSON text.");
        if (request.Latitude.HasValue != request.Longitude.HasValue)
            return ParcelResult.Fail("Enter both latitude and longitude, or neither.");
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            return ParcelResult.Fail("The latitude or longitude is out of range.");

        await using var uow = uowFactory.Create();

        var owner = await uow.Owners.GetAsync(request.OwnerId);
        if (owner is null)
            return ParcelResult.Fail("Choose the owner of this parcel (the landowner or the company itself).");

        if (await uow.Parcels.LabelExistsAsync(label))
            return ParcelResult.Fail($"A parcel labeled \"{label}\" already exists. Labels must be unique.");

        var parcel = new Parcel
        {
            Label = label,
            Description = request.Description?.Trim() ?? string.Empty,
            AreaSqm = request.AreaSqm,
            Zoning = request.Zoning.Trim(),
            Price = request.Price,
            ReservationFee = request.ReservationFee,
            Status = request.Status,
            GeoJson = string.IsNullOrWhiteSpace(request.GeoJson) ? null : request.GeoJson.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TaxDeclarationNo = request.TaxDeclarationNo?.Trim() ?? string.Empty,
            TitleNo = request.TitleNo?.Trim() ?? string.Empty,
            Utilities = request.Utilities?.Trim() ?? string.Empty,
            RoadAccess = request.RoadAccess?.Trim() ?? string.Empty,
            OwnerId = owner.Id
        };

        uow.Parcels.Add(parcel);
        await uow.SaveAsync();

        return ParcelResult.Success(ToDto(parcel, owner), "Parcel created.");
    }

    public async Task<IReadOnlyList<ParcelDto>> ListAsync()
    {
        await using var uow = uowFactory.Create();
        return (await uow.Parcels.ListAsync()).Select(p => ToDto(p, p.Owner)).ToList();
    }

    public async Task<ParcelResult> GetAsync(Guid id)
    {
        await using var uow = uowFactory.Create();
        var parcel = await uow.Parcels.GetAsync(id);
        return parcel is null
            ? ParcelResult.Fail("That parcel was not found.")
            : ParcelResult.Success(ToDto(parcel, parcel.Owner));
    }

    private static ParcelDto ToDto(Parcel p, Owner owner)
    {
        var pricePerSqm = p.AreaSqm > 0 ? Math.Round(p.Price / p.AreaSqm, 2) : 0m;
        var reservationPercent = p.Price > 0 ? Math.Round(p.ReservationFee / p.Price * 100m, 2) : 0m;

        return new ParcelDto(
            p.Id, p.Label, p.Description, p.AreaSqm, p.Zoning, p.Price, pricePerSqm,
            p.ReservationFee, reservationPercent, p.Status, p.GeoJson, p.Latitude, p.Longitude,
            p.TaxDeclarationNo, p.TitleNo, p.Utilities, p.RoadAccess, owner.Id, owner.Name);
    }

    private static OwnerDto ToDto(Owner o) => new(o.Id, o.Name, o.Phone, o.Email, o.ListingAgreementOnFile);

    private static bool IsValidJson(string text)
    {
        try
        {
            using var _ = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}