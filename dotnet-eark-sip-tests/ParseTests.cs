using IP;
using IPEnums;
using Mets;
using System.IO.Compression;
using Xml.Mets.CsipExtensionMets;

namespace dotnet_eark_sip_tests;

/// <summary>
/// Targeted parse-side tests: version detection, factory routing, and negative paths (bad checksum, missing
/// file, malformed METS, unsupported algorithm).
/// </summary>
[Collection("Non-Parallel Tests")]
public class ParseTests : IDisposable
{
    private readonly string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "parse_tests");
    private readonly string resourcesPath = "Resources" + Path.DirectorySeparatorChar + "EARK";

    public ParseTests()
    {
        if (Directory.Exists(outputPath))
        {
            try { Directory.Delete(outputPath, recursive: true); } catch { /* best-effort */ }
        }
        Directory.CreateDirectory(outputPath);
    }

    public void Dispose()
    {
        try { Directory.Delete(outputPath, recursive: true); } catch { /* best-effort */ }
    }

    // ---- Factory routing ----

    [Fact]
    public void GetParser_RoutesByVersion()
    {
        METSGeneratorFactory factory = new METSGeneratorFactory();
        Assert.IsType<EARKMETSParser204>(factory.GetParser("2.0.4"));
        Assert.IsType<EARKMETSParser210>(factory.GetParser("2.1.0"));
        // Unknown version falls back to the 2.1.0 parser.
        Assert.IsType<EARKMETSParser210>(factory.GetParser("9.9.9"));
    }

    [Fact]
    public void DetectMETSVersion_ReadsProfileAttribute()
    {
        string fixturePath = Path.Combine(outputPath, "version-probe.xml");
        File.WriteAllText(fixturePath,
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<mets xmlns=\"http://www.loc.gov/METS/\" PROFILE=\"https://earksip.dilcis.eu/profile/E-ARK-SIP-v2-0-4.xml\" />");

        Assert.Equal("2.0.4", METSUtils.DetectMETSVersion(fixturePath));
    }

    [Fact]
    public void DetectMETSVersion_FallsBackToDefaultWithInfoEntry()
    {
        string fixturePath = Path.Combine(outputPath, "unknown-profile.xml");
        File.WriteAllText(fixturePath,
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<mets xmlns=\"http://www.loc.gov/METS/\" PROFILE=\"https://example.com/some-other-profile.xml\" />");

        ValidationReport report = new ValidationReport();
        string version = METSUtils.DetectMETSVersion(fixturePath, report);

        Assert.Equal("2.1.0", version);
        Assert.Contains(report.GetEntries(ValidationEntryLevel.INFO),
            e => e.Id == ValidationConstants.METS_UNKNOWN_PROFILE);
    }

    // ---- Negative paths: parse should record errors, not throw ----

    [Fact]
    public void Parse_CorruptedFileContent_RecordsChecksumMismatch()
    {
        // Build a valid SIP, then corrupt one of the metadata files inside the ZIP.
        string zipPath = BuildMinimalSip("RT_CHECKSUM_1");
        CorruptFileInZip(zipPath, "RT_CHECKSUM_1/metadata/descriptive/metadata_descriptive_dc.xml");

        string extractDir = Path.Combine(outputPath, "extracted-checksum");
        SIP parsed = EARKSIP.Parse(zipPath, extractDir);

        Assert.False(parsed.IsValid());
        Assert.Contains(
            parsed.GetValidationReport().GetEntries(ValidationEntryLevel.ERROR),
            e => e.Id == ValidationConstants.CHECKSUM_MISMATCH);
    }

    [Fact]
    public void Parse_MissingReferencedFile_RecordsFileNotFound()
    {
        string zipPath = BuildMinimalSip("RT_MISSING_1");
        RemoveFileFromZip(zipPath, "RT_MISSING_1/metadata/descriptive/metadata_descriptive_dc.xml");

        string extractDir = Path.Combine(outputPath, "extracted-missing");
        SIP parsed = EARKSIP.Parse(zipPath, extractDir);

        Assert.False(parsed.IsValid());
        Assert.Contains(
            parsed.GetValidationReport().GetEntries(ValidationEntryLevel.ERROR),
            e => e.Id == ValidationConstants.FILE_NOT_FOUND);
    }

    [Fact]
    public void Parse_MalformedMETS_RecordsUnmarshalFailed()
    {
        // Hand-craft a tiny SIP where METS.xml is malformed.
        string sipDir = Path.Combine(outputPath, "malformed-sip", "RT_MALFORMED_1");
        Directory.CreateDirectory(sipDir);
        File.WriteAllText(Path.Combine(sipDir, "METS.xml"), "<not-valid-mets><unclosed>");

        string zipPath = Path.Combine(outputPath, "malformed.zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(Path.Combine(outputPath, "malformed-sip"), zipPath);

        string extractDir = Path.Combine(outputPath, "extracted-malformed");
        SIP parsed = EARKSIP.Parse(zipPath, extractDir);

        Assert.False(parsed.IsValid());
        Assert.Contains(
            parsed.GetValidationReport().GetEntries(ValidationEntryLevel.ERROR),
            e => e.Id == ValidationConstants.METS_UNMARSHAL_FAILED);
    }

    // ---- Helpers ----

    private string BuildMinimalSip(string id)
    {
        string currentPath = Directory.GetCurrentDirectory();
        SIP built = new EARKSIP(id, IPContentType.GetMIXED(), IPContentInformationType.GetMIXED(), "2.1.0");
        built.AddSubmitterAgent("dotnet-eark-sip-parse-tests");
        built.AddDescriptiveMetadata(new IPDescriptiveMetadata(
            new IPFile(Path.Combine(currentPath, resourcesPath, "metadata_descriptive_dc.xml")),
            new MetadataType(IMetadataMdtype.DC), null));

        IWriteStrategy writeStrategy = new ZipWriteStrategyFactory().Create(outputPath);
        return built.Build(writeStrategy);
    }

    /// <summary>Rewrites the given ZIP entry with junk bytes, breaking its checksum.</summary>
    private static void CorruptFileInZip(string zipPath, string entryFullName)
    {
        using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
        {
            ZipArchiveEntry? entry = archive.GetEntry(entryFullName);
            Assert.NotNull(entry);
            using (Stream stream = entry!.Open())
            {
                stream.SetLength(0);
                byte[] junk = System.Text.Encoding.UTF8.GetBytes("CORRUPTED CONTENT");
                stream.Write(junk, 0, junk.Length);
            }
        }
    }

    /// <summary>Deletes the given ZIP entry, so the parser will report it as missing.</summary>
    private static void RemoveFileFromZip(string zipPath, string entryFullName)
    {
        using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
        {
            ZipArchiveEntry? entry = archive.GetEntry(entryFullName);
            Assert.NotNull(entry);
            entry!.Delete();
        }
    }
}
