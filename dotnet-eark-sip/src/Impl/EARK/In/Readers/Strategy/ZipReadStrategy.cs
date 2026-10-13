/// <summary>
/// <see cref="IReadStrategy"/> implementation that extracts a ZIP-packaged SIP.
/// </summary>
/// <remarks>
/// Symmetric counterpart to <see cref="ZipWriteStrategy"/>. Delegates extraction to
/// <see cref="ZIPUtils.ExtractIPIfInZipFormat(string, string)"/>, which also handles the case where the SIP
/// content is wrapped in a single <c>{sipId}/</c> folder inside the archive.
/// </remarks>
public class ZipReadStrategy : IReadStrategy
{
    /// <inheritdoc />
    public string DestinationPath { get; private set; } = string.Empty;

    /// <inheritdoc />
    public void Setup(string destinationPath)
    {
        DestinationPath = destinationPath;
    }

    /// <inheritdoc />
    public string Read(string source)
    {
        if (string.IsNullOrEmpty(DestinationPath))
        {
            throw new IPException("ZipReadStrategy must be configured via Setup before Read is called.");
        }
        return ZIPUtils.ExtractIPIfInZipFormat(source, DestinationPath);
    }
}
