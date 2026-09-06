namespace Dental.Framework.Storage;

/// <summary>Metadata about one stored object.</summary>
/// <param name="Key">Object key, relative to the bucket.</param>
/// <param name="SizeInBytes">Size in bytes.</param>
/// <param name="ContentType">Content type recorded at upload.</param>
/// <param name="LastModified">Last modification time reported by the provider.</param>
public sealed record StoredObject(
    string Key,
    long SizeInBytes,
    string? ContentType,
    DateTimeOffset? LastModified);
