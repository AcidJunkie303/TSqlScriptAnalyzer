namespace DatabaseAnalyzer.Core;

internal sealed class NullProgressWriter : IProgressCallback
{
    public void OnProgress(ProgressCallbackArgs args)
    {
    }
}
