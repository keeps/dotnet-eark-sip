/// <summary>
/// Factory for <see cref="ZipReadStrategy"/>.
/// </summary>
public class ZipReadStrategyFactory : ReadStrategyFactory
{
    /// <inheritdoc />
    protected override IReadStrategy CreateReadStrategy()
    {
        return new ZipReadStrategy();
    }
}
