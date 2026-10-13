/// <summary>
/// Defines a strategy for resolving a SIP source (e.g. ZIP file) into a directory that can be parsed.
/// </summary>
/// <remarks>
/// Mirrors <see cref="IWriteStrategy"/>: where the write side abstracts how a SIP gets persisted, the read
/// side abstracts how a SIP gets materialised on disk before its METS files are parsed.
/// </remarks>
public interface IReadStrategy
{
    /// <summary>
    /// Gets the destination path where extracted SIP contents live after <see cref="Read"/> has been called.
    /// </summary>
    string DestinationPath { get; }

    /// <summary>
    /// Sets up the read strategy with the specified destination path (where the SIP will be extracted to).
    /// </summary>
    /// <param name="destinationPath">The directory where the SIP will be extracted.</param>
    void Setup(string destinationPath);

    /// <summary>
    /// Materialises the SIP from <paramref name="source"/> into the configured destination directory and
    /// returns the path to the SIP root — the directory that directly contains <c>METS.xml</c>.
    /// </summary>
    /// <param name="source">The SIP source path (e.g. a .zip file).</param>
    /// <returns>The absolute path to the SIP root.</returns>
    string Read(string source);
}
