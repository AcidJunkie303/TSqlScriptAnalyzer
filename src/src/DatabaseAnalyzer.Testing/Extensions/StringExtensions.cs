namespace DatabaseAnalyzer.Testing.Extensions;

public static class StringExtensions
{
    public static (int LineIndex, int ColumnIndex) GetLineAndColumnIndex(this string text, int index)
    {
        var lineIndex = 0;
        var columnIndex = 0;
        for (var i = 0; i < index; i++)
        {
            if (text[i] == '\n')
            {
                lineIndex++;
                columnIndex = 0;
            }
            else
            {
                columnIndex++;
            }
        }

        return (lineIndex, columnIndex);
    }

    public static (int LineNumber, int ColumnNumber) GetLineAndColumnNumber(this string text, int index)
    {
        var (lineIndex, columnIndex) = text.GetLineAndColumnIndex(index);
        return (lineIndex + 1, columnIndex + 1);
    }
}
