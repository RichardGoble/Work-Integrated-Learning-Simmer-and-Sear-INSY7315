namespace SimmerAndSear.Application.Abstractions;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 20 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed record ImageValidationResult(
    bool IsValid,
    string? ErrorMessage,
    string ContentType,
    int Width,
    int Height)
{
    public static ImageValidationResult Success(string contentType, int width, int height) =>
        new(true, null, contentType, width, height);

    public static ImageValidationResult Failure(string errorMessage) =>
        new(false, errorMessage, string.Empty, 0, 0);
}

public interface IImageProcessor
{
    Task<ImageValidationResult> ValidateImageAsync(Stream stream, CancellationToken ct = default);
    Task<Stream> CreateThumbnailAsync(Stream inputStream, int maxDimension = 400, CancellationToken ct = default);
}
