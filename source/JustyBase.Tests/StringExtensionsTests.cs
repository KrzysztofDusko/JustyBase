using JustyBase.PluginDatabaseBase.Extensions;

namespace JustyBase.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("select '10'","select     ")]
    [InlineData("/*A*/B/*C*/", "     B     ")]
    [InlineData("'aaa'", "     ")]
    [InlineData("\"aaa\"", "     ")]
    [InlineData("", "")]
    [InlineData("\"\"", "  ")]
    [InlineData("''", "  ")]
    [InlineData("'", " ")]
    public void CreateCleanSqlTheory(string input, string expected)
    {
        var result = input.CreateCleanSql();
        Assert.Equal(expected, result);
    }


    private List<string> _stringsToTest = [
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
        """
    ];

    [Fact]
    public void CreateCleanSqlShouldHaveSameResultAsDifferentImplementationV1()
    {
        foreach (var s in _stringsToTest)
        {
            var expected = CreateCleanSqlAlternativeImplementation(s);
            var result = s.CreateCleanSql();
            Assert.Equal(expected, result);
        }
    }

    [Fact]
    public void CreateCleanSqlShouldHaveSameResultAsDifferentImplementationV2()
    {
        foreach (var s in Directory.GetFiles("D:\\DEV\\sqls\\", "*.sql", SearchOption.AllDirectories))
        {
            var expected = CreateCleanSqlAlternativeImplementation(s);
            var result = s.CreateCleanSql();
            Assert.Equal(expected, result);
        }
    }

    private string CreateCleanSqlAlternativeImplementation(string actualString)
    {
        string str = string.Create(actualString.Length, actualString, (chars, buf) =>
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

}
