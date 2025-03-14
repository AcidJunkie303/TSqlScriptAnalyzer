namespace DatabaseAnalyzer.Common.Various;

public static class DisposeActionFactory
{
    public static IDisposable Create(Action action) => new Disposer(action);

    private sealed class Disposer : IDisposable
    {
        private readonly Action _action;
        private bool _disposed;

        public Disposer(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _action();
            _disposed = true;
        }
    }
}
