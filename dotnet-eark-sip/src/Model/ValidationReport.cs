/// <summary>
/// Collects <see cref="ValidationEntry"/> instances produced while parsing or validating an Information Package.
/// </summary>
/// <remarks>
/// An IP is considered valid if its report contains no entry of level <see cref="ValidationEntryLevel.ERROR"/>. 
/// Adding any error entry flips <see cref="IsValid"/> to <c>false</c> automatically.
/// </remarks>
public class ValidationReport
{
    private readonly List<ValidationEntry> entries = new List<ValidationEntry>();

    /// <summary>
    /// Returns true while no <see cref="ValidationEntryLevel.ERROR"/> entries have been recorded.
    /// </summary>
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Snapshot of all recorded entries, in insertion order.
    /// </summary>
    public IReadOnlyList<ValidationEntry> Entries => entries;

    /// <summary>
    /// Records a new validation entry. If <paramref name="entry"/> has level <see cref="ValidationEntryLevel.ERROR"/>,
    /// <see cref="IsValid"/> is set to <c>false</c>.
    /// </summary>
    public ValidationReport AddEntry(ValidationEntry entry)
    {
        entries.Add(entry);
        if (entry.Level == ValidationEntryLevel.ERROR)
        {
            IsValid = false;
        }
        return this;
    }

    /// <summary>Records an INFO entry.</summary>
    public ValidationReport AddInfo(string id, string message, string? location = null, string? details = null)
        => AddEntry(new ValidationEntry(ValidationEntryLevel.INFO, id, message, location, details));

    /// <summary>Records a WARN entry.</summary>
    public ValidationReport AddWarning(string id, string message, string? location = null, string? details = null)
        => AddEntry(new ValidationEntry(ValidationEntryLevel.WARN, id, message, location, details));

    /// <summary>Records an ERROR entry; flips <see cref="IsValid"/> to false.</summary>
    public ValidationReport AddError(string id, string message, string? location = null, string? details = null)
        => AddEntry(new ValidationEntry(ValidationEntryLevel.ERROR, id, message, location, details));

    /// <summary>
    /// Returns the entries of a given level.
    /// </summary>
    public IEnumerable<ValidationEntry> GetEntries(ValidationEntryLevel level)
    {
        foreach (ValidationEntry entry in entries)
        {
            if (entry.Level == level) yield return entry;
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"ValidationReport [valid={IsValid}, entries={entries.Count}]";
    }
}
