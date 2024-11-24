using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchy;

[MemoryDiagnoser]
public class CreateCleanSqlBench
{
    public string ActualString = "SELECT * FROM JUST_DATA..DIM_DATE";
    [GlobalSetup]
    public void Setup()
    {
        ActualString = File.ReadAllText("D:\\DEV\\sqls\\UPDATE_VERSIONS.SQL");
        //ActualString = File.ReadAllText("D:\\DEV\\sqls\\LONG_SQL_4MB.SQL");
        //ActualString = File.ReadAllText("D:\\DEV\\sqls\\LONG_SQL.SQL");

        //ActualString = File.ReadAllText("D:\\DEV\\sqls\\sqlServerResults.csv");
        //ActualString = File.ReadAllText("D:\\DEV\\sqls\\all_text.SQL");
        //ActualString = File.ReadAllText("D:\\DEV\\sqls\\UPDATE_VERSIONS.SQL");
    }

    public List<string> StringsToTest { get; set; } = [
        "ABC/*DEF*/XYZ",
        "/*",
        "*/",
        "'/*'",
        "/**/",
        "/*\n*/",
        "/*\r\n*/",
        "'AAA'B'CCC'",
         "'AAA''CCC'",
         "'",
        "''",
         "'''",
        """
            select 10
            --test
            select 20
        """,
        """
            select 10
            --test
            select 20
        """,
        File.ReadAllText("D:\\DEV\\sqls\\UPDATE_VERSIONS.SQL"),
        File.ReadAllText("D:\\DEV\\sqls\\LONG_SQL.SQL"),
        File.ReadAllText("D:\\DEV\\sqls\\LONG_SQL_4MB.SQL"),
        File.ReadAllText("D:\\DEV\\sqls\\sqlServerResults.csv"),
        File.ReadAllText("D:\\DEV\\sqls\\all_text.sql"),
        File.ReadAllText("D:\\DEV\\sqls\\UPDATE_VERSIONS.SQL")
        ];

    [Benchmark(Baseline = true)]
    public string CreateCleanSql()
    {
        string str = string.Create(ActualString.Length, ActualString, (chars, buf) =>
        {
            for (int i = 0; i < chars.Length; i++)
            {
                char c = buf[i];

                if (c == '\'')
                {
                    chars[i] = ' ';
                    c = (char)0;
                    i++;
                    while (i < chars.Length && c != '\'')
                    {
                        c = buf[i];
                        chars[i] = ' ';
                        i++;
                    }
                    i--;
                    continue;
                }
                else if (c == '\"')
                {
                    chars[i] = ' ';
                    c = (char)0;
                    i++;
                    while (i < chars.Length && c != '\"')
                    {
                        c = buf[i];
                        chars[i] = ' ';
                        i++;
                    }
                    i--;
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
                    c = (char)0;
                    i++;
                    while (i < chars.Length)
                    {
                        c = buf[i];
                        chars[i] = ' ';
                        i++;
                        if (c == '*' && i < chars.Length && buf[i] == '/')
                        {
                            chars[i] = ' ';
                            ++i;
                            break;
                        }
                    }
                    i--;
                    continue;
                }
                chars[i] = c;
            }
        });
        return str;
    }

    private readonly static SearchValues<char> _specialCharsToFind = SearchValues.Create(['\'', '\"','-', '/']);

    [Benchmark]
    public string CreateCleanSqlBetter()
    {
        string str = string.Create(ActualString.Length, ActualString, (chars, buf) =>
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
                    c = (char)0;
                    i++;
                    while (i < chars.Length && c != '\'')
                    {
                        c = buf[i];
                        chars[i] = ' ';
                        i++;
                    }
                    i--;
                    continue;
                }
                else if (c == '\"')
                {
                    chars[i] = ' ';
                    c = (char)0;
                    i++;
                    while (i < chars.Length && c != '\"')
                    {
                        c = buf[i];
                        chars[i] = ' ';
                        i++;
                    }
                    i--;
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
                    c = (char)0;
                    i++;
                    while (i < chars.Length)
                    {
                        c = buf[i];
                        chars[i] = ' ';
                        i++;
                        if (c == '*' && i < chars.Length && buf[i] == '/')
                        {
                            chars[i] = ' ';
                            ++i;
                            break;
                        }
                    }
                    i--;
                    continue;
                }
                chars[i] = c;
            }
        });
        return str;
    }


    [Benchmark]
    public string CreateCleanSqlBetterV2()
    {
        string str = string.Create(ActualString.Length, ActualString, static (chars, buf) =>
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
                    chars.Slice(i,indx + 1).Fill(' ');
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
                    c = (char)0;
                    i++;
                    while (i < chars.Length)
                    {
                        c = buf[i];
                        chars[i] = ' ';
                        i++;
                        if (c == '*' && i < chars.Length && buf[i] == '/')
                        {
                            chars[i] = ' ';
                            ++i;
                            break;
                        }
                    }
                    i--;
                    continue;
                }
                chars[i] = c;
            }
        });
        return str;
    }


    [Benchmark]
    public string CreateCleanSqlBetterV3()
    {
        string str = string.Create(ActualString.Length, ActualString, static (chars, buf) =>
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
                    chars[i+1] = ' ';
                    i+=2;
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

}



