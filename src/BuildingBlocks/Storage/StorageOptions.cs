using System.ComponentModel.DataAnnotations;

namespace Dental.Framework.Storage;

/// <summary>
/// Storage configuration.
/// </summary>
/// <remarks>
/// NOTE the deliberate exception to the "section name equals type name" rule: this binds from the
/// <c>Storage</c> section, not <c>StorageOptions</c>. It is the only such exception in the codebase.
/// </remarks>
public sealed class StorageOptions
{
    /// <summary>Configuration section this type binds from.</summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// <c>s3</c> selects S3 or MinIO; anything else selects local disk. Read EAGERLY at
    /// registration, so a test must re-register the provider rather than only overlay configuration.
    /// </summary>
    public string Provider { get; set; } = "local";

    /// <summary>Bucket (or root folder) objects are written to.</summary>
    [Required]
    public string Bucket { get; set; } = "dental";

    /// <summary>S3 endpoint. Set for MinIO; leave empty for AWS.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>S3 region.</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>S3 access key.</summary>
    public string? AccessKey { get; set; }

    /// <summary>S3 secret key.</summary>
    public string? SecretKey { get; set; }

    /// <summary>Forces path style addressing, which MinIO requires.</summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>Root directory for the local provider.</summary>
    public string LocalRoot { get; set; } = "storage";

    /// <summary>Base URL used to build publicly reachable object URLs.</summary>
    public string? PublicBaseUrl { get; set; }
}
