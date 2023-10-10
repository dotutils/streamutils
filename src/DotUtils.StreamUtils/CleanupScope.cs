namespace DotUtils.StreamUtils;

internal class CleanupScope : IDisposable
{
    private readonly Action _disposeAction;

    public CleanupScope(Action disposeAction) => _disposeAction = disposeAction;

    public void Dispose() => _disposeAction();
}
