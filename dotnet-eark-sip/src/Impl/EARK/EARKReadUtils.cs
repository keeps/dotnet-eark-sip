using IP;
using IPEnums;
using Mets;
using Microsoft.Extensions.Logging;

/// <summary>
/// Read-side counterpart to <see cref="EARKUtils"/>. Orchestrates parsing of an extracted E-ARK SIP into the
/// in-memory <see cref="IP.IP"/> / <see cref="IP.SIP"/> / <see cref="IP.IPRepresentation"/> object graph.
/// </summary>
/// <remarks>
/// Where <see cref="EARKUtils"/> builds METS structures while collecting ZIP entries, this class consumes a
/// pre-parsed <see cref="MetsWrapper"/> (produced by <see cref="EARKMETSParser"/>) and walks its dmdSec,
/// amdSec, fileSec, and structMap to populate the IP model. All file references are verified against the
/// checksums declared in METS; verification failures are recorded as ERROR entries on the IP's
/// <see cref="ValidationReport"/>.
/// </remarks>
public class EARKReadUtils
{
    private static readonly ILogger<EARKReadUtils> logger = DefaultLogger.Create<EARKReadUtils>();

    private readonly EARKMETSParser metsParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="EARKReadUtils"/> class.
    /// </summary>
    /// <param name="metsParser">The version-specific METS parser to use for deserialising METS files.</param>
    public EARKReadUtils(EARKMETSParser metsParser)
    {
        this.metsParser = metsParser;
    }

    /// <summary>
    /// Parses the root METS file of a SIP and populates header-level data on <paramref name="ip"/>.
    /// </summary>
    /// <param name="ip">The IP to populate. Must already have its base path set.</param>
    /// <param name="sipPath">Absolute path to the SIP root directory (the folder that contains <c>METS.xml</c>).</param>
    /// <returns>
    /// A <see cref="MetsWrapper"/> wrapping the parsed root METS document. If parsing failed, <see cref="MetsWrapper.Mets"/>
    /// is <c>null</c> and the IP's <see cref="ValidationReport"/> has an ERROR entry describing the failure.
    /// </returns>
    public MetsWrapper ProcessMainMETS(IP.IP ip, string sipPath)
    {
        string mainMetsFile = Path.Combine(sipPath, IPConstants.METS_FILE);
        ValidationReport report = ip.GetValidationReport();

        if (!File.Exists(mainMetsFile))
        {
            report.AddError(ValidationConstants.METS_FILE_NOT_FOUND, "Main METS file not found", mainMetsFile);
            return new MetsWrapper(null!, mainMetsFile);
        }

        MetsWrapper wrapper = metsParser.ParseMets(mainMetsFile, report);
        if (wrapper.Mets == null) return wrapper;

        Mets.Mets mets = wrapper.Mets;

        // OBJID → IDs
        if (!string.IsNullOrEmpty(mets.Objid))
        {
            ip.SetIds(new List<string>(mets.Objid.Split(' ')));
        }

        // Header
        if (mets.MetsHdr != null)
        {
            if (mets.MetsHdr.CreatedateSpecified)
            {
                ip.SetCreateDate(mets.MetsHdr.Createdate);
            }
            if (mets.MetsHdr.LastmoddateSpecified)
            {
                ip.SetModificationDate(mets.MetsHdr.Lastmoddate);
            }
            if (!string.IsNullOrEmpty(mets.MetsHdr.Recordstatus))
            {
                try
                {
                    IPStatus status = (IPStatus)Enum.Parse(typeof(IPStatus), mets.MetsHdr.Recordstatus, ignoreCase: true);
                    ip.SetStatus(status);
                }
                catch (ArgumentException)
                {
                    // Unknown status — leave the default; not an error.
                }
            }

            // Package type (SIP/AIP/DIP) — we only support SIP at this layer; mismatches are warned, not errored,
            // so consumers can decide how strict to be.
            try
            {
                Xml.Mets.CsipExtensionMets.Oaispackagetype packageType = mets.MetsHdr.Oaispackagetype;
                if (packageType == Xml.Mets.CsipExtensionMets.Oaispackagetype.SIP)
                {
                    ip.SetType(IPType.SIP);
                }
                else
                {
                    report.AddWarning(
                        ValidationConstants.METS_UNKNOWN_PROFILE,
                        $"OAISPACKAGETYPE is '{packageType}', expected 'SIP'.",
                        mainMetsFile);
                }
            }
            catch
            {
                // Enum may be unset on malformed METS — leave default.
            }

            // Agents
            if (mets.MetsHdr.Agent != null)
            {
                foreach (MetsTypeMetsHdrAgent agent in mets.MetsHdr.Agent)
                {
                    ip.AddAgent(metsParser.CreateIPAgent(agent));
                }
            }
        }

        // Content type
        SetIPContentType(mets, ip, mainMetsFile);

        return wrapper;
    }

