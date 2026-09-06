namespace Dental.Framework.Shared.Storage;

/// <summary>
/// Category of an uploaded file. The category - not the client supplied content type - decides which
/// extensions and which size ceiling are allowed.
/// </summary>
public enum FileType
{
    /// <summary>Profile and practice imagery.</summary>
    Image = 0,

    /// <summary>Signed forms, referrals, lab slips.</summary>
    Document = 1,

    /// <summary>Intraoral and panoramic radiographs.</summary>
    Radiograph = 2,

    /// <summary>Generated statements and reports.</summary>
    Report = 3,
}
