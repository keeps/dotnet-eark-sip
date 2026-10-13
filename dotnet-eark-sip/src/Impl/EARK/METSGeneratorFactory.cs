/// <summary>
/// Factory class for creating METS generator instances.
/// </summary>
public class METSGeneratorFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="METSGeneratorFactory"/> class.
    /// </summary>
    public METSGeneratorFactory() { }

    /// <summary>
    /// Gets the appropriate METS generator instance based on the specified version.
    /// </summary>
    /// <param name="version">The version of the METS generator to create.</param>
    /// <returns>An instance of <see cref="EARKMETSCreator"/> for the specified version.</returns>
    public EARKMETSCreator GetGenerator(string version)
    {
        if (version == "2.0.4") return new EARKMETSCreator204();
        return new EARKMETSCreator210();
    }

    /// <summary>
    /// Gets the appropriate METS parser instance based on the specified version.
    /// </summary>
    /// <param name="version">The version of the METS parser to create (e.g. <c>"2.0.4"</c>, <c>"2.1.0"</c>).</param>
    /// <returns>An instance of <see cref="EARKMETSParser"/> for the specified version. Defaults to 2.1.0 when the version is unknown.</returns>
    /// <remarks>
    /// Symmetric counterpart to <see cref="GetGenerator"/>. Same version-routing rule, applied to the read side.
    /// </remarks>
    public EARKMETSParser GetParser(string version)
    {
        if (version == "2.0.4") return new EARKMETSParser204();
        return new EARKMETSParser210();
    }
}