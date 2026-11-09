namespace RaceDay.Api.Services;

/// <summary>Validation shared by the banner and profile-picture upload endpoints.</summary>
public static class ImageUpload
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp"
    };

    /// <summary>Returns an error message, or null when the file is an acceptable image.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Choose an image file to upload.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";
        if (!Extensions.ContainsKey(file.ContentType)) return "Only JPG, PNG, GIF or WebP images are allowed.";
        return null;
    }

    public static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };

    public static string Extension(string contentType) =>
        Extensions.TryGetValue(contentType, out var ext) ? ext : ".img";
}
