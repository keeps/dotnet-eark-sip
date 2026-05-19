/// <summary>
/// E-ARK METS parser for spec version 2.0.4.
/// </summary>
/// <remarks>
/// Read-side counterpart to <see cref="EARKMETSCreator204"/>. For the structural elements this library cares
/// about (header, dmdSec, amdSec, fileSec, structMap, representations), the 2.0.4 and 2.1.0 layouts are
/// equivalent, so this class currently inherits the base implementation without override. It exists so the
/// factory can route by spec version and so version-specific divergences can be added here without touching
/// the 2.1.0 path.
/// </remarks>
public class EARKMETSParser204 : EARKMETSParser
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EARKMETSParser204"/> class.
    /// </summary>
    public EARKMETSParser204() { }
}
