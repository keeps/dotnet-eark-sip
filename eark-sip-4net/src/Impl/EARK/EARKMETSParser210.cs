/// <summary>
/// E-ARK METS parser for spec version 2.1.0.
/// </summary>
/// <remarks>
/// Read-side counterpart to <see cref="EARKMETSCreator210"/>. The 2.1.0 layout is fully handled by
/// <see cref="EARKMETSParser"/>'s base implementation, so this class is currently a marker that allows the
/// factory to select a version-specific parser.
/// </remarks>
public class EARKMETSParser210 : EARKMETSParser
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EARKMETSParser210"/> class.
    /// </summary>
    public EARKMETSParser210() { }
}
