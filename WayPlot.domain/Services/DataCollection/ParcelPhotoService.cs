using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Repositories;

namespace WayPlot.domain.Services.DataCollection;

public sealed class ParcelPhotoService(ITenantUnitOfWorkFactory uowFactory, TenantAccessPolicy access)
{
    private const string JpegContentType = "image/jpeg";

    public async Task<ServiceResult<IReadOnlyList<ParcelPhotoThumbnailDto>>> ListPhotosAsync(
        Guid parcelId,
        CancellationToken cancellationToken = default)
    {
        await using var uow = uowFactory.Create();
        var parcel = await uow.Parcels.GetAsync(parcelId);
        if (parcel is null)
            return ServiceResult<IReadOnlyList<ParcelPhotoThumbnailDto>>.Fail(
                "Cannot view parcel photos: the parcel was not found or has been archived.");

        var permission = access.CanReadParcelPhotos(parcel);
        if (!permission.Ok)
            return ServiceResult<IReadOnlyList<ParcelPhotoThumbnailDto>>.Fail(permission.Message);

        return ServiceResult<IReadOnlyList<ParcelPhotoThumbnailDto>>.Success(
            await uow.ParcelPhotos.ListByParcelAsync(parcelId, cancellationToken));
    }

    public async Task<ServiceResult<ParcelPhotoThumbnailDto>> AddPhotoAsync(
        Guid parcelId,
        string fileName,
        byte[] processedBytes,
        byte[] thumbnailBytes,
        CancellationToken cancellationToken = default)
    {
        await using var uow = uowFactory.Create();
        var parcel = await uow.Parcels.GetAsync(parcelId);
        if (parcel is null)
            return ServiceResult<ParcelPhotoThumbnailDto>.Fail(
                "Cannot add photo: the parcel was not found or has been archived.");

        var permission = access.CanEditParcel(parcel);
        if (!permission.Ok)
            return ServiceResult<ParcelPhotoThumbnailDto>.Fail($"Cannot add photo: {permission.Message}");

        var storedFileName = Path.GetFileName(fileName?.Trim() ?? string.Empty);
        if (!ParcelPhotoLimits.SupportsFileName(storedFileName))
            return ServiceResult<ParcelPhotoThumbnailDto>.Fail(
                "Cannot add photo: choose a JPG, JPEG, or PNG file.");
        if (processedBytes is null || processedBytes.Length == 0)
            return ServiceResult<ParcelPhotoThumbnailDto>.Fail("Cannot add photo: the processed image is empty.");
        if (processedBytes.Length > ParcelPhotoLimits.MaxProcessedBytes)
            return ServiceResult<ParcelPhotoThumbnailDto>.Fail(
                $"Cannot add photo: the processed image exceeds {ParcelPhotoLimits.MaxProcessedBytes / (1024 * 1024)} MB.");
        if (!HasJpegSignature(processedBytes))
            return ServiceResult<ParcelPhotoThumbnailDto>.Fail(
                "Cannot add photo: the processed image is not a JPEG.");
        if (thumbnailBytes is null || thumbnailBytes.Length == 0 || !HasJpegSignature(thumbnailBytes))
            return ServiceResult<ParcelPhotoThumbnailDto>.Fail(
                "Cannot add photo: the generated thumbnail is not a JPEG.");
        if (thumbnailBytes.Length > ParcelPhotoLimits.MaxProcessedBytes)
            return ServiceResult<ParcelPhotoThumbnailDto>.Fail(
                "Cannot add photo: the generated thumbnail exceeds the size limit.");

        var count = await uow.ParcelPhotos.CountByParcelAsync(parcelId, cancellationToken);
        if (count >= ParcelPhotoLimits.MaxPhotosPerParcel)
            return ServiceResult<ParcelPhotoThumbnailDto>.Fail(
                $"Cannot add photo: a parcel can have at most {ParcelPhotoLimits.MaxPhotosPerParcel} photos.");

        var photo = new ParcelPhoto
        {
            ParcelId = parcel.Id,
            Parcel = parcel,
            FileName = storedFileName
        };
        photo.StoragePath = $"db:{photo.Id}";
        uow.ParcelPhotos.Add(photo);
        uow.ParcelPhotos.AddData(new ParcelPhotoData
        {
            ParcelPhotoId = photo.Id,
            ContentType = JpegContentType,
            SizeBytes = processedBytes.Length,
            ImageBytes = processedBytes,
            ThumbnailBytes = thumbnailBytes
        });
        uow.Parcels.Touch(parcel);

        try
        {
            await uow.SaveAsync();
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ServiceResult<ParcelPhotoThumbnailDto>.Fail(
                "Cannot add photo: the parcel changed while photos were being saved. Reload and try again.");
        }

        return ServiceResult<ParcelPhotoThumbnailDto>.Success(
            new ParcelPhotoThumbnailDto(photo.Id, photo.FileName, processedBytes.Length, thumbnailBytes),
            "Photo added.");
    }

    public async Task<ServiceResult<bool>> RemovePhotoAsync(
        Guid parcelId,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        await using var uow = uowFactory.Create();
        var parcel = await uow.Parcels.GetAsync(parcelId);
        if (parcel is null)
            return ServiceResult<bool>.Fail(
                "Cannot remove photo: the parcel was not found or has been archived.");

        var permission = access.CanEditParcel(parcel);
        if (!permission.Ok)
            return ServiceResult<bool>.Fail($"Cannot remove photo: {permission.Message}");

        var photo = await uow.ParcelPhotos.GetAsync(photoId, cancellationToken);
        if (photo is null || photo.ParcelId != parcel.Id)
            return ServiceResult<bool>.Fail(
                "Cannot remove photo: it was not found on the selected parcel.");

        uow.ParcelPhotos.Remove(photo);
        uow.Parcels.Touch(parcel);
        try
        {
            await uow.ExecuteInTransactionAsync(async () =>
            {
                await uow.SaveAsync();
                var deletedRows = await uow.ParcelPhotos.DeleteDataAsync(photo.Id, cancellationToken);
                if (deletedRows != 1)
                    throw new InvalidOperationException(
                        "Photo removal was rolled back because its stored image data could not be found.");
            });
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ServiceResult<bool>.Fail(
                "Cannot remove photo: the parcel changed while photos were being saved. Reload and try again.");
        }

        return ServiceResult<bool>.Success(true, "Photo removed.");
    }

    public async Task<ServiceResult<ParcelPhotoImageDto>> GetPhotoImageAsync(
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        await using var uow = uowFactory.Create();
        var photo = await uow.ParcelPhotos.GetAsync(photoId, cancellationToken);
        if (photo is null)
            return ServiceResult<ParcelPhotoImageDto>.Fail("Cannot view parcel photo: it was not found.");
        var parcel = await uow.Parcels.GetAsync(photo.ParcelId);
        if (parcel is null)
            return ServiceResult<ParcelPhotoImageDto>.Fail(
                "Cannot view parcel photo: its parcel was not found or has been archived.");

        var permission = access.CanReadParcelPhotos(parcel);
        if (!permission.Ok)
            return ServiceResult<ParcelPhotoImageDto>.Fail(permission.Message);

        var image = await uow.ParcelPhotos.GetImageAsync(photoId, cancellationToken);
        return image is null
            ? ServiceResult<ParcelPhotoImageDto>.Fail(
                "Cannot view parcel photo: its stored image data could not be found.")
            : ServiceResult<ParcelPhotoImageDto>.Success(image);
    }

    private static bool HasJpegSignature(byte[] bytes) =>
        bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
}
