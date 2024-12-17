using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommon.Contracts;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;

namespace JustyBase.Helpers.Importers;

public sealed class OracleImportHelper
{
    public static async Task OracleImportExecute(OracleConnection oracleConnection, IDbImportJob importJob, string tableName, Action<string>? progress, bool tableExists = false)
    {
        await Task.Run(() =>
        {
            if (!tableExists)
            {
                string[] headers = importJob.ReturnHeadersWithDataTypes(DatabaseTypeEnum.Oracle);
                string SQL = $"CREATE TABLE {tableName} ({string.Join(',', headers)})";
                using DbCommand cmd = oracleConnection.CreateCommand();
                cmd.CommandText = SQL;
                cmd.ExecuteNonQuery();
            }

            using OracleBulkCopy cpy = new(oracleConnection);
            cpy.BulkCopyTimeout = 3_600;
            cpy.NotifyAfter = 10_000;
            cpy.OracleRowsCopied += (o, e) => progress?.Invoke($"Copied {e.RowsCopied:N0}");
            cpy.DestinationTableName = tableName;
            cpy.WriteToServer(importJob.AsReader);

            cpy.Close();
        });
    }
}
