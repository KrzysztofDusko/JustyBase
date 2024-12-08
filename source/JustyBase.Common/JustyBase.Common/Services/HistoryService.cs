using JustyBase.Tools.Models;

namespace JustyBase.Common.Services;

public sealed class HistoryService
{
    private readonly IGeneralApplicationData _generalApplicationData;

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

    public HistoryService(IGeneralApplicationData generalApplicationData)
    {
        _generalApplicationData = generalApplicationData;
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
            _historyEntries = new List<HistoryEntry>();

            using var fs = new FileStream(IGeneralApplicationData.HistoryDatFilePath, FileMode.OpenOrCreate, FileAccess.Read);
            using (ZstdSharp.DecompressionStream decompressionStream = new ZstdSharp.DecompressionStream(fs, leaveOpen: false))
            {
                using (var binaryReader = new BinaryReader(decompressionStream/*fs*/, encoding: System.Text.Encoding.UTF8, leaveOpen: false))
                {
                    //while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
                    while (true)
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
    }

    public void AddHistoryEntry(string sql, string baza, string connectioName)
    {
        lock (_sync)
        {
            var currentDateTime = DateTime.Now;
            using (var compressionStream = new ZstdSharp.CompressionStream(File.Open(IGeneralApplicationData.HistoryDatFilePath, FileMode.Append, FileAccess.Write), leaveOpen: false))
            {
                using (var binaryWriter = new BinaryWriter(compressionStream, encoding: System.Text.Encoding.UTF8))
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