    /// <summary>
    /// Maps METS <c>@TYPE</c> / <c>@CONTENTINFORMATIONTYPE</c> / <c>@OTHERCONTENTINFORMATIONTYPE</c> onto
    /// <see cref="IP.IP"/>'s content type and content-information type.
    /// </summary>
    private void SetIPContentType(Mets.Mets mets, IP.IP ip, string metsPath)
    {
        if (!string.IsNullOrEmpty(mets.Type))
        {
            ip.SetContentType(new IPContentType(mets.Type, mets.Othertype));
        }

        // Content information type: prefer Other when populated, otherwise the enumerated value.
        if (!string.IsNullOrEmpty(mets.Othercontentinformationtype))
        {
            ip.SetContentInformationType(new IPContentInformationType(mets.Othercontentinformationtype));
        }
        else if (mets.ContentinformationtypeSpecified)
        {
            ip.SetContentInformationType(new IPContentInformationType(mets.Contentinformationtype));
        }
    }

    /// <summary>
    /// Processes the descriptive metadata (<c>dmdSec</c> entries linked from <c>MetadataDiv.Dmdid</c>) of a METS
    /// document and attaches the resulting <see cref="IPDescriptiveMetadata"/> instances to
    /// <paramref name="ip"/> or <paramref name="representation"/>.
    /// </summary>
    /// <param name="metsWrapper">The wrapper around the METS document to read from.</param>
    /// <param name="ip">The IP owning the validation report and the metadata (when <paramref name="representation"/> is null).</param>
    /// <param name="representation">If non-null, metadata is attached to this representation instead of the IP itself.</param>
    /// <param name="basePath">The directory the METS file lives in (used to resolve referenced files).</param>
    /// <remarks>
    /// Descriptive and "other" metadata both live in the METS <c>dmdSec</c>; the distinction comes from which
    /// structMap div references the dmdSec via its <c>DMDID</c> list. Filtering by div membership is more
    /// resilient than path-prefix sniffing.
    /// </remarks>
    public void ProcessDescriptiveMetadata(MetsWrapper metsWrapper, IP.IP ip, IPRepresentation? representation, string basePath)
    {
        if (metsWrapper?.Mets?.DmdSec == null) return;
        ValidationReport report = ip.GetValidationReport();

        HashSet<string> descriptiveDmdIds = CollectDmdIds(metsWrapper.MetadataDiv);
        HashSet<string> otherDmdIds = CollectDmdIds(metsWrapper.OtherMetadataDiv);

        foreach (MdSecType mdSec in metsWrapper.Mets.DmdSec)
        {
            if (mdSec.MdRef == null) continue;
            // Skip entries the OtherMetadataDiv claims, and (when explicit) require descriptive membership.
            if (!string.IsNullOrEmpty(mdSec.Id) && otherDmdIds.Contains(mdSec.Id)) continue;
            if (descriptiveDmdIds.Count > 0 && !string.IsNullOrEmpty(mdSec.Id) && !descriptiveDmdIds.Contains(mdSec.Id)) continue;

            MdSecTypeMdRef mdRef = mdSec.MdRef;
            IPFile? metadataFile = ResolveAndValidateMdRef(ip, mdRef, basePath, IPConstants.DESCRIPTIVE);
            if (metadataFile == null) continue;

            MetadataType dmdType = BuildMetadataType(mdRef, report, mdRef.Href);

            string id = !string.IsNullOrEmpty(mdRef.Id) ? mdRef.Id : Utils.GenerateRandomAndPrefixedUUID();
            IPDescriptiveMetadata descriptiveMetadata = new IPDescriptiveMetadata(id, metadataFile, dmdType, mdRef.Mdtypeversion);
            if (mdRef.CreatedSpecified)
            {
                descriptiveMetadata.SetCreateDate(mdRef.Created);
            }

            if (representation == null)
            {
                ip.AddDescriptiveMetadata(descriptiveMetadata);
            }
            else
            {
                representation.AddDescriptiveMetadata(descriptiveMetadata);
            }
        }
    }

