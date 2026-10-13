/// <summary>
/// Stable identifiers and standard messages used when populating a <see cref="ValidationReport"/>.
/// </summary>
/// <remarks>
/// IDs are stable strings so downstream consumers can match on them without parsing free-text messages.
/// </remarks>
public static class ValidationConstants
{
    // Package-level
    /// <summary>
    /// Source path does not exist or is not a file.
    /// </summary>
    public const string SOURCE_NOT_FOUND = "PKG-001";
    /// <summary>
    /// Source is not a valid ZIP file.
    /// </summary>
    public const string NOT_A_ZIP = "PKG-002";
    /// <summary>
    /// ZIP extraction failed.
    /// </summary>
    public const string ZIP_EXTRACTION_FAILED = "PKG-003";

    // METS-level
    /// <summary>
    /// Root METS.xml was not found at the expected location.
    /// </summary>
    public const string METS_FILE_NOT_FOUND = "METS-001";
    /// <summary>
    /// METS.xml could not be parsed (malformed XML or schema violation).
    /// </summary>
    public const string METS_UNMARSHAL_FAILED = "METS-002";
    /// <summary>
    /// The METS document declares an unrecognised or unsupported profile/version.
    /// </summary>
    public const string METS_UNKNOWN_PROFILE = "METS-003";
    /// <summary>
    /// The expected E-ARK structMap was missing from the METS document.
    /// </summary>
    public const string METS_STRUCTMAP_MISSING = "METS-004";

    // File-level
    /// <summary>
    /// A file referenced by METS was not found on disk.
    /// </summary>
    public const string FILE_NOT_FOUND = "FILE-001";
    /// <summary>
    /// A file's computed checksum did not match the METS-declared checksum.
    /// </summary>
    public const string CHECKSUM_MISMATCH = "FILE-002";
    /// <summary>
    /// The checksum algorithm declared in METS is not supported by this implementation.
    /// </summary>
    public const string UNSUPPORTED_CHECKSUM_ALGORITHM = "FILE-003";
    /// <summary>
    /// The METS-declared checksum or algorithm is missing.
    /// </summary>
    public const string CHECKSUM_NOT_DECLARED = "FILE-004";

    // Metadata-level
    /// <summary>
    /// A metadata reference (MdRef) is missing required attributes.
    /// </summary>
    public const string MDREF_INCOMPLETE = "META-001";
    /// <summary>
    /// Failed to read referenced metadata file content.
    /// </summary>
    public const string METADATA_READ_FAILED = "META-002";

    // Representation-level
    /// <summary>
    /// A representation METS file pointer was not resolvable.
    /// </summary>
    public const string REPRESENTATION_METS_MISSING = "REP-001";
    /// <summary>
    /// A representation METS file failed to parse.
    /// </summary>
    public const string REPRESENTATION_METS_INVALID = "REP-002";
}
