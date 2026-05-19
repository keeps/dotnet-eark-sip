/// <summary>
/// Severity level of a <see cref="ValidationEntry"/>.
/// </summary>
public enum ValidationEntryLevel
{
    /// <summary>Informational entry; does not affect validity.</summary>
    INFO,
    /// <summary>Non-fatal issue; does not affect validity.</summary>
    WARN,
    /// <summary>Fatal issue; flips the owning IP to invalid.</summary>
    ERROR
}

/// <summary>
/// A single entry in a <see cref="ValidationReport"/>, recorded while parsing or validating an Information Package.
/// </summary>
public class ValidationEntry
{
    /// <summary>Severity level.</summary>
    public ValidationEntryLevel Level { get; }
    /// <summary>Stable identifier for the rule that produced this entry (see <see cref="ValidationConstants"/>).</summary>
    public string Id { get; }
    /// <summary>Human-readable description.</summary>
    public string Message { get; }
    /// <summary>Optional pointer to where in the package the issue was found (file path, METS XPath, etc.).</summary>
    public string? Location { get; }
    /// <summary>Optional structured details (exception message, expected vs. actual checksum, etc.).</summary>
    public string? Details { get; }
    /// <summary>UTC timestamp the entry was recorded.</summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new <see cref="ValidationEntry"/>.
    /// </summary>
    public ValidationEntry(ValidationEntryLevel level, string id, string message, string? location = null, string? details = null)
    {
        Level = level;
        Id = id;
        Message = message;
        Location = location;
        Details = details;
        Timestamp = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[{Level}] {Id}: {Message}" +
            (Location != null ? $" (at {Location})" : "") +
            (Details != null ? $" — {Details}" : "");
    }
}