    /// <summary>
    /// Processes "other" metadata: <c>dmdSec</c> entries referenced by <see cref="MetsWrapper.OtherMetadataDiv"/>.
    /// </summary>
    public void ProcessOtherMetadata(MetsWrapper metsWrapper, IP.IP ip, IPRepresentation? representation, string basePath)
    {
        if (metsWrapper?.Mets?.DmdSec == null || metsWrapper.OtherMetadataDiv == null) return;

        HashSet<string> otherDmdIds = CollectDmdIds(metsWrapper.OtherMetadataDiv);
        if (otherDmdIds.Count == 0) return;

        foreach (MdSecType mdSec in metsWrapper.Mets.DmdSec)
        {
            if (mdSec.MdRef == null) continue;
            if (string.IsNullOrEmpty(mdSec.Id) || !otherDmdIds.Contains(mdSec.Id)) continue;

            IPFile? metadataFile = ResolveAndValidateMdRef(ip, mdSec.MdRef, basePath, IPConstants.OTHER);
            if (metadataFile == null) continue;

            IPMetadata other = new IPMetadata(metadataFile);
            if (!string.IsNullOrEmpty(mdSec.MdRef.Id)) other.SetID(mdSec.MdRef.Id);
            if (mdSec.MdRef.CreatedSpecified) other.SetCreateDate(mdSec.MdRef.Created);

            if (representation == null) ip.AddOtherMetadata(other);
            else representation.AddOtherMetadata(other);
        }
    }

    /// <summary>
    /// Walks each <c>amdSec</c> in the METS document and attaches preservation, technical, source, and rights
    /// metadata to the IP (or the given representation).
    /// </summary>
    public void ProcessAdministrativeMetadata(MetsWrapper metsWrapper, IP.IP ip, IPRepresentation? representation, string basePath)
    {
        if (metsWrapper?.Mets?.AmdSec == null) return;

        foreach (AmdSecType amdSec in metsWrapper.Mets.AmdSec)
        {
            ProcessAmdSubsection(amdSec.DigiprovMd, IPConstants.PRESERVATION, ip, representation, basePath);
            ProcessAmdSubsection(amdSec.TechMd, IPConstants.TECHNICAL, ip, representation, basePath);
            ProcessAmdSubsection(amdSec.SourceMd, IPConstants.SOURCE, ip, representation, basePath);
            ProcessAmdSubsection(amdSec.RightsMd, IPConstants.RIGHTS, ip, representation, basePath);
        }
    }

