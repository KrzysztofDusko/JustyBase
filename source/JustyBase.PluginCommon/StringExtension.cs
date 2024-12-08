using System.Buffers;
using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace JustyBase.StringExtensions;

public static partial class StringExtension
{
    private readonly static SearchValues<char> _specialCharsToFind = SearchValues.Create(['\'', '\"', '-', '/']);
    public static string CreateCleanSql(this string actualString)
    {
        string str = string.Create(actualString.Length, actualString, static (chars, buf) =>
        {
            var span = buf.AsSpan();
            var orginalBufAsSpan = buf.AsSpan();
            for (int i = 0; i < chars.Length; i++)
            {
                span = orginalBufAsSpan[i..];
                int nextSpecialIndex = span.IndexOfAny(_specialCharsToFind);
                if (nextSpecialIndex == -1)
                {
                    nextSpecialIndex = chars.Length - i - 1;
                }
                span[..nextSpecialIndex].CopyTo(chars[i..]);
                i += nextSpecialIndex;

                char c = buf[i];

                if (c == '\'')
                {
                    chars[i] = ' ';
                    i++;
                    if (i == chars.Length)
                    {
                        break;
                    }
                    int indx = orginalBufAsSpan[i..].IndexOf('\'');
                    if (indx == -1)
                    {
                        chars[i..].Fill(' ');
                        break;
                    }
                    chars.Slice(i, indx + 1).Fill(' ');
                    i += indx;
                    continue;
                }
                else if (c == '\"')
                {
                    chars[i] = ' ';
                    i++;
                    if (i == chars.Length)
                    {
                        break;
                    }
                    int indx = orginalBufAsSpan[i..].IndexOf('\"');
                    if (indx == -1)
                    {
                        chars[i..].Fill(' ');
                        break;
                    }
                    chars.Slice(i, indx + 1).Fill(' ');
                    i += indx;
                    continue;
                }
                else if (c == '-' && i < chars.Length - 1 && buf[i + 1] == '-')
                {
                    chars[i] = ' ';
                    c = (char)0;
                    i++;
                    while (i < chars.Length && c != '\n')
                    {
                        c = buf[i];
                        if (c != '\r' && c != '\n')
                        {
                            chars[i] = ' ';
                        }
                        else
                        {
                            chars[i] = c;
                        }

                        i++;
                    }
                    i--;
                    continue;
                }
                else if (c == '/' && i < chars.Length - 1 && buf[i + 1] == '*')
                {
                    chars[i] = ' ';
                    chars[i + 1] = ' ';
                    i += 2;
                    if (i == chars.Length)
                    {
                        break;
                    }
                    int indx = orginalBufAsSpan[i..].IndexOf("*/");
                    if (indx == -1)
                    {
                        chars[i..].Fill(' ');
                        break;
                    }
                    chars.Slice(i, indx + 2).Fill(' ');
                    i += indx;
                    i++;
                    continue;
                }
                chars[i] = c;
            }
        });
        return str;
    }

    public static bool IsAllSqlComment(this string txt)
    {
        int n = txt.Length;
        for (int i = 0; i < n; i++)
        {
            char c1 = txt[i];
            if (char.IsWhiteSpace(c1))
            {
                continue;
            }
            if (c1 != '-' && c1 != '/')
            {
                return false;
            }

            char c2 = txt[i + 1];
            if (i < n - 1 && c1 == '-' && c2 == '-')
            {
                do
                {
                    i++;
                } while (i < n && txt[i] != '\n');
            }
            else if (i < n - 1 && c1 == '/' && c2 == '*')
            {
                do
                {
                    i++;
                } while (i < n - 1 && (txt[i] != '*' || txt[i + 1] != '/'));
                i++;
            }
        }

        return true;
    }

    public static List<Range> ClipboardTextToLinesArray(ReadOnlySpan<char> clip)
    {
        char sepInClipboard = '\t';
        List<int> l1 = new List<int>();

        int n = clip.Length;
        for (int i = 1; i < n - 1; i++)
        {
            if (clip[i] == sepInClipboard && clip[i + 1] == '"')
            {
                i += 2;
                var indx = clip[i..].IndexOf('"');
                i += indx;
            }
            else if (clip[i] == '\n')
            {
                l1.Add(i);
            }
        }
        if (l1.Count == 0)
        {
            return new List<Range>();
        }
        var res = new List<Range>(l1.Count);

        res.Add(new Range(0, l1[0]));

        for (int i = 1; i < l1.Count; i++)
        {
            res.Add(new Range(l1[i - 1] + 1, l1[i - 1] + 1 + l1[i] - l1[i - 1] - 1));
        }
        res.Add(new Range((l1[^1] + 1), clip.Length));
        return res;
    }

