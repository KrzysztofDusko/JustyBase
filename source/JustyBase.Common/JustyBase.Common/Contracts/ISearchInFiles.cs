namespace JustyBase.Common.Contracts;

public interface ISearchInFiles
{
    bool IsWordInFile(string path, string toSearch, bool searchInSqlComments);
    bool IsWholeWordInFile(string path, string toSearch, bool searchInSqlComments);
}
