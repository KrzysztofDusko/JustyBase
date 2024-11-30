namespace JustyBase.Editor;

public interface ICompletionDataEx : ICompletionData
{
    bool IsSelected { get; }

    string SortText { get; }
}