    public static void GetDotsPositionsAndCount(this string text, out int lastDot, out int dotCnt, out int firstDot)
    {
        dotCnt = 0;
        firstDot = -1;
        lastDot = -1;
        int n = text.Length;
        bool isQuote = false;

        for (int i = 0; i < n; i++)
        {
            char c = text[i];
            if (c == '\"')
            {
                isQuote = !isQuote;
            }
            else if (c == '.' && !isQuote)
            {
                lastDot = i;
                if (firstDot == -1)
                {
                    firstDot = i;
                }
                dotCnt++;
            }
        }
    }    

    public static string ReplaceVariablesInSql(this string query, List<string> toAsk, Dictionary<string, string> knownVariables, char variableStart = '$')
    {
        string cleanSql = query.CreateCleanSql();
        StringBuilder stringBuilder = new StringBuilder();

        List<string>? sortedToAsk = null;

        int i1 = 0;
        int i2 = cleanSql.IndexOf(variableStart);
        while (i2 != -1)
        {
            sortedToAsk ??= toAsk.OrderByDescending(o => o.Length).ToList();
            stringBuilder.Append(query.AsSpan()[i1..i2]);
            bool founded = false;
            foreach (var prosalVariableName in sortedToAsk)
            {
                if (query.AsSpan()[i2..].StartsWith(prosalVariableName, StringComparison.OrdinalIgnoreCase))
                {
                    stringBuilder.Append(knownVariables[prosalVariableName]);
                    i2 += prosalVariableName.Length;
                    founded = true;
                    break;
                }
            }
            i1 = i2;
            if (!founded)
            {
                i2++;
            }

            var tmp = cleanSql.AsSpan()[i2..].IndexOf(variableStart);
            if (tmp == -1)
            {
                stringBuilder.Append(query.AsSpan()[i1..]);
                break;
            }
            i2 += tmp;
        }
        if (stringBuilder.Length == 0)
        {
            return query;
        }

        return stringBuilder.ToString();

    }

