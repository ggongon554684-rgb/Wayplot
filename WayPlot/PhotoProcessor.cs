using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using WayPlot.domain.Contracts;

namespace WayPlot;

public sealed record ProcessedParcelPhoto(byte[] ImageBytes, byte[] ThumbnailBytes);

public sealed record PhotoProcessingResult(bool Ok, string Message, ProcessedParcelPhoto? Value);

public static class PhotoProcessor
{
    private const int ImageMaxSide = 1600;
    private const int ThumbnailMaxSide = 240;
    private const long JpegQuality = 80;
    private const int ExifOrientationId = 0x0112;

    public static PhotoProcessingResult Process(string filePath)
    {
        if (!ParcelPhotoLimits.SupportsFileName(filePath))
            return Refused("Choose a JPG, JPEG, or PNG file.");

        var file = new FileInfo(filePath);
        if (file.Length > ParcelPhotoLimits.MaxSourceFileBytes)
            return Refused($"The source file exceeds {ParcelPhotoLimits.MaxSourceFileBytes / (1024 * 1024)} MB.");
        if (file.Length == 0)
            return Refused("The selected file is empty.");

        try
        {
            using var source = Image.FromFile(filePath, useEmbeddedColorManagement: true);
            ApplyExifOrientation(source);

            using var processed = Resize(source, ImageMaxSide);
            // Re-encoding a fresh bitmap removes source metadata, including EXIF location data.
            var imageBytes = EncodeJpeg(processed);
            if (imageBytes.Length > ParcelPhotoLimits.MaxProcessedBytes)
                return Refused(
                    $"The processed image exceeds {ParcelPhotoLimits.MaxProcessedBytes / (1024 * 1024)} MB.");

            using var thumbnail = Resize(source, ThumbnailMaxSide);
            var thumbnailBytes = EncodeJpeg(thumbnail);
            return new PhotoProcessingResult(
                true,
                string.Empty,
                new ProcessedParcelPhoto(imageBytes, thumbnailBytes));
        }
        catch (ArgumentException)
        {
            return Refused("The selected file is not a valid image.");
        }
        catch (OutOfMemoryException)
        {
            return Refused("The selected file could not be decoded as an image.");
        }
        catch (ExternalException)
        {
            return Refused("The selected image could not be processed.");
        }
    }

    private static Bitmap Resize(Image source, int maxSide)
    {
        var scale = Math.Min(1d, (double)maxSide / Math.Max(source.Width, source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.White);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, 0, 0, width, height);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    private static byte[] EncodeJpeg(Image image)
    {
        var encoder = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(codec => codec.MimeType == "image/jpeg")
            ?? throw new InvalidOperationException("The JPEG encoder is unavailable.");
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, JpegQuality);
        using var stream = new MemoryStream();
        image.Save(stream, encoder, parameters);
        return stream.ToArray();
    }

    private static void ApplyExifOrientation(Image image)
    {
        if (!image.PropertyIdList.Contains(ExifOrientationId))
            return;

        var value = image.GetPropertyItem(ExifOrientationId)?.Value;
        if (value is not { Length: > 0 })
            return;

        var orientation = value[0];
        var rotation = orientation switch
        {
            2 => RotateFlipType.RotateNoneFlipX,
            3 => RotateFlipType.Rotate180FlipNone,
            4 => RotateFlipType.Rotate180FlipX,
            5 => RotateFlipType.Rotate90FlipX,
            6 => RotateFlipType.Rotate90FlipNone,
            7 => RotateFlipType.Rotate270FlipX,
            8 => RotateFlipType.Rotate270FlipNone,
            _ => RotateFlipType.RotateNoneFlipNone
        };
        image.RotateFlip(rotation);
    }

    private static PhotoProcessingResult Refused(string message) => new(false, message, null);
}
