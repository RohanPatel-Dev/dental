namespace Dental.Framework.Shared.Storage;

/// <summary>Bucket prefixes and lifetimes used by the storage subsystem.</summary>
public static class StorageConstants
{
    /// <summary>Prefix under which tenant scoped objects are written.</summary>
    public const string TenantPrefix = "tenants";

    /// <summary>How long a presigned upload URL stays valid.</summary>
    public static readonly TimeSpan UploadUrlLifetime = TimeSpan.FromMinutes(15);

    /// <summary>How long a presigned download URL stays valid.</summary>
    public static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromMinutes(10);

    /// <summary>How long a pending upload may stay unfinalized before it is swept.</summary>
    public static readonly TimeSpan PendingUploadLifetime = TimeSpan.FromHours(6);
}
