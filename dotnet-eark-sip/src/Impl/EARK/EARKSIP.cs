using System.Reflection;
using IP;
using IPEnums;
using Mets;
using Microsoft.Extensions.Logging;

/// <summary>
/// Represents an implementation of a SIP (Submission Information Package) for EARK.
/// </summary>
public class EARKSIP : SIP
{
    private static readonly ILogger<EARKSIP> logger = DefaultLogger.Create<EARKSIP>();
    private static readonly string SIP_TEMP_DIR = "EARKSIP";
    private static readonly string DEFAULT_SIP_VERSION = "2.1.0";

    private static readonly string SOFTWARE_VERSION = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    private readonly EARKMETSCreator metsCreator;

    /// <summary>
    /// Initializes a new instance of the <see cref="EARKSIP"/> class with default settings.
    /// </summary>
    public EARKSIP() : base()
    {
        SetProfile(IPConstants.SIP_SPEC_PROFILE);
        metsCreator = new METSGeneratorFactory().GetGenerator(DEFAULT_SIP_VERSION);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EARKSIP"/> class with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier for the SIP.</param>
    /// <remarks>Uses the default SIP version - 2.1.0</remarks>
    public EARKSIP(string id) : base(id)
    {
        SetProfile(IPConstants.SIP_SPEC_PROFILE);
        metsCreator = new METSGeneratorFactory().GetGenerator(DEFAULT_SIP_VERSION);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EARKSIP"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The identifier for the SIP.</param>
    /// <param name="contentType">The content type of the SIP.</param>
    /// <param name="contentInformationType">The content information type of the SIP.</param>
    /// <param name="version">The version of the SIP specification.</param>
    public EARKSIP(string id, IPContentType contentType, IPContentInformationType contentInformationType, string version) : base(id, contentType, contentInformationType)
    {
        if (version == "2.2.0" || version == "2.1.0")
        {
            SetProfile(IPConstants.SIP_SPEC_PROFILE.Replace(".xml", "-v" + version.Replace(".", "-") + ".xml"));
        }
        else
        {
            SetProfile(IPConstants.SIP_SPEC_PROFILE);
        }

        metsCreator = new METSGeneratorFactory().GetGenerator(version);
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    /// <summary>
    /// Initializes a new instance of the <see cref="EARKSIP"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The identifier for the SIP.</param>
    /// <param name="contentType">The content type of the SIP.</param>
    /// <param name="contentInformationType">The content information type of the SIP.</param>
    /// <remarks>Uses the default SIP version - 2.1.0</remarks>
    public EARKSIP(string id, IPContentType contentType, IPContentInformationType contentInformationType)
    {
        new EARKSIP(id, contentType, contentInformationType, DEFAULT_SIP_VERSION);
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

    /// <summary>
    /// Builds the SIP package with the specified write strategy.
    /// Uses defaults for all other parameters.
    /// </summary>
    /// <param name="writeStrategy">The strategy used to write the SIP package.</param>
    /// <returns>The path to the built SIP package.</returns>
    public override string Build(IWriteStrategy writeStrategy)
    {
        return Build(writeStrategy, false);
    }

    /// <summary>
    /// Builds the SIP package with the specified parameters.
    /// Uses defaults for all other parameters.
    /// </summary>
    /// <param name="writeStrategy">The strategy used to write the SIP package.</param>
    /// <param name="onlyManifest">Indicates whether only the manifest should be built.</param>
    /// <returns>The path to the built SIP package.</returns>
    public override string Build(IWriteStrategy writeStrategy, bool onlyManifest)
    {
        return Build(writeStrategy, null, onlyManifest);
    }

    /// <summary>
    /// Builds the SIP package with the specified parameters.
    /// Uses defaults for all other parameters.
    /// </summary>
    /// <param name="writeStrategy">The strategy used to write the SIP package.</param>
    /// <param name="fileNameWithoutExtension">The base name for the output file, without extension.</param>
    /// <returns>The path to the built SIP package.</returns>
    public override string Build(IWriteStrategy writeStrategy, string fileNameWithoutExtension)
    {
        return Build(writeStrategy, fileNameWithoutExtension, false, SIPType.EARK2);
    }

    /// <summary>
    /// Builds the SIP package with the specified parameters.
    /// </summary>
    /// <param name="writeStrategy">The strategy used to write the SIP package.</param>
    /// <param name="fileNameWithoutExtension">The base name for the output file, without extension.</param>
    /// <param name="sipType">The type of SIP to build.</param>
    /// <returns>The path to the built SIP package.</returns>
    public override string Build(IWriteStrategy writeStrategy, string fileNameWithoutExtension, SIPType sipType)
    {
        return Build(writeStrategy, fileNameWithoutExtension, false, sipType);
    }

    /// <summary>
    /// Builds the SIP package with the specified parameters.
    /// </summary>
    /// <param name="writeStrategy">The strategy used to write the SIP package.</param>
    /// <param name="fileNameWithoutExtension">The base name for the output file, without extension.</param>
    /// <param name="onlyManifest">Indicates whether only the manifest should be built.</param>
    /// <returns>The path to the built SIP package.</returns>
    public override string Build(IWriteStrategy writeStrategy, string? fileNameWithoutExtension, bool onlyManifest)
    {
        return Build(writeStrategy, fileNameWithoutExtension, onlyManifest, SIPType.EARK2);
    }

    /// <summary>
    /// Builds the SIP package with the specified parameters.
    /// </summary>
    /// <param name="writeStrategy">The strategy used to write the SIP package.</param>
    /// <param name="fileNameWithoutExtension">The base name for the output file, without extension.</param>
    /// <param name="onlyManifest">Indicates whether only the manifest should be built.</param>
    /// <param name="sipType">The type of SIP to build.</param>
    /// <returns>The path to the built SIP package.</returns>
    public override string Build(IWriteStrategy writeStrategy, string? fileNameWithoutExtension, bool onlyManifest, SIPType sipType)
    {
        IPConstants.METS_ENCODE_AND_DECODE_HREF = true;
        string buildDir = ModelUtils.CreateBuildDir(SIP_TEMP_DIR).FullName;

        EARKUtils earkUtils = new EARKUtils(metsCreator);

        try
        {
            AddCreatorSoftwareAgent("dotnet-eark-sip", SOFTWARE_VERSION);

            Dictionary<string, IZipEntryInfo> zipEntries = GetZipEntries();
            earkUtils.AddDefaultSchemas(logger, GetSchemas(), buildDir, GetOverride());

            bool isMetadataOther = GetOtherMetadata() != null && GetOtherMetadata().Count > 0;
            bool isMetadata = (GetDescriptiveMetadata() != null && GetDescriptiveMetadata().Count > 0)
                || (GetPreservationMetadata() != null && GetPreservationMetadata().Count > 0)
                || (GetTechnicalMetadata() != null && GetTechnicalMetadata().Count > 0)
                || (GetSourceMetadata() != null && GetSourceMetadata().Count > 0)
                || (GetRightsMetadata() != null && GetRightsMetadata().Count > 0);
            bool isDocumentation = GetDocumentation() != null && GetDocumentation().Count > 0;
            bool isSchemas = GetSchemas() != null && GetSchemas().Count > 0;
            bool isRepresentations = GetRepresentations() != null && GetRepresentations().Count > 0;

            MetsWrapper mainMETSWrapper = metsCreator.GenerateMets(
                string.Join(" ", GetIds()),
                GetDescription(),
                GetProfile(),
                true,
                GetAncestors(),
                null,
                GetHeader(),
                GetContentType(),
                GetContentInformationType(),
                isMetadata,
                isMetadataOther,
                isSchemas,
                isDocumentation,
                isRepresentations,
                false
            );

            earkUtils.AddDescriptiveMetadataToZipAndMETS(zipEntries, mainMETSWrapper, GetDescriptiveMetadata(), null);
            earkUtils.AddPreservationMetadataToZipAndMETS(zipEntries, mainMETSWrapper, GetPreservationMetadata(), null);
            earkUtils.AddTechnicalMetadataToZipAndMETS(zipEntries, mainMETSWrapper, GetTechnicalMetadata(), null);
            earkUtils.AddSourceMetadataToZipAndMETS(zipEntries, mainMETSWrapper, GetSourceMetadata(), null);
            earkUtils.AddRightsMetadataToZipAndMETS(zipEntries, mainMETSWrapper, GetRightsMetadata(), null);
            earkUtils.AddOtherMetadataToZipAndMETS(zipEntries, mainMETSWrapper, GetOtherMetadata(), null);
            earkUtils.AddRepresentationsToZipAndMETS(this, GetRepresentations(), zipEntries, mainMETSWrapper, buildDir, sipType);
            earkUtils.AddSchemasToZipAndMETS(zipEntries, mainMETSWrapper, GetSchemas(), null);
            earkUtils.AddDocumentationToZipAndMETS(zipEntries, mainMETSWrapper, GetDocumentation(), null);

            METSUtils.AddMainMETSToZip(zipEntries, mainMETSWrapper, buildDir);
            NotifySipBuildPackagingStarted(zipEntries.Count);
            return writeStrategy.Write(zipEntries, this, fileNameWithoutExtension, GetId(), true);
        }
        catch (Exception e)
        {
            ModelUtils.CleanUpUponInterrupt(logger, writeStrategy.DestinationPath);
            throw e;
        }
        finally
        {
            ModelUtils.DeleteBuildDir(buildDir);
        }
    }

    /// <summary>
    /// Gets the extra checksum algorithms supported by this SIP implementation.
    /// </summary>
    /// <returns>A set of extra checksum algorithms.</returns>
    public override HashSet<IFilecoreChecksumtype> GetExtraChecksumAlgorithms()
    {
        return new HashSet<IFilecoreChecksumtype>();
    }

    /// <summary>
    /// Parses an E-ARK SIP ZIP archive into a fully populated <see cref="SIP"/> object graph.
    /// </summary>
    /// <param name="source">Path to the .zip file containing the SIP.</param>
    /// <returns>The parsed <see cref="SIP"/>. Inspect <c>sip.GetValidationReport()</c> and <c>sip.IsValid()</c> to check for issues.</returns>
    /// <remarks>
    /// Creates a temporary directory for extraction. To control the extraction location, use the
    /// <see cref="Parse(string, string)"/> overload.
    ///
    /// Errors during parsing (malformed METS, missing files, checksum mismatches, unsupported algorithms) are
    /// recorded as ERROR entries on the SIP's <see cref="ValidationReport"/>; the method only throws if the
    /// source cannot be opened as a ZIP or no destination directory can be created.
    /// </remarks>
    public static SIP Parse(string source)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "dotnet-eark-sip-extracted-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return Parse(source, tempDir);
    }

    /// <summary>
    /// Parses an E-ARK SIP ZIP archive into a fully populated <see cref="SIP"/> object graph, extracting into the given directory.
    /// </summary>
    /// <param name="source">Path to the .zip file containing the SIP.</param>
    /// <param name="destinationDirectory">Directory to extract the SIP into. Caller owns its lifetime.</param>
    /// <returns>The parsed <see cref="SIP"/>. Inspect <c>sip.GetValidationReport()</c> and <c>sip.IsValid()</c> to check for issues.</returns>
    /// <exception cref="IPException">Thrown if <paramref name="source"/> cannot be opened or extracted.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="source"/> is null or empty.</exception>
    public static SIP Parse(string source, string destinationDirectory)
    {
        if (string.IsNullOrEmpty(source))
        {
            throw new ArgumentException("Source path must be provided.", nameof(source));
        }
        if (!File.Exists(source) && !Directory.Exists(source))
        {
            EARKSIP earlyFailSip = new EARKSIP();
            earlyFailSip.GetValidationReport().AddError(
                ValidationConstants.SOURCE_NOT_FOUND,
                "SIP source does not exist",
                source);
            return earlyFailSip;
        }

        IPConstants.METS_ENCODE_AND_DECODE_HREF = true;

        EARKSIP sip = new EARKSIP();
        ValidationReport report = sip.GetValidationReport();

        IReadStrategy readStrategy = new ZipReadStrategyFactory().Create(destinationDirectory);
        string sipPath;
        try
        {
            sipPath = readStrategy.Read(source);
        }
        catch (IPException e)
        {
            report.AddError(ValidationConstants.ZIP_EXTRACTION_FAILED, "Failed to extract SIP", source, e.Message);
            return sip;
        }

        sip.SetBasePath(sipPath);

        string mainMetsPath = Path.Combine(sipPath, IPConstants.METS_FILE);
        string detectedVersion = METSUtils.DetectMETSVersion(mainMetsPath, report);
        EARKMETSParser parser = new METSGeneratorFactory().GetParser(detectedVersion);
        EARKReadUtils readUtils = new EARKReadUtils(parser);

        MetsWrapper mainWrapper = readUtils.ProcessMainMETS(sip, sipPath);
        if (mainWrapper.Mets == null)
        {
            // Catastrophic parse failure already logged; nothing further to do.
            return sip;
        }

        StructMapType? structMap = parser.ExtractStructMap(mainWrapper, report, true);
        if (structMap != null)
        {
            parser.PreProcessStructMap(mainWrapper, structMap);

            readUtils.ProcessDescriptiveMetadata(mainWrapper, sip, null, sipPath);
            readUtils.ProcessOtherMetadata(mainWrapper, sip, null, sipPath);
            readUtils.ProcessAdministrativeMetadata(mainWrapper, sip, null, sipPath);
            readUtils.ProcessRepresentations(mainWrapper, sip);
            readUtils.ProcessSchemasAndDocumentation(mainWrapper, sip, sipPath);
            readUtils.ProcessAncestors(mainWrapper, sip);
        }

        return sip;
    }
}