    private void ProcessAmdSubsection(
        IEnumerable<MdSecType>? subsection,
        string metadataType,
        IP.IP ip,
        IPRepresentation? representation,
        string basePath)
    {
        if (subsection == null) return;

        foreach (MdSecType mdSec in subsection)
        {
            if (mdSec.MdRef == null) continue;

            IPFile? metadataFile = ResolveAndValidateMdRef(ip, mdSec.MdRef, basePath, metadataType);
            if (metadataFile == null) continue;

            IPMetadata metadata = new IPMetadata(metadataFile);
            if (!string.IsNullOrEmpty(mdSec.MdRef.Id)) metadata.SetID(mdSec.MdRef.Id);
            if (mdSec.MdRef.CreatedSpecified) metadata.SetCreateDate(mdSec.MdRef.Created);
            try
            {
                metadata.SetMetadataType(mdSec.MdRef.Mdtype);
            }
            catch
            {
                // Leave default; the report already captured warnings via BuildMetadataType where relevant.
            }

            AddAdministrativeMetadata(ip, representation, metadata, metadataType);
        }
    }

    private static void AddAdministrativeMetadata(IP.IP ip, IPRepresentation? representation, IPMetadata metadata, string metadataType)
    {
        if (representation == null)
        {
            if (metadataType == IPConstants.PRESERVATION) ip.AddPreservationMetadata(metadata);
            else if (metadataType == IPConstants.TECHNICAL) ip.AddTechnicalMetadata(metadata);
            else if (metadataType == IPConstants.SOURCE) ip.AddSourceMetadata(metadata);
            else if (metadataType == IPConstants.RIGHTS) ip.AddRightsMetadata(metadata);
        }
        else
        {
            if (metadataType == IPConstants.PRESERVATION) representation.AddPreservationMetadata(metadata);
            else if (metadataType == IPConstants.TECHNICAL) representation.AddTechnicalMetadata(metadata);
            else if (metadataType == IPConstants.SOURCE) representation.AddSourceMetadata(metadata);
            else if (metadataType == IPConstants.RIGHTS) representation.AddRightsMetadata(metadata);
        }
    }

    /// <summary>
    /// Walks the <c>fileSec</c> file groups named <c>Schemas</c> or <c>Documentation</c> and attaches each
    /// validated file as a schema or documentation reference on the IP.
    /// </summary>
    /// <remarks>
    /// Uses the <c>fileGrp/@USE</c> attribute rather than walking via <c>fptr</c> IDREFs because the C# bindings
    /// expose <c>Fptr.Fileid</c> as a string, not the resolved <c>FileGrpType</c> object the Java JAXB binding
    /// returns. The result is semantically equivalent — every file group has a single representative use and
    /// the writer always assigns it.
    /// </remarks>
    public void ProcessSchemasAndDocumentation(MetsWrapper metsWrapper, IP.IP ip, string basePath)
    {
        if (metsWrapper?.Mets?.FileSec?.FileGrp == null) return;

        foreach (MetsTypeFileSecFileGrp fileGrp in metsWrapper.Mets.FileSec.FileGrp)
        {
            string? use = fileGrp.Use;
            if (string.IsNullOrEmpty(use)) continue;

            bool isSchemas = string.Equals(use, IPConstants.SCHEMAS_WITH_FIRST_LETTER_CAPITAL, StringComparison.OrdinalIgnoreCase);
            bool isDocumentation = string.Equals(use, IPConstants.DOCUMENTATION_WITH_FIRST_LETTER_CAPITAL, StringComparison.OrdinalIgnoreCase);
            if (!isSchemas && !isDocumentation) continue;

            string subfolder = isSchemas ? IPConstants.SCHEMAS : IPConstants.DOCUMENTATION;
            foreach (FileType file in fileGrp.File)
            {
                IPFile? ipFile = ResolveAndValidateFile(ip, file, basePath, subfolder);
                if (ipFile == null) continue;

                if (isSchemas) ip.AddSchema(ipFile);
                else ip.AddDocumentation(ipFile);
            }
        }
    }

