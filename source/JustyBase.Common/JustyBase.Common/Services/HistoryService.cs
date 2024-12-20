using JustyBase.Common.Contracts;
using JustyBase.Common.Models;

namespace JustyBase.Common.Services;

public sealed class HistoryService(IGeneralApplicationData generalApplicationData)
{
    private readonly IGeneralApplicationData _generalApplicationData = generalApplicationData;

    private List<HistoryEntry>? _historyEntries = null;
    public List<HistoryEntry>? HistoryItemsCollection
    {
        get
        {
            if (_historyEntries is null)
            {
                LoadFromFileToList();
            }
            return _historyEntries;
        }
    }

    private bool Loaded => _historyEntries is not null;
    private readonly Lock _sync = new();
    private void LoadFromFileToList()
    {
        lock (_sync)
        {
            if (Loaded) // load only one time
            {
                return;
            }
            _historyEntries = [];

            using var fs = new FileStream(IGeneralApplicationData.HistoryDatFilePath, FileMode.OpenOrCreate, FileAccess.Read);
            using (var binaryReader = new BinaryReader(fs, encoding: System.Text.Encoding.UTF8, leaveOpen: false))
            {
                while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
                {
                    try
                    {
                        var logDateTime = DateTime.FromBinary(binaryReader.ReadInt64());
                        var sql = binaryReader.ReadString();
                        var database = binaryReader.ReadString();
                        var connectioName = binaryReader.ReadString();
                        if (logDateTime >= DateTime.Now.AddMonths(-_generalApplicationData.Config.LimitHistoryMonths))
                        {
                            HistoryItemsCollection.Add(new HistoryEntry()
                            {
                                Date = logDateTime,
                                Database = database,
                                Connection = connectioName,
                                SQL = sql
                            });
                        }
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
        }
    }

    public void AddHistoryEntry(string sql, string baza, string connectioName)
    {
        lock (_sync)
        {
            var currentDateTime = DateTime.Now;
            using (var fs = File.Open(IGeneralApplicationData.HistoryDatFilePath, FileMode.Append, FileAccess.Write))
            {
                using (var binaryWriter = new BinaryWriter(fs, encoding: System.Text.Encoding.UTF8))
                {
                    binaryWriter.Write(currentDateTime.ToBinary());
                    binaryWriter.Write(sql);
                    binaryWriter.Write(baza);
                    binaryWriter.Write(connectioName);
                }
            }

            if (Loaded)
            {
                HistoryItemsCollection.Add(new HistoryEntry()
                {
                    Date = currentDateTime,
                    Database = baza,
                    Connection = connectioName,
                    SQL = sql
                });
            }
        }
    }
}
