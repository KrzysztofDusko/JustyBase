
using System;
using System.Data.Common;
using System.Threading.Tasks;
using System.Data.Odbc;
using JustyBase.Helpers.Importers;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
#if ORACLE
using Oracle.ManagedDataAccess.Client;
#endif

namespace JustyBase.Database.Sample.Services;

internal sealed class ItemToImportService : IDatabaseWithSpecificImportService
{
    public DatabaseTypeEnum DatabaseType { get; init; }
    public ItemToImportService(DbConnection connection, string tempDir, DatabaseTypeEnum databaseType)
    {
        _connection = connection;
        _tempDir = tempDir;
        DatabaseType = databaseType;
    }
    
    private readonly DbConnection _connection;
    private readonly string _tempDir;


    public async Task DbSpecificImportPart(IDbImportJob importJob, string randName, Action<string>? progress, bool tableExists = false)
    {
#if ORACLE
        if (DatabaseType == DatabaseTypeEnum.Oracle && _connection is OracleConnection oracleConnection)
        {
            await Task.Run(async () =>
            {
                oracleConnection.Open();
                await OracleImportHelper.OracleImportExecute(oracleConnection, importJob, randName, progress, tableExists);
                oracleConnection.Close();
            });
        }
        else 
#endif
        if (_connection is null)
        {
            throw new NullReferenceException();
        }
        if (DatabaseType == DatabaseTypeEnum.NetezzaSQLOdbc || DatabaseType == DatabaseTypeEnum.NetezzaSQL)
        {
            await _connection.OpenAsync();
            if (_connection is OdbcConnection odbcConnection)
            {
                odbcConnection.InfoMessage += (sender, args) =>
                {
                    progress?.Invoke(args.Message);
                };
            }
            await NetezzaImportHelper.NetezzaImportExecute(_connection, _tempDir, importJob, randName, progress,
                DatabaseType == DatabaseTypeEnum.NetezzaSQLOdbc? "odbc" : "dotnet");
            progress?.Invoke("database processing...");
            await _connection.CloseAsync();
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}
