using IP;
using Mets;
using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base class responsible for parsing E-ARK METS files into a <see cref="MetsWrapper"/>.
/// </summary>
/// <remarks>
/// Read-side counterpart of <see cref="EARKMETSCreator"/>. The parser is intentionally thin: it deserialises
/// the METS XML via <see cref="METSUtils.UnmarshalMETS"/>, locates the E-ARK structural map, and indexes the
/// first-level divisions onto the <see cref="MetsWrapper"/> so the higher-level read orchestrator
/// (<c>EARKReadUtils</c>) can walk them without knowing the spec layout.
///
/// Version-specific differences between 2.0.4 / 2.1.0 / 2.2.0 are accommodated by subclasses; the structural
/// layout that this base class understands is the common subset shared by all supported versions.
/// </remarks>
public abstract class EARKMETSParser
{
    /// <summary>
    /// Submission division label, lowercase. Not yet defined in <see cref="IPConstants"/>; kept here so the
    /// parser can recognise it on read without forcing the write side to expose it.
    /// </summary>
    protected const string SUBMISSION = "submission";

    private static readonly ILogger<EARKMETSParser> logger = DefaultLogger.Create<EARKMETSParser>();

    /// <summary>
    /// Deserialises the METS document at <paramref name="metsFilePath"/> and returns a wrapper around it.
    /// </summary>
    /// <param name="metsFilePath">Absolute path to the METS file.</param>
    /// <param name="report">Validation report to which parse failures are recorded.</param>
    /// <returns>A <see cref="MetsWrapper"/>; <c>Mets</c> may be <c>null</c> if parsing failed.</returns>
    public virtual MetsWrapper ParseMets(string metsFilePath, ValidationReport report)
    {
        Mets.Mets? mets = METSUtils.UnmarshalMETS(metsFilePath, report);
        return new MetsWrapper(mets!, metsFilePath);
    }

    /// <summary>
    /// Finds the E-ARK structural map (labelled <see cref="IPConstants.COMMON_SPEC_STRUCTURAL_MAP"/>) in the METS document.
    /// </summary>
    /// <param name="metsWrapper">The wrapper holding the parsed METS object.</param>
    /// <param name="report">Validation report that receives an ERROR entry if the struct map is missing.</param>
    /// <param name="mainMets">Whether this is the root METS or a representation METS (controls error message).</param>
    /// <returns>The matching <see cref="StructMapType"/>, or <c>null</c> if none was found.</returns>
    public virtual StructMapType? ExtractStructMap(MetsWrapper metsWrapper, ValidationReport report, bool mainMets)
    {
        if (metsWrapper?.Mets?.StructMap == null) return null;

        foreach (StructMapType structMap in metsWrapper.Mets.StructMap)
        {
            if (string.Equals(structMap.Label, IPConstants.COMMON_SPEC_STRUCTURAL_MAP, StringComparison.Ordinal))
            {
                return structMap;
            }
        }

        report.AddError(
            ValidationConstants.METS_STRUCTMAP_MISSING,
            mainMets
                ? "Main METS has no E-ARK structural map"
                : "Representation METS has no E-ARK structural map",
            metsWrapper.MetsPath);
        return null;
    }

    /// <summary>
    /// Indexes the first-level divisions of <paramref name="structMap"/> onto <paramref name="metsWrapper"/>.
    /// </summary>
    /// <remarks>
    /// Mirror of <see cref="EARKMETSCreator.AddCommonDivsToMainDiv"/>: the writer populates these div references
    /// while building, the parser populates them while reading. Labels are matched case-insensitively because
    /// the spec allows either capitalised (<c>"Metadata"</c>) or lowercase (<c>"metadata"</c>) forms.
    /// </remarks>
    public virtual void PreProcessStructMap(MetsWrapper metsWrapper, StructMapType structMap)
    {
        if (structMap?.Div == null) return;

        DivType ipDiv = structMap.Div;
        metsWrapper.MainDiv = ipDiv;

        if (ipDiv.Div == null) return;

        foreach (DivType firstLevel in ipDiv.Div)
        {
            string? label = firstLevel.Label;
            if (label == null) continue;

            if (string.Equals(label, IPConstants.METADATA, StringComparison.OrdinalIgnoreCase)
                || string.Equals(label, IPConstants.METADATA_WITH_FIRST_LETTER_CAPITAL, StringComparison.OrdinalIgnoreCase))
            {
                metsWrapper.MetadataDiv = firstLevel;
            }
            else if (string.Equals(label, IPConstants.METADATA + IPConstants.METS_PATH_SEPARATOR + IPConstants.OTHER, StringComparison.OrdinalIgnoreCase)
                || string.Equals(label, IPConstants.METADATA_WITH_FIRST_LETTER_CAPITAL + IPConstants.METS_PATH_SEPARATOR + IPConstants.OTHER_WITH_FIRST_LETTER_CAPITAL, StringComparison.OrdinalIgnoreCase))
            {
                metsWrapper.OtherMetadataDiv = firstLevel;
            }
            else if (string.Equals(label, IPConstants.DATA, StringComparison.OrdinalIgnoreCase)
                || string.Equals(label, IPConstants.DATA_WITH_FIRST_LETTER_CAPITAL, StringComparison.OrdinalIgnoreCase))
            {
                metsWrapper.DataDiv = firstLevel;
            }
            else if (string.Equals(label, IPConstants.SCHEMAS, StringComparison.OrdinalIgnoreCase)
                || string.Equals(label, IPConstants.SCHEMAS_WITH_FIRST_LETTER_CAPITAL, StringComparison.OrdinalIgnoreCase))
            {
                metsWrapper.SchemasDiv = firstLevel;
            }
            else if (string.Equals(label, IPConstants.DOCUMENTATION, StringComparison.OrdinalIgnoreCase)
                || string.Equals(label, IPConstants.DOCUMENTATION_WITH_FIRST_LETTER_CAPITAL, StringComparison.OrdinalIgnoreCase))
            {
                metsWrapper.DocumentationDiv = firstLevel;
            }
            else if (string.Equals(label, SUBMISSION, StringComparison.OrdinalIgnoreCase))
            {
                metsWrapper.SubmissionsDiv = firstLevel;
            }
            // Representations/<id> divs are not indexed here; the read orchestrator iterates them
            // directly off MainDiv.Div in ProcessRepresentations.
        }
    }

    /// <summary>
    /// Creates an <see cref="IPAgent"/> from a METS-level <see cref="MetsTypeMetsHdrAgent"/>.
    /// </summary>
    /// <remarks>
    /// Read-side mirror of the agent creation done on the write side. The first note (if present) is mapped to
    /// the agent's note text and note type; additional notes are dropped.
    /// </remarks>
    public virtual IPAgent CreateIPAgent(MetsTypeMetsHdrAgent metsAgent)
    {
        string note = string.Empty;
        Xml.Mets.CsipExtensionMets.Notetype noteType = Xml.Mets.CsipExtensionMets.Notetype.NOT_SET;

        if (metsAgent.Note != null && metsAgent.Note.Count > 0)
        {
            MetsTypeMetsHdrAgentNote first = metsAgent.Note[0];
            note = first.Value ?? string.Empty;
            noteType = first.Notetype;
        }

        return new IPAgent(
            metsAgent.Name ?? string.Empty,
            metsAgent.Role,
            metsAgent.Otherrole,
            metsAgent.Type,
            metsAgent.Othertype,
            note,
            noteType
        );
    }
}
