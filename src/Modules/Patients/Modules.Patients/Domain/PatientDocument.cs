using Dental.Framework.Core.Domain;
using Dental.Framework.Shared.Storage;

namespace Dental.Modules.Patients.Domain;

/// <summary>
/// A file attached to a patient record - a signed consent form, a referral, a radiograph.
/// </summary>
/// <remarks>
/// Reached only through <see cref="Patient.Documents"/>, so its EF configuration sets
/// <c>ValueGeneratedNever()</c> on the key. Without that, EF marks a newly added child as
/// <c>Modified</c> instead of <c>Added</c> and the insert misbehaves silently.
/// </remarks>
public sealed class PatientDocument : BaseEntity
{
    /// <summary>Owning patient.</summary>
    public Guid PatientId { get; set; }

    /// <summary>File name as uploaded.</summary>
    public string FileName { get; set; } = default!;

    /// <summary>Object storage key.</summary>
    public string StorageKey { get; set; } = default!;

    /// <summary>File category, which decided the extension and size rules.</summary>
    public FileType FileType { get; set; }

    /// <summary>Size in bytes, recorded at finalization.</summary>
    public long SizeInBytes { get; set; }

    /// <summary>Content type recorded at upload.</summary>
    public string? ContentType { get; set; }

    /// <summary>False until the client has finished the presigned upload.</summary>
    public bool IsAvailable { get; set; }
}
