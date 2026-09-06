using Dental.Framework.Shared.Storage;

namespace Dental.Framework.Storage;

/// <summary>
/// Object storage.
/// </summary>
/// <remarks>
/// Prefer the presigned flow for anything a user uploads: request a URL, let the browser PUT
/// directly to storage, then finalize. Streaming a large radiograph through the API burns a request
/// thread, a quota check and a timeout budget for no benefit.
/// </remarks>
public interface IStorageService
{
    /// <summary>Uploads a stream.</summary>
    /// <param name="key">Object key.</param>
    /// <param name="content">Content to write.</param>
    /// <param name="contentType">Content type to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Metadata for the stored object.</returns>
    Task<StoredObject> UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes an object.</summary>
    /// <param name="key">Object key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the object is gone.</returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Opens an object for reading.</summary>
    /// <param name="key">Object key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The content stream. The caller disposes it.</returns>
    Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Tests whether an object exists.</summary>
    /// <param name="key">Object key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the object exists.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Reads an object's size without downloading it.</summary>
    /// <param name="key">Object key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The size in bytes, or null when the object does not exist.</returns>
    Task<long?> GetSizeAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Reads an object's metadata without downloading it.</summary>
    /// <param name="key">Object key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The metadata, or null when the object does not exist.</returns>
    Task<StoredObject?> HeadObjectAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Issues a presigned URL the browser can PUT to directly.</summary>
    /// <param name="key">Object key.</param>
    /// <param name="contentType">Content type the client will send.</param>
    /// <param name="lifetime">How long the URL stays valid.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The presigned URL.</returns>
    Task<Uri> GenerateUploadUrlAsync(
        string key,
        string contentType,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    /// <summary>Issues a presigned URL the browser can GET directly.</summary>
    /// <param name="key">Object key.</param>
    /// <param name="lifetime">How long the URL stays valid.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The presigned URL.</returns>
    Task<Uri> GenerateDownloadUrlAsync(
        string key,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    /// <summary>Builds the publicly reachable URL for an object, when the bucket allows it.</summary>
    /// <param name="key">Object key.</param>
    /// <returns>The public URL, or null when no public base URL is configured.</returns>
    Uri? BuildPublicUrl(string key);

    /// <summary>Builds the tenant scoped key for a file.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="fileType">File category.</param>
    /// <param name="fileName">File name, including extension.</param>
    /// <returns>The object key.</returns>
    static string BuildKey(string tenantId, FileType fileType, string fileName) =>
        $"{StorageConstants.TenantPrefix}/{tenantId}/{fileType.ToString().ToLowerInvariant()}/{fileName}";
}
