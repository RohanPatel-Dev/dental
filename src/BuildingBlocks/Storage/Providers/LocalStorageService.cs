using System.Globalization;
using Microsoft.Extensions.Options;

namespace Dental.Framework.Storage.Providers;

/// <summary>
/// Local disk provider for development. Presigned URLs are not real signatures - they point at the
/// API's own passthrough endpoints - so never run this in production.
/// </summary>
/// <param name="options">Storage configuration.</param>
public sealed class LocalStorageService(IOptions<StorageOptions> options) : IStorageService
{
    private readonly StorageOptions _options = options.Value;

    /// <inheritdoc />
    public async Task<StoredObject> UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        string path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        FileStream file = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await using (file.ConfigureAwait(false))
        {
            await content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
        }

        FileInfo info = new(path);
        return new StoredObject(key, info.Length, contentType, info.LastWriteTimeUtc);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        string path = ResolvePath(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        string path = ResolvePath(key);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Object '{key}' was not found.", path);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(ResolvePath(key)));

    /// <inheritdoc />
    public Task<long?> GetSizeAsync(string key, CancellationToken cancellationToken = default)
    {
        FileInfo info = new(ResolvePath(key));
        return Task.FromResult(info.Exists ? info.Length : (long?)null);
    }

    /// <inheritdoc />
    public Task<StoredObject?> HeadObjectAsync(string key, CancellationToken cancellationToken = default)
    {
        FileInfo info = new(ResolvePath(key));
        StoredObject? result = info.Exists
            ? new StoredObject(key, info.Length, null, info.LastWriteTimeUtc)
            : null;

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Uri> GenerateUploadUrlAsync(
        string key,
        string contentType,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(BuildPassthroughUrl("upload", key, lifetime));

    /// <inheritdoc />
    public Task<Uri> GenerateDownloadUrlAsync(
        string key,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(BuildPassthroughUrl("download", key, lifetime));

    /// <inheritdoc />
    public Uri? BuildPublicUrl(string key) =>
        string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? null
            : new Uri($"{_options.PublicBaseUrl.TrimEnd('/')}/{key}");

    private Uri BuildPassthroughUrl(string operation, string key, TimeSpan lifetime)
    {
        string expires = DateTimeOffset.UtcNow
            .Add(lifetime)
            .ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);

        string root = _options.PublicBaseUrl?.TrimEnd('/') ?? "http://localhost:5030/api/v1/files";
        return new Uri($"{root}/{operation}?key={Uri.EscapeDataString(key)}&expires={expires}");
    }

    private string ResolvePath(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        string root = Path.GetFullPath(Path.Combine(_options.LocalRoot, _options.Bucket));
        string path = Path.GetFullPath(Path.Combine(root, key));

        // Refuse anything that escapes the bucket root - a key is attacker influenced input.
        if (!path.StartsWith(root, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException($"Object key '{key}' resolves outside the bucket root.");
        }

        return path;
    }
}
