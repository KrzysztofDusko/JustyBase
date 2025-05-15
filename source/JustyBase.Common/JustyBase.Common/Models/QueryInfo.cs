using System.Data.Common;

namespace JustyBase.Common.Models;
public sealed class QueryInfo
{
    public bool FullFinish { get; set; }
    public Dictionary<DbCommand, SqlCommandState> DbCommands { get; set; }
}