    public static string PasteAsInHelper(string pasteType, string clip)
    {
        //clip should be small so this alocation should not be a problem
        string[] lines = clip.Split(Environment.NewLine);

        var tempCol = lines.Where(arg => arg != "").Select(arg => arg.Trim());
        var tempColDistinct = tempCol.Distinct();

        StringBuilder stringBuilder = new StringBuilder(clip?.Length ?? 64);
        stringBuilder.AppendLine($"--REGION pasted {tempColDistinct.Count()} unique from {tempCol.Count()}");

        if (pasteType != "Text")
        {
            stringBuilder.Append($"({String.Join(",\n", tempColDistinct)})");
        }
        else
        {
            stringBuilder.Append('(');
            stringBuilder.Append(String.Join(",\n", tempColDistinct.Select(arg => $"'{arg}'")));
            stringBuilder.Append(')');
        }
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"--ENDREGION");
        return stringBuilder.ToString();
    }

    public static string ConvertAsSqlCompatybile(object data)
    {
        if (data is null)
        {
            return "";
        }
        else if (data == DBNull.Value)
        {
            return "NULL";
        }
        else if (data is Single singledata)
        {
            return singledata.ToString(CultureInfo.InvariantCulture);
        }
        else if (data is double doubledata)
        {
            return doubledata.ToString(CultureInfo.InvariantCulture);
        }
        else if (data is decimal decimaledata)
        {
            return decimaledata.ToString(CultureInfo.InvariantCulture);
        }
        else if (data is DateTime datetimedata)
        {
            return datetimedata.ToString("yyyy-MM-dd HH:mm:ss");
        }
        else if (data is string stringdata)
        {
            return retQuoted(data);
        }
        else
        {
            return data?.ToString() ?? "NULL";
        }

        static string retQuoted(object o)
        {
            string? res = o?.ToString();
            if (res is not null && res.Contains('\''))
            {
                res = res.Replace("'", "''");
            }

            return $"'{res}'";
        }
    }



    //https://www.ibm.com/docs/en/netezza?topic=keywords-sql-common-reserved-words
    private static readonly FrozenSet<string> _SQL_RESERVER_KEYWORDS = new HashSet<string>()
    {
        "CTID",
        "OID",
        "XMIN",
        "CMIN",
        "XMAX",
        "CMAX",
        "TABLEOID",
        "ROWID",
        "DATASLICEID",
        "CREATEXID",
        "DELETEXID",
        "BORT",
        "ALL",
        "ALLOCATE",
        "ANALYSE",
        "ANALYZE",
        "AND",
        "ANY",
        "AS",
        "ASC",
        "AUTOMAINT",
        "AWSS3",
        "AZUREBLOB",
        "BETWEEN",
        "BINARY",
        "BIT",
        "BOTH",
        "CASE",
        "CAST",
        "CHAR",
        "CHARACTER",
        "CHECK",
        "CLUSTER",
        "COALESCE",
        "COLLATE",
        "COLLATION",
        "COLUMN",
        "CONSTRAINT",
        "COPY",
        "CROSS",
        "CURRENT",
        "CURRENT_CATALOG",
        "CURRENT_DATE",
        "CURRENT_DB",
        "CURRENT_SCHEMA",
        "CURRENT_SID",
        "CURRENT_TIME",
        "CURRENT_TIMESTAMP",
        "CURRENT_USER",
        "CURRENT_USERID",
        "CURRENT_USEROID",
        "DAYSPERROW",
        "DEALLOCATE",
        "DEC",
        "DECIMAL",
        "DECODE",
        "DEFAULT",
        "DEREGISTER",
        "DESC",
        "DISTINCT",
        "DISTRIBUTE",
        "DO",
        "ELSE",
        "END",
        "EXCEPT",
        "EXCLUDE",
        "EXISTS",
        "EXPLAIN",
        "EXPRESS",
        "EXTEND",
        "EXTERNAL",
        "EXTRACT",
        "FALSE",
        "FIRST",
        "FLOAT",
        "FOLLOWING",
        "FOR",
        "FOREIGN",
        "FROM",
        "FULL",
        "FUNCTION",
        "GENSTATS",
        "GLOBAL",
        "GROUP",
        "HAVING",
        "HISTOGRAM",
        "IDENTIFIER_CASE",
        "ILIKE",
        "IN",
        "INDEX",
        "INITIALLY",
        "INNER",
        "INOUT",
        "INTERSECT",
        "INTERVAL",
        "INTO",
        "JOURNAL",
        "LEADING",
        "LEFT",
        "LIKE",
        "LIMIT",
        "LOAD",
        "LOCAL",
        "LOCK",
        "MINUS",
        "MOVE",
        "NATURAL",
        "NCHAR",
        "NEW",
        "NOCASCADE",
        "NOT",
        "NOTNULL",
        "NULL",
        "NULLS",
        "NUMERIC",
        "NVL",
        "NVL2",
        "OFF",
        "OFFSET",
        "OLD",
        "ON",
        "ONLINE",
        "ONLY",
        "OR",
        "ORDER",
        "OTHERS",
        "OUT",
        "OUTER",
        "OVER",
        "OVERLAPS",
        "PARTITION",
        "PAUSESTEPS",
        "PAUSETIME",
        "POSITION",
        "PRECEDING",
        "PRECISION",
        "PRESERVE",
        "PRIMARY",
        "REGISTER",
        "RESET",
        "REUSE",
        "RIGHT",
        "ROWS",
        "SELECT",
        "SESSION_USER",
        "SETOF",
        "SHOW",
        "SOME",
        "TABLE",
        "TEMPORAL",
        "THEN",
        "TIES",
        "TIME_TRAVEL_ENABLE",
        "TIME",
        "TIMESTAMP",
        "TO",
        "TRAILING",
        "TRANSACTION",
        "TRIGGER",
        "TRIM",
        "TRUE",
        "UNBOUNDED",
        "UNION",
        "UNIQUE",
        "USER",
        "USING",
        "VACUUM",
        "VARCHAR",
        "VERBOSE",
        "VERSION",
        "VIEW",
        "UNBOUNDED",
        "UNION",
        "UNIQUE",
        "USER"
    }.ToFrozenSet();

    /// <summary>
    /// is good name for SQL name
    /// </summary>
    /// <param name="word"></param>
    /// <returns></returns>
    public static bool IsGoodName(this string word, bool preferUpper)
    {
        for (int i = 0; i < word.Length; i++)
        {
            char c = word[i];
            if ((preferUpper && Char.IsLower(c) || !preferUpper && Char.IsUpper(c))
                || !Char.IsLetter(c) && !Char.IsDigit(c) && c != '_')
            {
                return false;
            }
        }
        return !_SQL_RESERVER_KEYWORDS.Contains(word);
    }

    //public static string CutToLongNumeric(this string arg, int precision = 8)
    //{
    //    if (!arg.Contains('.'))
    //    {
    //        return arg;
    //    }
    //    else
    //    {
    //        int n = arg.IndexOf('.');
    //        int l = arg.Length;
    //        if (l - n - 1 <= 8)
    //        {
    //            return arg;
    //        }
    //        else
    //        {
    //            var firstPart = arg.AsSpan()[..n];
    //            var secoudtPart = arg.AsSpan().Slice(n + 1, precision);
    //            return $"{firstPart}.{secoudtPart}";
    //        }
    //    }
    //}


    private static readonly FrozenSet<string> _notAllowdedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ABORT", "DECIMAL", "INTERVAL", "PRESERVE",
        "ALL", "DECODE", "INTO", "PRIMARY",
        "ALLOCATE", "DEFAULT", "LEADING", "RESET",
        "ANALYSE", "DESC", "LEFT", "REUSE",
        "ANALYZE", "DISTINCT", "LIKE", "RIGHT",
        "AND", "DISTRIBUTE", "LIMIT", "ROWS",
        "ANY", "DO", "LOAD", "SELECT",
        "AS", "ELSE", "LOCAL", "SESSION_USER",
        "ASC", "END", "LOCK", "SETOF",
        "BETWEEN", "EXCEPT", "MINUS", "SHOW",
        "BINARY", "EXCLUDE", "MOVE", "SOME",
        "BIT", "EXISTS", "NATURAL", "TABLE",
        "BOTH", "EXPLAIN", "NCHAR", "THEN",
        "CASE", "EXPRESS", "NEW", "TIES",
        "CAST", "EXTEND", "NOT", "TIME",
        "CHAR", "EXTERNAL", "NOTNULL", "TIMESTAMP",
        "CHARACTER", "EXTRACT", "NULL", "TO",
        "CHECK", "FALSE", "NULLS", "TRAILING",
        "CLUSTER", "FIRST", "NUMERIC", "TRANSACTION",
        "COALESCE", "FLOAT", "NVL", "TRIGGER",
        "COLLATE", "FOLLOWING", "NVL2", "TRIM",
        "COLLATION", "FOR", "OFF", "TRUE",
        "COLUMN", "FOREIGN", "OFFSET", "UNBOUNDED",
        "CONSTRAINT", "FROM", "OLD", "UNION",
        "COPY", "FULL", "ON", "UNIQUE",
        "CROSS", "FUNCTION", "ONLINE", "USER",
        "CURRENT", "GENSTATS", "ONLY", "USING",
        "CURRENT_CATALOG", "GLOBAL", "OR", "VACUUM",
        "CURRENT_DATE", "GROUP", "ORDER", "VARCHAR",
        "CURRENT_DB", "HAVING", "OTHERS", "VERBOSE",
        "CURRENT_SCHEMA", "IDENTIFIER_CASE", "OUT", "VERSION",
        "CURRENT_SID", "ILIKE", "OUTER", "VIEW",
        "CURRENT_TIME", "IN", "OVER", "WHEN",
        "CURRENT_TIMESTAMP", "INDEX", "OVERLAPS", "WHERE",
        "CURRENT_USER", "INITIALLY", "PARTITION", "WITH",
        "CURRENT_USERID", "INNER", "POSITION", "WRITE",
        "CURRENT_USEROID", "INOUT", "PRECEDING", "RESET",
        "DEALLOCATE", "INTERSECT", "PRECISION", "REUSE",
        "DEC"
    }.ToFrozenSet();


    [GeneratedRegex(@"[^a-zA-Z0-9_]", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex rxGen();

    [GeneratedRegex(@"^_*", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex rx2Gen();

    [GeneratedRegex(@"^[^a-zA-Z]", RegexOptions.Compiled)]
    private static partial Regex rx3Gen();

    private static readonly Regex rx = rxGen();
    private static readonly Regex rx2 = rx2Gen();
    private static readonly Regex rx3 = rx3Gen();

    public static string RandomSuffix(string startName = "export_", int len = 10, bool withDate = true)
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        if (string.IsNullOrEmpty(startName)) startName = "ABCDE_";

        return startName + (withDate ? DateTime.Now.ToString("yyMMdd_HHmm") : "")
                         + new string(Enumerable.Repeat(letters, len).Select(s => s[Random.Shared.Next(s.Length)])
                             .ToArray());
    }

    public static void DeDuplicate(string[] list)
    {
        Dictionary<string, (int, int)> dict = new(list.Length, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < list.Length; i++)
            if (dict.TryGetValue(list[i], out var value))
                dict[list[i]] = (value.Item1 + 1, value.Item2);
            else
                dict[list[i]] = (1, 0);

        for (var i = 0; i < list.Length; i++)
            if (dict[list[i]].Item1 > 1)
            {
                dict[list[i]] = (dict[list[i]].Item1, dict[list[i]].Item2 + 1);
                list[i] = list[i] + "_" + dict[list[i]].Item2;
            }
    }

    public static string NormalizeDbColumnName(this string arg)
    {
        if (string.IsNullOrWhiteSpace(arg)) return RandomSuffix("EMPTY_COLNAME_", 3);

        var res = rx2.Replace(rx.Replace(
            arg.Trim().ToUpper()
                .Replace('Ą', 'A')
                .Replace('Ć', 'C')
                .Replace('Ę', 'E')
                .Replace('Ł', 'L')
                .Replace('Ń', 'N')
                .Replace('Ó', 'O')
                .Replace('Ś', 'S')
                .Replace('Ż', 'Z')
                .Replace('Ź', 'Z')
            , "_"), "");

        if (res.Length >= 129) res = res[..126];
        if (rx3.IsMatch(res)) res = $"K{res}";
        if (_notAllowdedWords.Contains(res)) res += RandomSuffix("_", 2, false);

        return res.Trim();
    }

    public static string[] GetSqLParts(this string sql)
    {
        var maxLen = 8_192;
        if (sql.Length < maxLen) return [sql];

        var tmpList = new List<string>();
        var m = (sql.Length - 1) / maxLen;

        for (var i = 0; i < m + 1; i++)
        {
            var start = i * maxLen;
            var end = maxLen * (i + 1);
            if (end > sql.Length) end = sql.Length;

            tmpList.Add(sql[start..end]);
        }

        return tmpList.ToArray();
    }

    public static bool EndsWithAny(this string str, IEnumerable<string> Values)
    {
        foreach (var Entry in Values)
            if (str.EndsWith(Entry, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
    public static List<string> MySplit2(this ReadOnlySpan<char> primarySQL, char separator = ',')
    {
        List<string> newCreatedList = new List<string>();

        int hmN = 0;
        int hmq1 = 0;
        int hmq2 = 0;

        int prevRes = -1;
        int N = primarySQL.Length;
        for (int i = 0; i < N; i++)
        {
            char c = primarySQL[i];
            if (c == separator && hmN == 0 && hmq1 == 0 && hmq2 == 0)
            {
                newCreatedList.Add(primarySQL.Slice(prevRes + 1, i - prevRes - 1).ToString());
                prevRes = i;
            }
            else if (c == '(')
            {
                hmN++;
            }
            else if (c == ')')
            {
                hmN--;
            }
            else if (c == '\'')
            {
                hmq1 = 1 - hmq1;
            }
            else if (c == '\"')
            {
                hmq2 = 1 - hmq2;
            }
        }

        newCreatedList.Add(primarySQL[(prevRes + 1)..].ToString());

        return newCreatedList;
    }

    public static List<string> MySplitForSqlSplit(this string primarySql, char separator = ',')
    {
        List<string> newCreatedList = [];

        int hmN = 0;
        int hmq1 = 0;
        int hmq2 = 0;

        int prevRes = -1;
        int N = primarySql.Length;
        char c = (char)0;
        for (int i = 0; i < N; i++)
        {
            c = primarySql[i];
            if (c == separator && hmN == 0 && hmq1 == 0 && hmq2 == 0)
            {
                newCreatedList.Add(primarySql.Substring(prevRes + 1, i - prevRes - 1));
                prevRes = i;
            }
            else if (c == '(')
            {
                hmN++;
            }
            else if (c == ')')
            {
                hmN--;
            }
            else if (c == '\'')
            {
                hmq1 = 1 - hmq1;
            }
            else if (c == '\"')
            {
                hmq2 = 1 - hmq2;
            }
            else if (c == '-' && i < N - 1 && primarySql[i + 1] == '-')
            {
                while (i < N && primarySql[i] != '\n')
                {
                    ++i;
                }
            }
            else if (c == '/' && i < N - 1 && primarySql[i + 1] == '*')
            {
                while (i < N - 1 && !(primarySql[i] == '*' && primarySql[i + 1] == '/'))
                {
                    ++i;
                }
            }
        }
        newCreatedList.Add(primarySql.Substring(prevRes + 1));
        return newCreatedList;
    }

    //for typos
    public static int DamerauLevenshteinDistance(this Span<char> firstText, ReadOnlySpan<char> secondText)
    {
        var n = firstText.Length + 1;
        var m = secondText.Length + 1;
        var arrayD = new int[n, m];

        for (var i = 0; i < n; i++)
        {
            arrayD[i, 0] = i;
        }

        for (var j = 0; j < m; j++)
        {
            arrayD[0, j] = j;
        }

        for (var i = 1; i < n; i++)
        {
            for (var j = 1; j < m; j++)
            {
                var cost = firstText[i - 1] == secondText[j - 1] ? 0 : 1;

                arrayD[i, j] = MinimumOfThree(arrayD[i - 1, j] + 1, // delete
                                                        arrayD[i, j - 1] + 1, // insert
                                                        arrayD[i - 1, j - 1] + cost); // replacement

                if (i > 1 && j > 1
                   && firstText[i - 1] == secondText[j - 2]
                   && firstText[i - 2] == secondText[j - 1])
                {
                    arrayD[i, j] = Math.Min(arrayD[i, j],
                    arrayD[i - 2, j - 2] + cost); // permutation
                }
            }
        }

        return arrayD[n - 1, m - 1];

        static int MinimumOfThree(int a, int b, int c) => (a = a < b ? a : b) < c ? a : c;
    }


    /// <summary>
    ///  select 'wOrd' -> SELECT 'wOrd' (not  SELECT 'WORD')
    /// </summary>
    /// <param name="actualString"></param>
    /// <param name="upper"></param>
    /// <returns></returns>
    public static string ChangeCaseRespectingSqlRules(this string actualString, bool upper)
    {
        string str = string.Create(actualString.Length, actualString, (chars, buf) =>
        {
            for (int i = 0; i < chars.Length; i++)
            {
                char c = buf[i];

                if (c == '\'')
                {
                    chars[i] = c;
                    c = (char)0;
                    i++;
                    while (i < chars.Length && c != '\'')
                    {
                        c = buf[i];
                        chars[i] = c;
                        i++;
                    }
                    i--;
                    continue;
                }
                else if (c == '\"')
                {
                    chars[i] = c;
                    c = (char)0;
                    i++;
                    while (i < chars.Length && c != '\"')
                    {
                        c = buf[i];
                        chars[i] = c;
                        i++;
                    }
                    i--;
                    continue;
                }
                else if (c == '-' && i < chars.Length - 1 && buf[i + 1] == '-')
                {
                    chars[i] = c;
                    c = (char)0;
                    i++;
                    while (i < chars.Length && c != '\n')
                    {
                        c = buf[i];
                        chars[i] = c;
                        i++;
                    }
                    i--;
                    continue;
                }
                else if (c == '/' && i < chars.Length - 1 && buf[i + 1] == '*')
                {
                    chars[i] = c;
                    c = (char)0;
                    i++;
                    while (i < chars.Length)
                    {
                        c = buf[i];
                        chars[i] = c;
                        i++;
                        if (c == '*' && i < chars.Length && buf[i] == '/')
                        {
                            chars[i] = '/';
                            break;
                        }
                    }
                    i--;
                    continue;
                }

                if (upper && char.IsLower(c))
                {
                    c = char.ToUpper(c);
                }
                else if (!upper && char.IsUpper(c))
                {
                    c = char.ToLower(c);
                }
                chars[i] = c;
            }
        });
        return str;
    }

}