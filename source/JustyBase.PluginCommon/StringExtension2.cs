using System.Text.RegularExpressions;

namespace JustyBase.PluginDatabaseBase.Extensions;

public static partial class StringExtension2
{
    private static readonly HashSet<string> _notAllowdedWords = new(StringComparer.OrdinalIgnoreCase)
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
    };


    [GeneratedRegex(@"[^a-zA-Z0-9_]", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex rxGen();

    [GeneratedRegex(@"^_*", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex rx2Gen();

    [GeneratedRegex(@"^[^a-zA-Z]", RegexOptions.Compiled)]
    private static partial Regex rx3Gen();

    private static readonly Regex rx = rxGen();
    private static readonly Regex rx2 = rx2Gen();
    private static readonly Regex rx3 = rx3Gen();

    public static string RandomName(string startName = "export_", int len = 10, bool withDate = true)
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
        if (string.IsNullOrWhiteSpace(arg)) return RandomName("EMPTY_COLNAME_", 3);

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
        if (_notAllowdedWords.Contains(res)) res += RandomName("_", 2, false);

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

    public static bool EndsWith(this string str, IEnumerable<string> Values)
    {
        foreach (var Entry in Values)
            if (str.EndsWith(Entry, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}