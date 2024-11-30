using JustyBase.PluginDatabaseBase.AnotherContracts;
using JustyBase.StringExtensions;
using System.Buffers;
using System.Text.RegularExpressions;

namespace JustyBase.Tools.Helpers;

public sealed class SearchInFiles : ISearchInFiles
{
    const int BUFFER_SIZE = 65_536;

    public bool IsWordInFile(string path, string toSearch, bool searchInSqlComments)
    {
        int _toSearchLen = toSearch.Length;
        using var fs = new StreamReader(path);

        char[] borrowed = ArrayPool<char>.Shared.Rent(BUFFER_SIZE);
        Span<char> buffer = borrowed.AsSpan();

        int readed = BUFFER_SIZE;
        bool firstTime = true;
        bool isInFile = false;
        while (readed > 0)
        {
            int r = 0;
            if (firstTime)
            {
                readed = fs.Read(buffer);
                firstTime = false;
                r = new ReadOnlySpan<char>(borrowed,0,readed).IndexOf(toSearch, StringComparison.OrdinalIgnoreCase);
                if (r != -1)
                {
                    ArrayPool<char>.Shared.Return(borrowed);
                    isInFile = ((int)fs.BaseStream.Position - readed + r /*+ bomAdd*/) >=0;
                    break;
                }
            }
            else
            {
                buffer.Slice(BUFFER_SIZE - _toSearchLen).CopyTo(buffer.Slice(0, _toSearchLen)); 
                readed = fs.Read(buffer.Slice(_toSearchLen));
                r = new ReadOnlySpan<char>(borrowed, 0, readed + _toSearchLen).IndexOf(toSearch, StringComparison.OrdinalIgnoreCase);
                if (r != -1)
                {
                    ArrayPool<char>.Shared.Return(borrowed);
                    isInFile = ((int)fs.BaseStream.Position - readed + r - _toSearchLen /*+ bomAdd*/)>=0;
                    break;
                }
            }
        }
        ArrayPool<char>.Shared.Return(borrowed);

        if (!searchInSqlComments && isInFile && Path.GetExtension(path).Equals(".sql", StringComparison.OrdinalIgnoreCase))
        {
            var txt = File.ReadAllText(path);
            txt = txt.CreateCleanSql();
            isInFile = txt.Contains(toSearch, StringComparison.OrdinalIgnoreCase);
        }

        return isInFile;
    }

    public bool IsWholeWordInFile(string path, string toSearch, bool searchInSqlComments)
    {
        var searchRegex = new Regex($@"(\b|_){Regex.Escape(toSearch)}(\b|_)", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(3));
        bool isInFile = false;
        using (var sr = new StreamReader(path))
        {
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (line is not null && searchRegex.IsMatch(line))
                {
                    isInFile = true;
                    break;
                }
            }
        }

        if (!searchInSqlComments && isInFile && Path.GetExtension(path).Equals(".sql", StringComparison.OrdinalIgnoreCase)) 
        { 
            var txt = File.ReadAllText(path);
            txt = txt.CreateCleanSql();
            isInFile = searchRegex.IsMatch(txt);
        }

        return isInFile;
    }
}


