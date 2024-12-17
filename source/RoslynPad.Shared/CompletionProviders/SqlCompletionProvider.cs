using JustyBase.Helpers;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommons;


//using JustyBase.PluginDatabaseBase.Extensions;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JustyBase.Editor.CompletionProviders;

public partial class SqlCompletionProvider : ICodeEditorCompletionProvider
{
    private readonly ISqlAutocompleteData _sqlAutocompleteData;
    private readonly SnippetInfoService _snippetService;
    private readonly SqlCodeEditor _sqlCodeEditor;
    private readonly ISomeEditorOptions? _someEditorOptions;
    public SqlCompletionProvider(SqlCodeEditor sqlCodeEditor, ISqlAutocompleteData sqlAutocompleteData, ISomeEditorOptions snippetsProvider)
    {
        _someEditorOptions = snippetsProvider;
        _snippetService = new SnippetInfoService(_someEditorOptions);
        _sqlAutocompleteData = sqlAutocompleteData;
        _sqlCodeEditor = sqlCodeEditor;
    }

    [GeneratedRegex(@"\(((?!(select|\(|\)|;)).)*\)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex rxBracketsWithtNoSelectGen();
    private static readonly Regex rxBracketsWithtNoSelectInside = rxBracketsWithtNoSelectGen();


    [GeneratedRegex(@"^\(.*\)(\s)+\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex rxSubQueryGen();
    private static readonly Regex rxSubQuery = rxSubQueryGen();


    [GeneratedRegex(@"\(.*\)(\s)+(?<alias>\w+)", RegexOptions.Compiled  | RegexOptions.Singleline)]
    private static partial Regex rxAliasGen();
    private static readonly Regex rxAlias = rxAliasGen();

    [GeneratedRegex(@"^\(.*\)", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex rxQueryGen();
    private static readonly Regex rxQuerty = rxQueryGen();


    [GeneratedRegex(@"\b(with\s+)?(?<aliasDatabaseTable>\w+?)\b\s*as\b\s*\({0,1}", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex rxWithGen();
    private static readonly Regex rxWith = rxWithGen();

    [GeneratedRegex(@"\b(create\s+temp\s+table|create\s+table)\s+(?<aliasDatabaseTable>\w+?)\b\s*as\b\s*\({0,1}", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex rxTableGen();
    private static readonly Regex rxTable = rxTableGen();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex rxToMuchSpacesGen();
    private static readonly Regex rxToMuchSpaces = rxToMuchSpacesGen();

    [GeneratedRegex(@"\(\d+\)", RegexOptions.Compiled)]
    private static partial Regex rxBracketsWithNumbersOnlyGen();
    private static readonly Regex rxBracketsWithNumbersOnly = rxBracketsWithNumbersOnlyGen();

    //FIX THIS
    private string _cleanSqlText = "";

    private readonly Dictionary<string, List<string>> _additionalTableWith = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _additionalTableTables = new(StringComparer.OrdinalIgnoreCase);

    private static readonly ICompletionDataEx[] _encondingList =
    [
        new CompletionDataSql("ASCII", "An encoding for the ASCII (7-bit) character set.", false, Glyph.None, null),
        new CompletionDataSql("UTF8", "An encoding for the UTF-8 format.", false, Glyph.None, null),
        new CompletionDataSql("UTF8_BM", "An encoding for the UTF-8 format - without BOM", false, Glyph.None, null),
        new CompletionDataSql("UTF16", "An encoding object for the UTF-16 format", false, Glyph.None, null),
        new CompletionDataSql("UTF32", "An encoding object for the UTF-32 format using the little endian byte order.", false, Glyph.None, null),
        new CompletionDataSql("Unicode", "An encoding for the UTF-16 format using the little endian byte order.", false, Glyph.None, null),
        new CompletionDataSql("BigEndianUnicode", "An encoding object for the UTF-16 format that uses the big endian byte order.", false, Glyph.None, null),
        new CompletionDataSql("Latin1", "An encoding for the Latin1 character set (ISO-8859-1).", false, Glyph.None, null),
        new CompletionDataSql("Default", "Default encoding for this .NET implementation", false, Glyph.None, null)
    ];

    private static ICompletionDataEx[]? _encondingList2;

    private static readonly ICompletionDataEx[] _rowDelimiterList =
    [
        new CompletionDataSql("windows", @"\r\n as row delimiter", false, Glyph.None, null),
        new CompletionDataSql("unix", @"\n as row delimiter", false, Glyph.None, null)
    ];

    private static readonly ICompletionDataEx[] _headerList =
    [
        new CompletionDataSql("true", "use header", false, Glyph.None, null),
        new CompletionDataSql("false", "do not use header", false, Glyph.None, null)
    ];

    private static readonly ICompletionDataEx[] _columnDelimiter =
    [
        new CompletionDataSql("';'", "';' as column delimiter", false, Glyph.None, null),
        new CompletionDataSql("|", "'|' as column delimiter", false, Glyph.None, null),
        new CompletionDataSql(",", "',' as column delimiter", false, Glyph.None, null),
        new CompletionDataSql("#", "'#' as column delimiter", false, Glyph.None, null)
    ];

    private static readonly ICompletionDataEx[] _compressionList =
    [
        new CompletionDataSql("none", "no compression", false, Glyph.None, null),
        new CompletionDataSql("zip", "zip compression", false, Glyph.None, null),
        new CompletionDataSql("gzip", "gzip compression", false, Glyph.None, null),
        new CompletionDataSql("brotli", "brotli compression", false, Glyph.None, null),
        new CompletionDataSql("zstd", "zstd compression", false, Glyph.None, null),
        new CompletionDataSql("lz4", "lz4 compression", false, Glyph.None, null)
    ];

    private static readonly ICompletionDataEx[] _upFrontRowsCountList =
    [
        new CompletionDataSql("true", "determine rows count before export ON", false, Glyph.None, null),
        new CompletionDataSql("false","determine rows count before export OFF", false, Glyph.None, null)
    ];

    private readonly Stopwatch _stTempTableWith = Stopwatch.StartNew();
    private const string FAST_SNIPET_TXT = "fast"; //TODO
    public async Task<CompletionResult> GetCompletionData(int position, char? triggerChar)
    {
        List<ICompletionDataEx> completionData;
        if (triggerChar == '\n')
        {
            completionData = [];
        }
        else
        {
            string? lastWord = EditorHelpers.GetLastWord(_sqlCodeEditor,position);
            if (string.IsNullOrWhiteSpace(lastWord))
            {
                return new CompletionResult(Array.Empty<ICompletionDataEx>(), null, true);
            }
            else if (lastWord.Equals("#encoding ", StringComparison.OrdinalIgnoreCase))
            {
                if (_encondingList2 is null)
                {
                    var enc = System.Text.Encoding.GetEncodings();
                    _encondingList2 = new ICompletionDataEx[enc.Length + _encondingList.Length];

                    for (int i = 0; i < _encondingList.Length; i++)
                    {
                        _encondingList2[i] = _encondingList[i];
                    }
                    for (int i = _encondingList.Length; i < enc.Length + _encondingList.Length; i++)
                    {
                        var currentEnc = enc[i - _encondingList.Length];
                        _encondingList2[i] = new CompletionDataSql(currentEnc.Name, currentEnc.DisplayName, false, Glyph.None, null);
                    }
                }

                return new CompletionResult(_encondingList2, null, true);
            }
            else if (lastWord.Equals("#LineDelimiter ",StringComparison.OrdinalIgnoreCase))
            {
                return new CompletionResult(_rowDelimiterList, null, true);
            }
            else if (lastWord.Equals("#header ", StringComparison.OrdinalIgnoreCase))
            {
                return new CompletionResult(_headerList, null, true);
            }
            else if (lastWord.Equals("#delimiter ", StringComparison.OrdinalIgnoreCase))
            {
                return new CompletionResult(_columnDelimiter, null, true);
            }
            else if (lastWord.Equals("#compression ", StringComparison.OrdinalIgnoreCase))
            {
                return new CompletionResult(_compressionList, null, true);
            }
            else if (lastWord.Equals("#upFrontRowsCount ", StringComparison.OrdinalIgnoreCase))
            {
                return new CompletionResult(_upFrontRowsCountList, null, true);
            }

            completionData = [];

            _cleanSqlText = _sqlCodeEditor.CleanSqlCode;

            var betweenBrackets = BetweenBracketOrSemicolon(position);
            
            while (!betweenBrackets.text.Contains("select", StringComparison.OrdinalIgnoreCase)
                && betweenBrackets.start > 0 && _cleanSqlText[betweenBrackets.start - 1] == '('
                )
            {
                betweenBrackets = BetweenBracketOrSemicolon(/*position -*/ betweenBrackets.start - 1);
            }

            if (!betweenBrackets.text.Contains("select", StringComparison.OrdinalIgnoreCase))
            {
                completionData.Clear();
            }

            if (_cleanSqlText.Length < 5_000)
            {
                MakeCteTask(_additionalTableWith, position);
                MakeTempTableHintsTask(_additionalTableTables, position);
            }
            else if (_stTempTableWith.IsRunning && _stTempTableWith.ElapsedMilliseconds > 500)
            {
                _stTempTableWith.Reset();
                var t1 = Task.Run(() => MakeCteTask(_additionalTableWith, position));
                var t2 = Task.Run(() => MakeTempTableHintsTask(_additionalTableTables, position));
                await t1;
                await t2;
                _stTempTableWith.Start();
            }

            string betweenBracketsTextX = betweenBrackets.text;
            int select2Index = LastSelect2(ref betweenBracketsTextX);
            bool ignoreBracketMismatch = false;
            if (select2Index != -1)
            {
                ignoreBracketMismatch = CanIgnoreBracketMismatch(betweenBracketsTextX, select2Index, ignoreBracketMismatch);
            }

            string baseSQL = TextPartAfterFrom(betweenBrackets.text, ignoreBracketMismatch);
            var variablesDictionary = _someEditorOptions?.VariablesDictStatic;

            if (variablesDictionary is not null)
            {
                foreach (var item in variablesDictionary)
                {
                    if (item.Key.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase))
                    {
                        completionData.Add(new CompletionDataSql(item.Key[1..], $"value: {item.Value}", false, Glyph.None, null));
                    }
                }
            }
            var snippets = _someEditorOptions?.GetAllSnippets;
            if (snippets is not null)
            {
                foreach (var (snippetName, snippetValue) in snippets)
                {
                    if (snippetValue.snippetType != FAST_SNIPET_TXT && snippetName.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase))
                    {
                        completionData.Add(new CompletionDataSql(snippetName, snippetValue.Description, false, Glyph.Snippet, _snippetService.SnippetManager));
                    }
                }
            }

            betweenBrackets = (rxBracketsWithtNoSelectInside.Replace(betweenBrackets.text, ""), betweenBrackets.start, betweenBrackets.end);

            List<string> chunks = baseSQL.AsSpan().MySplit2().ToList<string>().Select(arg => arg.Trim()).ToList<string>();
            Dictionary<string,List<string>> subqueriesDict = new (StringComparer.OrdinalIgnoreCase);
            List<int> toRemove = [];//indexes of subquery

            //subqueries
            for (int i = 0; i < chunks.Count; i++)
            {
                string currentChank = chunks[i];
                //first inner the widest subquery -> ((aaaaa)bbb) cc (xxxxxxxxxxxx) winds ((aaaaa)bbb) cc
                Match currentSubquery = rxSubQuery.Match(currentChank);
                if (!currentSubquery.Success)
                {
                    continue;
                }
                toRemove.Add(i);//remove to skip this in standard mode

                //https://github.com/KrzysztofDusko/JustyBase/issues/270
                string subqueryWithoutCasting = rxBracketsWithNumbersOnly.Replace(currentSubquery.Value, "");

                string aliasX = rxAlias.Match(subqueryWithoutCasting).Groups["alias"].Value;//last word = alias
                string qry = rxQuerty.Match(subqueryWithoutCasting).Value;

                qry = qry[1..^1].Trim(); // (BCD) -> BCD
                int selectIndex = LastSelect2(ref qry);
                if (selectIndex == -1)
                {
                    continue;
                }

                string afterSelect = qry.Substring(selectIndex + "select".Length).Trim();
                int nr = FirstFrom2(afterSelect);//first not nested from (after select)
                if (nr == -1)
                {
                    nr = afterSelect.Length - 1;
                }

                if (nr > 0)
                {
                    string between = afterSelect.Substring(0, nr + 1).Trim();
                    subqueriesDict[aliasX] = between.AsSpan().MySplit2().Select(arg => LastWord(arg)).ToList();
                }
            }

            int l = 0;
            foreach (var i in toRemove)
            {
                chunks.RemoveAt(i - l);
                l++;
            }
            Dictionary<string, List<string>> aliasDatabaseTable = ExtractElements(chunks);

            if (!int.TryParse(lastWord,out _))
            {
                IAsyncEnumerable<CompletionDataSql> words = _sqlAutocompleteData.GetWordsList(lastWord, aliasDatabaseTable, subqueriesDict, _additionalTableWith, _additionalTableTables
                    //, betweenSelectAndFrom
                    );

                await foreach (var objectName in words)
                {
                    //var p = new CompletionDataSql(tableName, "desc", false, Glyph.Table, _snippetService.SnippetManager);
                    //completionData.Add(p);
                    completionData.Add(objectName);
                }
            }
        }
        return new CompletionResult(completionData, null,true);

        static bool CanIgnoreBracketMismatch(ReadOnlySpan<char> qry2, int selectIndex2, bool ignoreBracketMismatch)
        {
            ReadOnlySpan<char> afterSelect = qry2[(selectIndex2 + "select".Length)..].Trim();
            int index = FirstFrom2(afterSelect);//first not nested from (after select)
            if (index == -1 && afterSelect.Length < 1024) // (#48, for short SQLs we should accept no brackets balance, becouse ... [TODO sample])
            {
                index = FirstFromIgnoreBracketMismatch(afterSelect);
                if (index > 0)
                {
                    ignoreBracketMismatch = true;
                }
            }

            return ignoreBracketMismatch;
        }
    }

    public static string LastWord(string arg)  // COL1 AS C1 -> C1
    {
        string result = arg.Trim();
        result = result.Substring(LastDotSpaceOrNewLine(result) + 1);
        return result;
    }

    private static int LastDotSpaceOrNewLine(string text)
    {
        int index = -1;
        int n = text.Length;

        bool isQuote = false;

        for (int i = 0; i < n; i++)
        {
            char c = text[i];
            if (c == '\"')
            {
                isQuote = !isQuote;
            }
            else if (!isQuote && (c == '.' || c == ' ' || c == '\n' || c == '\r'))
            {
                index = i;
            }
        }
        return index;
    }

    private  (string text,int start,int end) BetweenBracketOrSemicolon(int position, int requiedLevelUp = 1)
    {
        bool accentBalance = true;
        bool quoteBalance = true;
        int bracketBalance = 0;
        int start = position > 0 ? position - 1 : position;

        int len = _cleanSqlText.Length;
        if (position >= len)
        {
            position = len - 1;
        }
        if (position == -1)
        {
            return ("",-1,-1);
        }
        int accualLevelUp = 0;
        while (start > 0 && start < len)
        {
            char c = _cleanSqlText[start];
            if ((c == ';' || c == '(' && ++bracketBalance == 1) && accentBalance && quoteBalance)
            {
                accualLevelUp++;
                if (accualLevelUp == requiedLevelUp)
                {
                    start++;
                    break;
                }
                else
                {
                    bracketBalance--;
                }
            }
            else if (c == '\'')
                accentBalance = !accentBalance;
            else if (c == '\"')
                quoteBalance = !quoteBalance;
            else if (c == ')')
                bracketBalance--;

            start--;
        }
        
        accentBalance = true;
        quoteBalance = true;
        bracketBalance = 0;
        int end = position;
        accualLevelUp = 0;
        while (end < len)
        {
            char c = _cleanSqlText[end];
            if ((c == ';' || c == ')' && --bracketBalance == -1) && accentBalance && quoteBalance)
            {
                accualLevelUp++;
                if (accualLevelUp == requiedLevelUp)
                {
                    break;
                }
                else
                {
                    bracketBalance++;
                }
            }
            else if (c == '\'')
                accentBalance = !accentBalance;
            else if (c == '\"')
                quoteBalance = !quoteBalance;
            else if (c == '(')
                bracketBalance++;

            end++;
        }

        if (end >= len)
            end = len - 1;

        if (start >= len)
            start = len - 1;

        if (end == len - 1)
            end++;
        return (_cleanSqlText[start..end],start,end);
    }

    private string BetweenSemicolons(int position)
    {
        bool accentBalance = true;
        bool quoteBalance = true;

        int len = _cleanSqlText.Length;

        if (position >= len)
        {
            position = len - 1;
        }
        int startPosition = position > 0 ? position - 1 : position;

        if (position == -1)
        {
            return "";
        }

        while (startPosition > 0 && startPosition < len)
        {
            char c = _cleanSqlText[startPosition];

            if (c == ';' && accentBalance && quoteBalance)
            {
                startPosition++;
                break;
            }
            else if (c == '\'')
            {
                accentBalance = !accentBalance;
            }
            else if (c == '\"')
            {
                quoteBalance = !quoteBalance;
            }
            startPosition--;
        }

        accentBalance = true;
        quoteBalance = true;
        int endPosition = position;

        while (endPosition < len)
        {
            char c = _cleanSqlText[endPosition];
            if (c == ';' && accentBalance && quoteBalance)
            {
                break;
            }
            else if (c == '\'')
            {
                accentBalance = !accentBalance;
            }
            else if (c == '\"')
            {
                quoteBalance = !quoteBalance;
            }

            endPosition++;
        }

        if (endPosition > len || endPosition < startPosition)
        {
            return _cleanSqlText[startPosition..len];
        }
        else
        {
            return _cleanSqlText[startPosition..endPosition];
        }
    }

    private static int LastSelect2(ref string innerString, bool trim = true)
    {
        if (trim)
        {
            innerString = innerString.Trim();
        }

        int n = innerString.Length;
        int indexOfSelect = -1;
        int bracketBalance = 0;
        int m = n - 5;

        for (int i = 0; i < m; i++)
        {
            char c = innerString[i];
            if (bracketBalance == 0
                && (((byte)innerString[i + 5] | 32) == 't')
                && (((byte)innerString[i + 4] | 32) == 'c')
                && (((byte)innerString[i + 3] | 32) == 'e')
                && (((byte)innerString[i + 2] | 32) == 'l')
                && (((byte)innerString[i + 1] | 32) == 'e')
                && (((byte) c | 32) == 's')
                //word start
                && (i == 0 || IsEndChar(innerString[i - 1]))
                //word end
                && (i + 5 == n - 1 || IsEndChar(innerString[i + 6]))
              )
            {
                if (i > 0)
                {
                    indexOfSelect = i - 1;
                }
                else
                {
                    indexOfSelect = 0;
                }
            }
            else if (c == '(')
            {
                bracketBalance++;
            }
            else if (c == ')')
            {
                bracketBalance--;
            }
        }
        return indexOfSelect;
    }

    private static string TextPartAfterFrom(string innerStr, bool ignoreBracketMismatch = false)
    {
        int selectIndex = LastSelect2(ref innerStr);

        if (selectIndex == -1)
        {
            return "";
        }
        string afterSelect = innerStr.Substring(selectIndex + "select".Length).Trim();

        int index = -1;
        if (ignoreBracketMismatch)
        {
            index = FirstFromIgnoreBracketMismatch(afterSelect);//first not nested from (after select)
        }
        else
        {
            index = FirstFrom2(afterSelect);//first not nested from (after select)
        }

        StringBuilder? sb = null;

        if (index > 0)
        {
            string afterFrom = afterSelect.Substring(index + "from".Length + 1);
            afterFrom = afterFrom.Trim();

            int contextEnd = FirsWhereLimitOrGroupBy(afterFrom);
            ReadOnlySpan<char> txt = afterFrom.AsSpan()[..(contextEnd > 0 ? contextEnd : afterFrom.Length)];


            //(\b|\s|\n)(inner|outer|cross|)(\b|\s|\n)
            // join -> ,
            int n = txt.Length;
            sb = new StringBuilder(n);

            for (int i = 0; i < n; i++) // 1 bo SQL nie zaczyna się od takich wyrazów
            {
                if (
                    (i == 0 || IsEndChar(txt[i - 1]))
                    && i < n - 3
                    && (((byte)txt[i] | 32) == 'j')
                    && (((byte)txt[i + 1] | 32) == 'o')
                    && (((byte)txt[i + 2] | 32) == 'i')
                    && (((byte)txt[i + 3] | 32) == 'n')
                    //end of word
                    && (i + 3 == n - 1 || IsEndChar(txt[i + 4]))
                  )
                {
                    sb.Append(',');
                    i += 3;
                }
                else if ( //left, full
                    (i == 0 || IsEndChar(txt[i - 1]))
                    &&
                    (
                    i < n - 3
                    && (
                        (((byte)txt[i] | 32) == 'l')
                        && (((byte)txt[i + 1] | 32) == 'e')
                        && (((byte)txt[i + 2] | 32) == 'f')
                        && (((byte)txt[i + 3] | 32) == 't')

                        ||

                        (((byte)txt[i] | 32) == 'f')
                        && (((byte)txt[i + 1] | 32) == 'u')
                        && (((byte)txt[i + 2] | 32) == 'l')
                        && (((byte)txt[i + 3] | 32) == 'l')
                    )
                    //end of word
                    && (i + 3 == n - 1 || IsEndChar(txt[i + 4]))

                    )
                  )
                {
                    i += 3;
                }
                else if ( //as
                    (i == 0 || IsEndChar(txt[i - 1]))
                    &&
                    (
                    i < n - 1
                    && (
                        (((byte)txt[i] | 32) == 'a')
                        && (((byte)txt[i + 1] | 32) == 's')
                    )
                    //end of word
                    && (i + 1 == n - 1 || IsEndChar(txt[i + 2]))
                    )
                  )
                {
                    i += 1;
                }
                else if ( // inner, outer, cross (irrelevant to autocomplete)
                    (i == 0 || IsEndChar(txt[i - 1]))
                    &&
                    (
                    i < n - 4
                    && (
                        (((byte)txt[i] | 32) == 'i')
                        && (((byte)txt[i + 1] | 32) == 'n')
                        && (((byte)txt[i + 2] | 32) == 'n')
                        && (((byte)txt[i + 3] | 32) == 'e')
                        && (((byte)txt[i + 4] | 32) == 'r')
                        ||
                        (((byte)txt[i] | 32) == 'o')
                        && (((byte)txt[i + 1] | 32) == 'u')
                        && (((byte)txt[i + 2] | 32) == 't')
                        && (((byte)txt[i + 3] | 32) == 'e')
                        && (((byte)txt[i + 4] | 32) == 'r')
                        ||
                        (((byte)txt[i] | 32) == 'c')
                        && (((byte)txt[i + 1] | 32) == 'r')
                        && (((byte)txt[i + 2] | 32) == 'o')
                        && (((byte)txt[i + 3] | 32) == 's')
                        && (((byte)txt[i + 4] | 32) == 's')
                    )
                    //end of word
                    && (i + 4 == n - 1 || IsEndChar(txt[i + 5]))

                    )
                  )
                {
                    i += 4;
                }
                else
                {
                    sb.Append(txt[i]);
                }
            }
        }
        
        return sb is null ? "" : sb.ToString();
    }

    private static int FirsWhereLimitOrGroupBy(ReadOnlySpan<char> txt)
    {
        int n = txt.Length;
        int index = -1;
        int bracketBalance = 0;
        for (int i = 0; i < n - 4; i++)
        {
            if (bracketBalance == 0
                //word start
                && (i == 0 || IsEndChar(txt[i - 1]))
                &&
                (
                    i < n - 4
                    && (((byte)txt[i] | 32) == 'w')
                    && (((byte)txt[i + 1] | 32) == 'h')
                    && (((byte)txt[i + 2] | 32) == 'e')
                    && (((byte)txt[i + 3] | 32) == 'r')
                    && (((byte)txt[i + 4] | 32) == 'e')
                    //end of word
                    && (i + 4 == n - 1 || IsEndChar(txt[i + 5]))
                ||
                    i < n - 4
                    && (((byte)txt[i] | 32) == 'l')
                    && (((byte)txt[i + 1] | 32) == 'i')
                    && (((byte)txt[i + 2] | 32) == 'm')
                    && (((byte)txt[i + 3] | 32) == 'i')
                    && (((byte)txt[i + 4] | 32) == 't')
                    //end of word
                    && (i + 4 == n - 1 || IsEndChar(txt[i + 5]))
                 ||
                    i < n - 7
                    && (((byte)txt[i] | 32) == 'g')
                    && (((byte)txt[i + 1] | 32) == 'r')
                    && (((byte)txt[i + 2] | 32) == 'o')
                    && (((byte)txt[i + 3] | 32) == 'u')
                    && (((byte)txt[i + 4] | 32) == 'p')
                    && (txt[i + 5] == ' ' || txt[i + 5] == '\n' || txt[i + 5] == '\t')
                    && (((byte)txt[i + 6] | 32) == 'b')
                    && (((byte)txt[i + 7] | 32) == 'y')
                    //end of word
                    && (i + 7 == n - 1 || IsEndChar(txt[i + 8]))
                )
              )
            {
                if (i > 0)
                {
                    index = i - 1;
                }
                else
                {
                    index = 0;
                }
                return index;
            }
            else if (txt[i] == '(')
            {
                bracketBalance++;
            }
            else if (txt[i] == ')')
            {
                bracketBalance--;
            }
        }
        return index;
    }

    private static int FirstFrom2(ReadOnlySpan<char> afterSelectText)
    {
        int n = afterSelectText.Length;
        int index = -1;
        int brakcetBalance = 0;

        for (int i = 0; i < n - 3; i++)
        {
            if (brakcetBalance == 0 && IsThereFromKeyword(afterSelectText, n, i))
            {
                if (i > 0)
                {
                    index = i - 1;
                }
                else
                {
                    index = 0;
                }
                return index;
            }
            else if (afterSelectText[i] == '(')
            {
                brakcetBalance++;
            }
            else if (afterSelectText[i] == ')')
            {
                brakcetBalance--;
            }
        }

        return index;
    }

    private static int FirstFromIgnoreBracketMismatch(ReadOnlySpan<char> afterSelectText)
    {
        int n = afterSelectText.Length;
        int nr = -1;
        for (int i = 0; i < n - 3; i++)
        {
            if (IsThereFromKeyword(afterSelectText, n, i))
            {
                if (i > 0)
                {
                    nr = i - 1;
                }
                else
                {
                    nr = 0;
                }
                return nr;
            }
        }

        return nr;
    }

    private static bool IsThereFromKeyword(ReadOnlySpan<char> afterSelect, int n, int i)
    {
        return  (((byte)afterSelect[i + 3] | 32) == 'm' )
                        && (((byte)afterSelect[i + 2] | 32) == 'o')
                        && (((byte)afterSelect[i + 1] | 32) == 'r')
                        && (((byte)afterSelect[i    ] | 32) == 'f')
                        //pierwsza litera lub po stacji itp
                        && (i == 0 || IsEndChar(afterSelect[i - 1]))
                        //end of word
                        && (i + 3 == n - 1 || IsEndChar(afterSelect[i + 4]));
    }
    private static bool IsEndChar(char ch)
    {
        return ch == ' ' || ch == '\n' || ch == '\r' || ch == '(' || ch == ')' || ch == '\t';
    }

    private static bool IsAllowdedAlias(ReadOnlySpan<char> alias)
    {
        if (alias.Equals("ON",StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (alias.Equals("ORDER", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (alias.Equals("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (alias.Equals("GROUP", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (alias.Equals("LIMIT", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static readonly SearchValues<char> _whiteChars = SearchValues.Create(['\r', '\n', '\t', ' ']);

    private static Dictionary<string, List<string>> ExtractElements(List<string> chunks)
    {
        Dictionary<string, List<string>> aliasDatabaseTable = [];
        for (int i = 0; i < chunks.Count; i++)
        {
            string tmp = chunks[i];
            tmp = rxToMuchSpaces.Replace(tmp.Trim('\n', '\r', ' '), " ");

            if (tmp.StartsWith(" ON ", StringComparison.OrdinalIgnoreCase) || tmp.StartsWith("ON ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int spaceIndex = tmp.IndexOf(' ');
            string tableName = "";
            string alias = "";
            if (spaceIndex == -1)
            {
                tableName = tmp;
                alias = "";
            }
            else
            {
                tableName = tmp[..spaceIndex];
                alias = tmp[(spaceIndex + 1)..];
            }
            
            int whiteCharIndex = alias.AsSpan().IndexOfAny(_whiteChars);

            if (whiteCharIndex > 0)
            {
                alias = alias[..whiteCharIndex];
            }
            if (!IsAllowdedAlias(alias))
            {
                alias = "";
            }

            if (!aliasDatabaseTable.TryGetValue(tableName, out List<string>? tmpVal1))
            {
                tmpVal1 = [];
                aliasDatabaseTable.Add(tableName, tmpVal1);
            }

            tmpVal1.Add(alias);
        }
        return aliasDatabaseTable;
    }

    private static int FindClosingBracket(ReadOnlySpan<char> sqlPart, int start = 0)
    {
        int index = -1;
        int bracketBalanse = 0;
        int length = sqlPart.Length;

        for (int i = start; i < length; i++)
        {
            if (sqlPart[i] == '(')
            {
                bracketBalanse++;
            }
            else if (sqlPart[i] == ')')
            {
                bracketBalanse--;
            }

            if (bracketBalanse == -1)
            {
                index = i;
                break;
            }
        }
        return index;
    }

    private void ProcessSqlSet(ReadOnlySpan<char> textIncludingWithsAndTables, Dictionary<string, List<string>> additionalTableColumns, char separator = ',', bool isTable = false)
    {
        foreach (string accualWithTable in textIncludingWithsAndTables.MySplit2(separator))
        {
            Match matchWithTable;
            if (!isTable)//with
            {
                matchWithTable = rxWith.Match(accualWithTable);
            }
            else//table / temp table
            {
                matchWithTable = rxTable.Match(accualWithTable);
            }

            if (!matchWithTable.Success)
            {
                continue;
            }

            string aliasWitha = matchWithTable.Groups["aliasDatabaseTable"].Value;

            int bracketIndex = FindClosingBracket(accualWithTable, matchWithTable.Index + matchWithTable.Length);
            if (bracketIndex == -1)
            {
                bracketIndex = accualWithTable.IndexOf("distribute", matchWithTable.Index + matchWithTable.Length, StringComparison.OrdinalIgnoreCase);
                if (bracketIndex == -1)
                {
                    bracketIndex = accualWithTable.Length;
                }
            }
            if (bracketIndex == -1)
            {
                continue;
            }

            string query = accualWithTable.Substring(matchWithTable.Index + matchWithTable.Length, bracketIndex - (matchWithTable.Index + matchWithTable.Length));
            int selectIndex = LastSelect2(ref query);
            if (selectIndex == -1)
            {
                continue;
            }
            string afterSelectText = query.Substring(selectIndex + 6).Trim(); // 6 = "select".Length
            int nr = FirstFrom2(afterSelectText);//first not nested from after select

            if (nr == -1)
            {
                nr = afterSelectText.Length - 1;
            }

            if (nr > 0)
            {
                string between1 = afterSelectText.Substring(0, nr + 1).Trim();
                if (!additionalTableColumns.TryGetValue(aliasWitha, out List<string>? value))
                {
                    value = ([]);
                    additionalTableColumns[aliasWitha] = value;
                }
                value.AddRange(between1.AsSpan().MySplit2().Select(arg => LastWord(arg)));
            }
        }
    }

    private int MakeCteTask(Dictionary<string, List<string>> tableDictionary, int indexOfSelect)
    {
        string fromOneSemicolonToAnother = BetweenSemicolons(indexOfSelect);
        fromOneSemicolonToAnother = rxBracketsWithtNoSelectInside.Replace(fromOneSemicolonToAnother, "");

        tableDictionary.Clear();

        int upperSelectIndex = LastSelect2(ref fromOneSemicolonToAnother);
        if (upperSelectIndex != -1)
        {
            var withsSet = fromOneSemicolonToAnother.AsSpan()[..upperSelectIndex];
            ProcessSqlSet(withsSet, tableDictionary);
        }

        //https://github.com/KrzysztofDusko/JustDataEvoProject/issues/192
        var fromOneBracketToAnother = BetweenBracketOrSemicolon(indexOfSelect);
        var fromOneBracketToAnotherText = fromOneBracketToAnother.text;
        if (fromOneSemicolonToAnother.Contains(fromOneBracketToAnotherText, StringComparison.OrdinalIgnoreCase)
            && fromOneBracketToAnotherText.Length < fromOneSemicolonToAnother.Length)
        {
            int upperSelectIndexX = LastSelect2(ref fromOneBracketToAnotherText);
            if (upperSelectIndexX != -1)
            {
                var withsSet2 = fromOneBracketToAnotherText.AsSpan()[..upperSelectIndexX];
                ProcessSqlSet(withsSet2, tableDictionary);
            }
        }
        return upperSelectIndex;
    }

    private int MakeTempTableHintsTask(Dictionary<string, List<string>> tableDictionary, int indexOfSelect)
    {
        tableDictionary.Clear();
        string query = _cleanSqlText;
        int selectWithIndex = LastSelect2(ref query, trim: false);

        if (selectWithIndex != -1)
        {
            var tables = query.AsSpan()[..selectWithIndex];
            ProcessSqlSet(tables, tableDictionary, ';', isTable: true);
        }

        //https://github.com/KrzysztofDusko/JustDataEvoProject/issues/192
        var fromOneBracketToAnother = BetweenBracketOrSemicolon(indexOfSelect);
        var fromOneBracketToAnotherText = fromOneBracketToAnother.text;
        if (fromOneBracketToAnotherText.Length < query.Length)
        {
            int tmpIndex1 = LastSelect2(ref fromOneBracketToAnotherText);
            if (tmpIndex1 != -1)
            {
                var tableSet = fromOneBracketToAnotherText.AsSpan()[..tmpIndex1];
                ProcessSqlSet(tableSet, tableDictionary);
            }
        }
        //https://github.com/KrzysztofDusko/JustDataEvoProject/issues/192 v2
        fromOneBracketToAnother = BetweenBracketOrSemicolon(indexOfSelect,2);
        fromOneBracketToAnotherText = fromOneBracketToAnother.text;
        if (fromOneBracketToAnotherText.Length < query.Length)
        {
            int tmpIndex1 = LastSelect2(ref fromOneBracketToAnotherText);
            if (tmpIndex1 != -1)
            {
                var tableSet = fromOneBracketToAnotherText.AsSpan()[..tmpIndex1];
                ProcessSqlSet(tableSet, tableDictionary);
            }
        }

        return selectWithIndex;
    }
}

