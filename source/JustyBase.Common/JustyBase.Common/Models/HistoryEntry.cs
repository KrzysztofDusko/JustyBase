using System;

namespace JustyBase.Tools.Models;
public sealed class HistoryEntry
{
    public required DateTime Date { get; set; }
    public required string Database { get; set; }
    public required string Connection { get; set; }
    public required string SQL { get; set; }
    public DateTime RunDateTime => Date;
    public string SqlShort
    {
        get
        {
            var res = SQL.Length <= 150 ? SQL: SQL[..150];
            return res.ReplaceLineEndings(" ");
        }
    }
    public bool FiltrerRow(string searchTxt)
    {
        return string.IsNullOrEmpty(searchTxt) || SQL.Contains(searchTxt, StringComparison.OrdinalIgnoreCase);
    }
}