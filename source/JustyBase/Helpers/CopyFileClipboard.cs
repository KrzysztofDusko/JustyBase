using JustyBase.Models;
using Sylvan.Data.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JustyBase.Helpers;

internal sealed class CopyHtmlOrTextClipboard : IDataObject
{
    private string _txt;
    private static readonly string[] formats = ["HTML Format", "Text"/*, "Unknown_Format_16", "Unknown_Format_7"*/];

    private byte[] _data;
    private static readonly (string, string)[] _valuesToReplace =
    [
        ("&", "&amp;"),
        ("<", "&lt;"),
        (">", "&gt;"),
        ("\"", "&quot;"),
        ("'", "&apos;")
    ];

    private static string GetEscapedText(string txt)
    {
        if (txt is null)
        {
            return "";
        }

        foreach (var (oldValue, newValue) in _valuesToReplace)
        {
            if (txt.Contains(oldValue, StringComparison.Ordinal))
            {
                txt = txt.Replace(oldValue, newValue);
            }
        }
        return txt;
    }

    private readonly TableOfSqlResults _table = null;


    private string TableToHtml()
    {
        StringBuilder sb = new();
        sb.Append("<table style=\"border: 2px solid black; background-color: rgb(220, 220, 220)\">");

        sb.Append("<tr>");
        for (int j = 0; j < _table.Headers.Count; j++)
        {
            sb.Append($"<th>{GetEscapedText(_table.Headers[j])}</th>");
        }
        sb.Append("</tr>");
        for (int i = 0; i < _table.Rows.Count; i++)
        {
            var fields = _table.Rows[i].Fields;
            sb.Append("<tr>");
            for (int j = 0; j < _table.Headers.Count; j++)
            {
                sb.Append($"<td>{GetEscapedText(fields[j]?.ToString())}</td>");
            }
            sb.Append("</tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }
    private static byte[] GetHtmlBytes(string htmlCode)
    {
        int htmlLenInBytex = Encoding.Default.GetBytes(htmlCode).Length;

        //int htmlLen = _htmlCode.Length;
        int StartHTML = 97;
        int StartFragment = 133;
        int EndHTML = htmlLenInBytex + StartHTML + 73;
        int EndFragment = StartFragment + htmlLenInBytex + 2;

        return Encoding.Default.GetBytes(
            $"""
                Version:1.0
                StartHTML:{StartHTML:00000000}
                EndHTML:{EndHTML:00000000}
                StartFragment:{StartFragment:00000000}
                EndFragment:{EndFragment:00000000}
                <HTML>
                <BODY>
                <!--StartFragment-->{htmlCode}
                <!--EndFragment-->
                </BODY>
                </HTML>
            """
            );
    }

    private byte[] GetHtmlBytesOfTable()
    {
        string _htmlCode = TableToHtml();
        return GetHtmlBytes(_htmlCode);
    }

    public CopyHtmlOrTextClipboard(TableOfSqlResults table)
    {
        _table = table;
    }

    private readonly string _htmlText;
    public CopyHtmlOrTextClipboard(string standardText, string htmlText)
    {
        _txt = standardText;
        _htmlText = htmlText;
    }

    public bool Contains(string dataFormat)
    {
        return formats.Contains(dataFormat);
    }

    public object? Get(string dataFormat)
    {
        if (_table is not null)
        {
            if (dataFormat == "HTML Format")
            {
                //var txt = Encoding.UTF8.GetString((byte[])bytes);                
                //int t4 = txt.IndexOf("<!--EndFragment-->");
                //var bytes = new  byte[] {86,101,114,115,105,111,110,58,48,46,57,13,10,83,116,97,114,116,72,84,77,76,58,48,48,48,48,48,48,48,49,54,51,13,10,69,110,100,72,84,77,76,58,48,48,48,48,48,48,48,55,56,54,13,10,83,116,97,114,116,70,114,97,103,109,101,110,116,58,48,48,48,48,48,48,48,49,57,57,13,10,69,110,100,70,114,97,103,109,101,110,116,58,48,48,48,48,48,48,48,55,53,48,13,10,83,111,117,114,99,101,85,82,76,58,104,116,116,112,115,58,47,47,119,119,119,46,119,51,115,99,104,111,111,108,115,46,99,111,109,47,104,116,109,108,47,104,116,109,108,95,116,97,98,108,101,115,46,97,115,112,13,10,60,104,116,109,108,62,13,10,60,98,111,100,121,62,13,10,60,33,45,45,83,116,97,114,116,70,114,97,103,109,101,110,116,45,45,62,60,115,112,97,110,32,115,116,121,108,101,61,34,99,111,108,111,114,58,32,114,103,98,40,48,44,32,48,44,32,48,41,59,32,102,111,110,116,45,102,97,109,105,108,121,58,32,97,114,105,97,108,44,32,115,97,110,115,45,115,101,114,105,102,59,32,102,111,110,116,45,115,105,122,101,58,32,49,53,112,120,59,32,102,111,110,116,45,115,116,121,108,101,58,32,110,111,114,109,97,108,59,32,102,111,110,116,45,118,97,114,105,97,110,116,45,108,105,103,97,116,117,114,101,115,58,32,110,111,114,109,97,108,59,32,102,111,110,116,45,118,97,114,105,97,110,116,45,99,97,112,115,58,32,110,111,114,109,97,108,59,32,102,111,110,116,45,119,101,105,103,104,116,58,32,55,48,48,59,32,108,101,116,116,101,114,45,115,112,97,99,105,110,103,58,32,110,111,114,109,97,108,59,32,111,114,112,104,97,110,115,58,32,50,59,32,116,101,120,116,45,97,108,105,103,110,58,32,108,101,102,116,59,32,116,101,120,116,45,105,110,100,101,110,116,58,32,48,112,120,59,32,116,101,120,116,45,116,114,97,110,115,102,111,114,109,58,32,110,111,110,101,59,32,119,104,105,116,101,45,115,112,97,99,101,58,32,110,111,114,109,97,108,59,32,119,105,100,111,119,115,58,32,50,59,32,119,111,114,100,45,115,112,97,99,105,110,103,58,32,48,112,120,59,32,45,119,101,98,107,105,116,45,116,101,120,116,45,115,116,114,111,107,101,45,119,105,100,116,104,58,32,48,112,120,59,32,98,97,99,107,103,114,111,117,110,100,45,99,111,108,111,114,58,32,114,103,98,40,50,53,53,44,32,50,53,53,44,32,50,53,53,41,59,32,116,101,120,116,45,100,101,99,111,114,97,116,105,111,110,45,116,104,105,99,107,110,101,115,115,58,32,105,110,105,116,105,97,108,59,32,116,101,120,116,45,100,101,99,111,114,97,116,105,111,110,45,115,116,121,108,101,58,32,105,110,105,116,105,97,108,59,32,116,101,120,116,45,100,101,99,111,114,97,116,105,111,110,45,99,111,108,111,114,58,32,105,110,105,116,105,97,108,59,32,100,105,115,112,108,97,121,58,32,105,110,108,105,110,101,32,33,105,109,112,111,114,116,97,110,116,59,32,102,108,111,97,116,58,32,110,111,110,101,59,34,62,67,111,110,116,97,99,116,60,47,115,112,97,110,62,60,33,45,45,69,110,100,70,114,97,103,109,101,110,116,45,45,62,13,10,60,47,98,111,100,121,62,13,10,60,47,104,116,109,108,62 };
                return _data ??= GetHtmlBytesOfTable();
            }
            else if (dataFormat == "Text")
            {
                return _txt ??= GetTextFromTable();
            }
        }
        else
        {
            if (dataFormat == "HTML Format")
            {
                return _data ??= GetHtmlBytes(_htmlText);
            }
            else if (dataFormat == "Text")
            {
                return _txt;
            }
        }
        //else if (dataFormat == "Unknown_Format_16")
        //{
        //    return new byte[] { 21, 4, 0, 0 };
        //}
        //else if (dataFormat == "Unknown_Format_7")
        //{
        //    return new byte[] { 67,111,110,116,97,99,116,0 };
        //}
        return null;
    }

    public IEnumerable<string> GetDataFormats()
    {
        return formats;
    }

    private string GetTextFromTable()
    {
        var rdr = new DBReaderWithMessagesTable(_table, null);

        using var stringWriter = new StringWriter();
        using (var csvWriter = CsvDataWriter.Create(stringWriter, new CsvDataWriterOptions()
        {
            NewLine = Environment.NewLine,
            Delimiter = '\t',
            WriteHeaders = true
        }))
        {
            csvWriter.Write(rdr);
        }
        return stringWriter.ToString();
    }
    public string? GetText()
    {
        return _txt ??= GetTextFromTable();
    }

    public IEnumerable<string>? GetFileNames()
    {
        throw new NotImplementedException();
    }
}
