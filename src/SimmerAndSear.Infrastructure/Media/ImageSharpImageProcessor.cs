using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SimmerAndSear.Application.Abstractions;

namespace SimmerAndSear.Infrastructure.Media;

// ── References ──────────────────────────────────────────────────────────────
//SixLabors, 2024. ImageSharp Documentation [Source code].
//Available at: <https://docs.sixlabors.com/articles/imagesharp/> [Accessed 20 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 20 September 2026].
//
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class ImageSharpImageProcessor : IImageProcessor
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public async Task<ImageValidationResult> ValidateImageAsync(Stream stream, CancellationToken ct = default)
    {
        try
        {
            var imageInfo = await Image.IdentifyAsync(stream, ct);
            if (imageInfo is null || imageInfo.Metadata.DecodedImageFormat is null)
            {
                return ImageValidationResult.Failure("The file content is not a valid or recognised image.");
            }

            var mimeType = imageInfo.Metadata.DecodedImageFormat.DefaultMimeType;
            if (!AllowedMimeTypes.Contains(mimeType))
            {
                return ImageValidationResult.Failure($"Unsupported image format '{mimeType}'. Supported formats are JPEG, PNG, and WebP.");
            }

            if (imageInfo.Width <= 0 || imageInfo.Height <= 0)
            {
                return ImageValidationResult.Failure("Image has invalid dimensions.");
            }

            return ImageValidationResult.Success(mimeType, imageInfo.Width, imageInfo.Height);
        }
        catch (Exception)
        {
            return ImageValidationResult.Failure("The uploaded file is corrupt or not a valid image.");
        }
    }

    public async Task<Stream> CreateThumbnailAsync(Stream inputStream, int maxDimension = 400, CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(inputStream, ct);
        var format = image.Metadata.DecodedImageFormat ?? JpegFormat.Instance;

        if (image.Width > maxDimension || image.Height > maxDimension)
        {
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxDimension, maxDimension)
            }));
        }

        var output = new MemoryStream();
        await image.SaveAsync(output, format, ct);
        output.Position = 0;
        return output;
    }
}
