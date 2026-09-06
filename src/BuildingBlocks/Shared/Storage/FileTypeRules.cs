using System.Collections.Frozen;

namespace Dental.Framework.Shared.Storage;

/// <summary>Extension and size rules per <see cref="FileType"/>, enforced before a presigned URL is issued.</summary>
public static class FileTypeRules
{
    private static readonly FrozenDictionary<FileType, FileTypeRule> Rules =
        new Dictionary<FileType, FileTypeRule>
        {
            [FileType.Image] = new([".jpg", ".jpeg", ".png", ".webp"], 8L * 1024 * 1024),
            [FileType.Document] = new([".pdf", ".doc", ".docx", ".txt"], 25L * 1024 * 1024),
            [FileType.Radiograph] = new([".jpg", ".jpeg", ".png", ".tif", ".tiff", ".dcm"], 128L * 1024 * 1024),
            [FileType.Report] = new([".pdf", ".csv"], 50L * 1024 * 1024),
        }.ToFrozenDictionary();

    /// <summary>Looks up the rule for a category.</summary>
    /// <param name="fileType">The category.</param>
    /// <returns>The rule.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The category is not known.</exception>
    public static FileTypeRule For(FileType fileType) =>
        Rules.TryGetValue(fileType, out FileTypeRule? rule)
            ? rule
            : throw new ArgumentOutOfRangeException(nameof(fileType), fileType, "Unknown file type.");

    /// <summary>Validates an extension and a declared size against a category.</summary>
    /// <param name="fileType">The category.</param>
    /// <param name="extension">File extension including the leading dot.</param>
    /// <param name="sizeInBytes">Declared size in bytes.</param>
    /// <param name="error">Why validation failed, when it did.</param>
    /// <returns><see langword="true"/> when the file is acceptable.</returns>
    public static bool Validate(FileType fileType, string extension, long sizeInBytes, out string? error)
    {
        FileTypeRule rule = For(fileType);

        if (string.IsNullOrWhiteSpace(extension) ||
            !rule.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            error = $"Extension '{extension}' is not allowed for {fileType}. " +
                    $"Allowed: {string.Join(", ", rule.Extensions)}.";
            return false;
        }

        if (sizeInBytes <= 0 || sizeInBytes > rule.MaxSizeInBytes)
        {
            error = $"Size {sizeInBytes} bytes is outside the allowed range for {fileType} " +
                    $"(max {rule.MaxSizeInBytes} bytes).";
            return false;
        }

        error = null;
        return true;
    }
}

/// <summary>Allowed extensions and maximum size for one <see cref="FileType"/>.</summary>
/// <param name="Extensions">Allowed extensions, including the leading dot.</param>
/// <param name="MaxSizeInBytes">Largest accepted size.</param>
public sealed record FileTypeRule(IReadOnlyList<string> Extensions, long MaxSizeInBytes);
