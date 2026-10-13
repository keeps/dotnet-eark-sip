/// <summary>
/// Factory base for creating <see cref="IReadStrategy"/> instances.
/// </summary>
/// <remarks>Mirrors <see cref="WriteStrategyFactory"/> on the write side.</remarks>
public abstract class ReadStrategyFactory
{
    /// <summary>
    /// Creates a read strategy and configures it with the specified extraction directory.
    /// </summary>
    /// <param name="extractionPath">The directory where the SIP will be extracted.</param>
    /// <returns>A configured <see cref="IReadStrategy"/>.</returns>
    public IReadStrategy Create(string extractionPath)
    {
        IReadStrategy strategy = CreateReadStrategy();
        strategy.Setup(extractionPath);
        return strategy;
    }

    /// <summary>
    /// Creates a fresh <see cref="IReadStrategy"/>.
    /// </summary>
    protected abstract IReadStrategy CreateReadStrategy();
}
