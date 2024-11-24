namespace DatabaseAnalyzer.Core.Extensions;

internal static class ProgressCallbackExtensions
{
    public static IDisposable OnProgressWithAutoEndActionNotification(this IProgressCallback callback, string messageTemplate, params string[] insertionStrings)
    {
        callback.OnProgress(new ProgressCallbackArgs(IsBeginOfAction: true, messageTemplate, insertionStrings));
        return new EndOfActionNotifier(callback, messageTemplate, insertionStrings);
    }

    private sealed class EndOfActionNotifier : IDisposable
    {
        private readonly IProgressCallback _callback;
        private readonly string[] _insertionStrings;
        private readonly string _messageTemplate;
        private bool _disposed;

        public EndOfActionNotifier(IProgressCallback callback, string messageTemplate, string[] insertionStrings)
        {
            _callback = callback;
            _messageTemplate = messageTemplate;
            _insertionStrings = insertionStrings;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _callback.OnProgress(new ProgressCallbackArgs(IsBeginOfAction: false, _messageTemplate, _insertionStrings));
        }
    }
}
