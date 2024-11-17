using JustyBase.PluginDatabaseBase.AnotherContracts;
using System.Buffers;
using System.Text.RegularExpressions;

namespace JustyBase.Tools.Helpers;

public sealed class SearchInFiles : ISearchInFiles
{
    const int BUFFER_SIZE = 65_536;

    public bool IsWordInFile(string path, string toSearch)
    {
        int _toSearchLen = toSearch.Length;
        using var fs = new StreamReader(path);

        char[] borrowed = ArrayPool<char>.Shared.Rent(BUFFER_SIZE);
        Span<char> buffer = borrowed.AsSpan();

        int readed = BUFFER_SIZE;
        bool firstTime = true;
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
                    return ((int)fs.BaseStream.Position - readed + r /*+ bomAdd*/) >=0;
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
                    return ((int)fs.BaseStream.Position - readed + r - _toSearchLen /*+ bomAdd*/)>=0;
                }
            }
        }
        ArrayPool<char>.Shared.Return(borrowed);
        return false;
    }

    public bool IsWholeWordInFile(string path, string toSearch)
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
        return isInFile;
    }
}


