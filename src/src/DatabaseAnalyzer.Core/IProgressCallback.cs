namespace DatabaseAnalyzer.Core;

public interface IProgressCallback
{
    void OnProgress(ProgressCallbackArgs args);
}
