using TasteBudz.Backend.Infrastructure.ProblemDetails;

namespace TasteBudz.Backend.Modules.Media;

/// <summary>
/// Shared image-upload validation used before media bytes are stored.
/// </summary>
internal static class ImageUploadValidator
{
    private const int MaxImageBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/gif",
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public static async Task<ValidatedImageFile> ReadValidatedImageAsync(UploadImageRequest request, CancellationToken cancellationToken)
    {
        var file = request.File ?? throw ApiException.BadRequest("file is required.");

        if (file.Length <= 0)
        {
            throw ApiException.BadRequest("file must not be empty.");
        }

        if (file.Length > MaxImageBytes)
        {
            throw ApiException.BadRequest($"file must be {MaxImageBytes} bytes or smaller.");
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? string.Empty : file.ContentType.Trim();

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw ApiException.BadRequest("Only PNG, JPEG, GIF, and WebP images are supported.");
        }

        var fileName = NormalizeFileName(file.FileName);

        await using var source = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length <= 0)
        {
            throw ApiException.BadRequest("file must not be empty.");
        }

        if (buffer.Length > MaxImageBytes)
        {
            throw ApiException.BadRequest($"file must be {MaxImageBytes} bytes or smaller.");
        }

        return new ValidatedImageFile(buffer.ToArray(), contentType, fileName);
    }

    private static string NormalizeFileName(string? value)
    {
        var fileName = Path.GetFileName(value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(fileName)
            ? "upload"
            : fileName[..Math.Min(fileName.Length, 255)];
    }
}
