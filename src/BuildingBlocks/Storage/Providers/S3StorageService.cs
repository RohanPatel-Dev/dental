using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Dental.Framework.Storage.Providers;

/// <summary>S3 and MinIO provider.</summary>
/// <param name="client">Configured S3 client.</param>
/// <param name="options">Storage configuration.</param>
public sealed class S3StorageService(IAmazonS3 client, IOptions<StorageOptions> options) : IStorageService
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

        PutObjectRequest request = new()
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true,
        };

        await client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);

        StoredObject? stored = await HeadObjectAsync(key, cancellationToken).ConfigureAwait(false);
        return stored ?? new StoredObject(key, content.CanSeek ? content.Length : 0, contentType, null);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        client.DeleteObjectAsync(_options.Bucket, key, cancellationToken);

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        GetObjectResponse response = await client
            .GetObjectAsync(_options.Bucket, key, cancellationToken)
            .ConfigureAwait(false);

        return response.ResponseStream;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        await HeadObjectAsync(key, cancellationToken).ConfigureAwait(false) is not null;

    /// <inheritdoc />
    public async Task<long?> GetSizeAsync(string key, CancellationToken cancellationToken = default) =>
        (await HeadObjectAsync(key, cancellationToken).ConfigureAwait(false))?.SizeInBytes;

    /// <inheritdoc />
    public async Task<StoredObject?> HeadObjectAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            GetObjectMetadataResponse metadata = await client
                .GetObjectMetadataAsync(_options.Bucket, key, cancellationToken)
                .ConfigureAwait(false);

            return new StoredObject(
                key,
                metadata.ContentLength,
                metadata.Headers.ContentType,
                metadata.LastModified);
        }
        catch (AmazonS3Exception exception)
            when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Uri> GenerateUploadUrlAsync(
        string key,
        string contentType,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        GetPreSignedUrlRequest request = new()
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(lifetime),
            ContentType = contentType,
        };

        string url = await client.GetPreSignedURLAsync(request).ConfigureAwait(false);
        return new Uri(url);
    }

    /// <inheritdoc />
    public async Task<Uri> GenerateDownloadUrlAsync(
        string key,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        GetPreSignedUrlRequest request = new()
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime),
        };

        string url = await client.GetPreSignedURLAsync(request).ConfigureAwait(false);
        return new Uri(url);
    }

    /// <inheritdoc />
    public Uri? BuildPublicUrl(string key) =>
        string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? null
            : new Uri($"{_options.PublicBaseUrl.TrimEnd('/')}/{_options.Bucket}/{key}");
}
