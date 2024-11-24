namespace DatabaseAnalyzer.Core;

public interface IProgressCallback
{
    void OnProgress(ProgressCallbackArgs args);
}

internal sealed class NullProgressWriter : IProgressCallback
{
    public void OnProgress(ProgressCallbackArgs args)
    {
    }
}