    /// <summary>
    /// Iterates <see cref="MetsWrapper.MainDiv"/> for representation divs (label prefix <c>"Representations/"</c>),
    /// recursively parses each representation METS, validates its files, and attaches an <see cref="IPRepresentation"/>
    /// (with its descriptive/preservation/technical/source/rights/other metadata, schemas, documentation, and files)
    /// to the SIP.
    /// </summary>
    public void ProcessRepresentations(MetsWrapper mainWrapper, IP.IP ip)
    {
        if (mainWrapper?.MainDiv?.Div == null) return;
        ValidationReport report = ip.GetValidationReport();
        string? sipBasePath = ip.GetBasePath();
        if (sipBasePath == null) return;

        string prefix = IPConstants.REPRESENTATIONS_WITH_FIRST_LETTER_CAPITAL + IPConstants.METS_PATH_SEPARATOR;

        foreach (DivType div in mainWrapper.MainDiv.Div)
        {
            if (div.Label == null || !div.Label.StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (div.Mptr == null || div.Mptr.Count == 0) continue;

            DivTypeMptr mptr = div.Mptr[0];
            if (string.IsNullOrEmpty(mptr.Href))
            {
                report.AddError(ValidationConstants.REPRESENTATION_METS_MISSING,
                    "Representation div has no METS pointer href", div.Label);
                continue;
            }

            string href = METSUtils.DecodeHref(StripFilePrefix(mptr.Href));
            string representationMetsPath = Path.Combine(sipBasePath, NormalisePath(href));

            if (!File.Exists(representationMetsPath))
            {
                report.AddError(ValidationConstants.REPRESENTATION_METS_MISSING,
                    "Representation METS file not found", representationMetsPath);
                continue;
            }

            string representationId = div.Label.Substring(prefix.Length);
            IPRepresentation representation = new IPRepresentation(representationId);

            MetsWrapper representationWrapper = metsParser.ParseMets(representationMetsPath, report);
            if (representationWrapper.Mets == null)
            {
                report.AddError(ValidationConstants.REPRESENTATION_METS_INVALID,
                    "Representation METS could not be parsed", representationMetsPath);
                continue;
            }

            // Status comes from the representation's main structMap div TYPE (e.g. "ORIGINAL", "NORMALIZED").
            StructMapType? representationStructMap = metsParser.ExtractStructMap(representationWrapper, report, false);
            if (representationStructMap == null) continue;

            metsParser.PreProcessStructMap(representationWrapper, representationStructMap);

            if (representationWrapper.MainDiv?.Type != null)
            {
                representation.SetStatus(new RepresentationStatus(representationWrapper.MainDiv.Type));
            }

            // Representation-level content type
            if (!string.IsNullOrEmpty(representationWrapper.Mets.Othercontentinformationtype))
            {
                representation.ContentInformationType = new IPContentInformationType(representationWrapper.Mets.Othercontentinformationtype);
            }
            else if (representationWrapper.Mets.ContentinformationtypeSpecified)
            {
                representation.ContentInformationType = new IPContentInformationType(representationWrapper.Mets.Contentinformationtype);
            }

            ip.AddRepresentation(representation);

            string representationBasePath = Path.GetDirectoryName(representationMetsPath) ?? sipBasePath;

            // Agents on the representation METS header
            if (representationWrapper.Mets.MetsHdr?.Agent != null)
            {
                foreach (MetsTypeMetsHdrAgent agent in representationWrapper.Mets.MetsHdr.Agent)
                {
                    representation.AddAgent(metsParser.CreateIPAgent(agent));
                }
            }

            // Files (walked from the FileSec's "Data" file group on the representation METS)
            ProcessRepresentationFiles(representationWrapper, ip, representation, representationBasePath);

            // Metadata sections attached to the representation
            ProcessDescriptiveMetadata(representationWrapper, ip, representation, representationBasePath);
            ProcessOtherMetadata(representationWrapper, ip, representation, representationBasePath);
            ProcessAdministrativeMetadata(representationWrapper, ip, representation, representationBasePath);

            // Representation-level schemas and documentation
            ProcessSchemasAndDocumentation(representationWrapper, ip, representationBasePath);
        }
    }

    private void ProcessRepresentationFiles(MetsWrapper representationWrapper, IP.IP ip, IPRepresentation representation, string representationBasePath)
    {
        if (representationWrapper?.Mets?.FileSec?.FileGrp == null) return;

        foreach (MetsTypeFileSecFileGrp fileGrp in representationWrapper.Mets.FileSec.FileGrp)
        {
            string? use = fileGrp.Use;
            if (string.IsNullOrEmpty(use)) continue;

            // Skip schemas/documentation here — they're processed by ProcessSchemasAndDocumentation.
            if (string.Equals(use, IPConstants.SCHEMAS_WITH_FIRST_LETTER_CAPITAL, StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(use, IPConstants.DOCUMENTATION_WITH_FIRST_LETTER_CAPITAL, StringComparison.OrdinalIgnoreCase)) continue;

            // The writer puts representation payload files in the "Data" group (and shallow-mode subfolders use
            // "data/<subpath>/" prefixes). Either way, treat all remaining file groups as representation data.
            foreach (FileType file in fileGrp.File)
            {
                IPFile? ipFile = ResolveAndValidateFile(ip, file, representationBasePath, IPConstants.DATA);
                if (ipFile == null) continue;
                representation.AddFile(ipFile);
            }
        }
    }

    /// <summary>
    /// Restores ancestor identifiers from the secondary RODA structMap (if present).
    /// </summary>
    /// <remarks>
    /// The writer represents each ancestor as a <see cref="DivTypeMptr"/> on the <c>Ancestors</c> div
    /// (<see cref="EARKMETSCreator.GenerateAncestorStructMap"/>), with the ancestor identifier in <c>@xlink:href</c>.
    /// </remarks>
    public void ProcessAncestors(MetsWrapper mainWrapper, IP.IP ip)
    {
        if (mainWrapper?.Mets?.StructMap == null) return;

        foreach (StructMapType structMap in mainWrapper.Mets.StructMap)
        {
            if (!string.Equals(structMap.Label, IPConstants.EARK_SIP_STRUCTURAL_MAP, StringComparison.Ordinal)) continue;
            DivType? rodaDiv = structMap.Div;
            if (rodaDiv?.Div == null) continue;

            foreach (DivType childDiv in rodaDiv.Div)
            {
                if (!string.Equals(childDiv.Label, IPConstants.EARK_SIP_ANCESTORS_DIV_LABEL, StringComparison.Ordinal)) continue;
                if (childDiv.Mptr == null) continue;

                List<string> ancestors = new List<string>();
                foreach (DivTypeMptr ancestorMptr in childDiv.Mptr)
                {
                    if (!string.IsNullOrEmpty(ancestorMptr.Href))
                    {
                        ancestors.Add(METSUtils.DecodeHref(ancestorMptr.Href));
                    }
                }
                if (ancestors.Count > 0) ip.SetAncestors(ancestors);
            }
        }
    }

    /// <summary>
    /// Resolves and validates a payload <see cref="FileType"/> entry from a METS <c>fileSec/fileGrp</c>.
    /// </summary>
    private IPFile? ResolveAndValidateFile(IP.IP ip, FileType file, string basePath, string subfolder)
    {
        ValidationReport report = ip.GetValidationReport();

        if (file.FLocat == null || file.FLocat.Count == 0)
        {
            report.AddError(ValidationConstants.FILE_NOT_FOUND, "FileType has no FLocat", file.Id);
            return null;
        }

        FileTypeFLocat fLocat = file.FLocat[0];
        if (string.IsNullOrEmpty(fLocat.Href))
        {
            report.AddError(ValidationConstants.FILE_NOT_FOUND, "FLocat has no @xlink:href", file.Id);
            return null;
        }

        string href = METSUtils.DecodeHref(StripFilePrefix(fLocat.Href));
        string filePath = Path.Combine(basePath, NormalisePath(href));

        if (!File.Exists(filePath))
        {
            report.AddError(ValidationConstants.FILE_NOT_FOUND, "Referenced payload file not found", filePath);
            return null;
        }

        if (!VerifyChecksum(filePath, file.Checksum, file.Checksumtype, file.ChecksumtypeSpecified, report))
        {
            return null;
        }

        IPFile ipFile = new IPFile(filePath);
        if (!string.IsNullOrEmpty(file.Checksum))
        {
            ipFile.SetChecksum(file.Checksum, file.ChecksumtypeSpecified ? file.Checksumtype : (IFilecoreChecksumtype?)null);
        }
        ipFile.SetRelativeFolders(ComputeRelativeFolders(basePath, filePath, subfolder));
        return ipFile;
    }

    private static HashSet<string> CollectDmdIds(DivType? div)
    {
        if (div?.Dmdid == null) return new HashSet<string>();
        HashSet<string> result = new HashSet<string>();
        foreach (string id in div.Dmdid)
        {
            if (!string.IsNullOrEmpty(id)) result.Add(id);
        }
        return result;
    }

    /// <summary>
    /// Maps a METS <c>MdRef</c> to its declared metadata type, with graceful fallback for unknown types.
    /// </summary>
    private MetadataType BuildMetadataType(MdSecTypeMdRef mdRef, ValidationReport report, string? location)
    {
        try
        {
            string typeName = EnumUtils.GetXmlEnumName(mdRef.Mdtype);
            MetadataType dmdType = new MetadataType(typeName);
            if (!string.IsNullOrEmpty(mdRef.Othermdtype))
            {
                dmdType.SetOtherType(mdRef.Othermdtype);
            }
            return dmdType;
        }
        catch (Exception e) when (e is ArgumentException || e is InvalidOperationException || e is NullReferenceException)
        {
            report.AddWarning(
                ValidationConstants.MDREF_INCOMPLETE,
                "MdRef has unknown or missing MDTYPE; defaulting to OTHER",
                location,
                e.Message);
            return new MetadataType(IMetadataMdtype.OTHER);
        }
    }

    /// <summary>
    /// Resolves an <see cref="MdSecTypeMdRef"/> to a concrete on-disk file, verifies its checksum, and returns
    /// a populated <see cref="IPFile"/>. Returns <c>null</c> on any validation failure (after recording an
    /// ERROR entry on the IP's validation report).
    /// </summary>
    private IPFile? ResolveAndValidateMdRef(IP.IP ip, MdSecTypeMdRef mdRef, string basePath, string metadataType)
    {
        ValidationReport report = ip.GetValidationReport();

        if (string.IsNullOrEmpty(mdRef.Href))
        {
            report.AddError(ValidationConstants.MDREF_INCOMPLETE, "MdRef has no @xlink:href", mdRef.Id);
            return null;
        }

        string href = METSUtils.DecodeHref(StripFilePrefix(mdRef.Href));
        string filePath = Path.Combine(basePath, NormalisePath(href));

        if (!File.Exists(filePath))
        {
            report.AddError(ValidationConstants.FILE_NOT_FOUND, $"{metadataType} metadata file not found", filePath);
            return null;
        }

        if (!VerifyChecksum(filePath, mdRef.Checksum, mdRef.Checksumtype, mdRef.ChecksumtypeSpecified, report))
        {
            // Error already recorded by VerifyChecksum.
            return null;
        }

        IPFile ipFile = new IPFile(filePath);
        if (!string.IsNullOrEmpty(mdRef.Checksum))
        {
            ipFile.SetChecksum(mdRef.Checksum, mdRef.ChecksumtypeSpecified ? mdRef.Checksumtype : (IFilecoreChecksumtype?)null);
        }
        ipFile.SetRelativeFolders(ComputeRelativeFolders(basePath, filePath, IPConstants.METADATA, metadataType));
        return ipFile;
    }

    /// <summary>
    /// Recomputes the file's checksum using the algorithm declared in METS and compares against the declared
    /// checksum. Adds an ERROR entry on the report if anything fails. Returns true iff the file verifies.
    /// </summary>
    private bool VerifyChecksum(string filePath, string? metsChecksum, IFilecoreChecksumtype algorithm, bool algorithmSpecified, ValidationReport report)
    {
        if (string.IsNullOrEmpty(metsChecksum) || !algorithmSpecified)
        {
            report.AddError(ValidationConstants.CHECKSUM_NOT_DECLARED,
                "METS does not declare a checksum or algorithm for this reference",
                filePath);
            return false;
        }

        string algorithmName;
        try
        {
            algorithmName = EnumUtils.GetXmlEnumName(algorithm);
        }
        catch
        {
            report.AddError(ValidationConstants.UNSUPPORTED_CHECKSUM_ALGORITHM,
                $"Unsupported checksum algorithm: {algorithm}",
                filePath);
            return false;
        }

        try
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                string computed = ZIPUtils.CalculateChecksum(stream, algorithm);
                if (!string.Equals(computed, metsChecksum, StringComparison.OrdinalIgnoreCase))
                {
                    report.AddError(ValidationConstants.CHECKSUM_MISMATCH,
                        $"Checksum mismatch using {algorithmName}",
                        filePath,
                        $"expected={metsChecksum}, computed={computed}");
                    return false;
                }
            }
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is System.Security.Cryptography.CryptographicException)
        {
            report.AddError(ValidationConstants.METADATA_READ_FAILED,
                "Failed to read file for checksum verification",
                filePath,
                e.Message);
            return false;
        }
        catch (ArgumentNullException)
        {
            report.AddError(ValidationConstants.UNSUPPORTED_CHECKSUM_ALGORITHM,
                $"Algorithm not supported by the runtime: {algorithmName}",
                filePath);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Strips METS file-URI prefixes (e.g. <c>file://./</c>, <c>file:</c>) that the writer may have applied.
    /// </summary>
    private static string StripFilePrefix(string href)
    {
        foreach (string prefix in IPConstants.METS_FILE_PREFIXES_TO_ACCEPT)
        {
            if (href.StartsWith(prefix, StringComparison.Ordinal))
            {
                return href.Substring(prefix.Length);
            }
        }
        return href;
    }

    /// <summary>
    /// Converts a forward-slash METS path into an OS-native path.
    /// </summary>
    private static string NormalisePath(string metsPath)
    {
        return metsPath.Replace('/', Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Computes the relative folder list between <paramref name="basePath"/>'s well-known subfolder
    /// (e.g. <c>metadata/descriptive</c>) and the actual file path. This mirrors what the writer expects
    /// in <see cref="IPFile.SetRelativeFolders"/>.
    /// </summary>
    private static List<string> ComputeRelativeFolders(string basePath, string filePath, params string[] wellKnownSubfolders)
    {
        string anchor = basePath;
        foreach (string sub in wellKnownSubfolders)
        {
            anchor = Path.Combine(anchor, sub);
        }

        List<string> folders = new List<string>();
        string fileDir = Path.GetDirectoryName(filePath) ?? string.Empty;
        if (fileDir.StartsWith(anchor, StringComparison.OrdinalIgnoreCase))
        {
            string rest = fileDir.Substring(anchor.Length).TrimStart(Path.DirectorySeparatorChar, '/');
            if (!string.IsNullOrEmpty(rest))
            {
                foreach (string segment in rest.Split(Path.DirectorySeparatorChar, '/'))
                {
                    if (!string.IsNullOrEmpty(segment)) folders.Add(segment);
                }
            }
        }
        return folders;
    }
}
