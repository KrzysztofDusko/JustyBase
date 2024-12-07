using System.Data.Common;

namespace JustyBase.PluginDatabaseBase.Database;

public interface INetezzaDotnet
{
    Task DropConnectionEmergencyModeAsync(DbConnection dbConnection);
    void OptimizeCommandBuffer(DbCommand cmd, bool noStringReturn = true);
}
