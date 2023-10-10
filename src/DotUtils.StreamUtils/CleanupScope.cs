namespace DotUtils.StreamUtils;

internal readonly struct CleanupScope : IDisposable
{
    private readonly Action _disposeAction;

    public CleanupScope(Action disposeAction) => _disposeAction = disposeAction;

    public void Dispose() => _disposeAction();
}
