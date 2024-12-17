using K4os.Compression.LZ4.Streams;
using System.Buffers;
using System.Formats.Tar;
using System.Text;

namespace JustyBase.Common.Helpers;

public sealed class OtherHelpers
{

    private static readonly List<string> _pluginsList = ["SqlitePlugin", "DuckDBPlugin", "MySqlPlugin",
            "PostgresPlugin", "OraclePlugin", "NetezzaDotnetPlugin", "DB2Plugin" ];
    public async Task DownloadAllPlugins(string pluginDirectory, string downloadBasePath)
    {
        if (Directory.Exists(pluginDirectory))
        {
            Directory.Delete(pluginDirectory, true);
        }
        Directory.CreateDirectory(pluginDirectory);
        HttpClient httpClientToDownload = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        foreach (var driverName in _pluginsList)
        {
            using var response = await httpClientToDownload.GetAsync($"{downloadBasePath}{driverName}.tar.lz4");
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var source = LZ4Stream.Decode(stream))
            using (var target = File.Create($@"{pluginDirectory}\{driverName}.tar"))
            {
                await source.CopyToAsync(target);
            }
            await TarFile.ExtractToDirectoryAsync($@"{pluginDirectory}\{driverName}.tar", pluginDirectory, true);
            File.Delete($@"{pluginDirectory}\{driverName}.tar");
        }
    }

    public async Task DownloadFileWithReverse(string remoteFilePath, string pathToSave)
    {
        HttpClient httpClientToDownload = new()
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        using (var response = await httpClientToDownload.GetAsync(remoteFilePath, HttpCompletionOption.ResponseHeadersRead))
        {
            var contentLength = (int)response.Content.Headers.ContentLength.Value;
            byte[] arr = ArrayPool<byte>.Shared.Rent(contentLength);
            int position = 0;

            using (var download = await response.Content.ReadAsStreamAsync())
            {
                while (position < contentLength)
                {
                    int toRead = contentLength - position < 32_768 ? contentLength - position : 32_768;
                    int readed = download.Read(arr, position, toRead);
                    position += readed;
                }
            }

            FinishDownload(arr, position);
            ArrayPool<byte>.Shared.Return(arr);
        }

        void FinishDownload(byte[] arr, int length)
        {
            var sp = arr.AsSpan()[..length];
            sp.Reverse();
            using var file = File.Create(pathToSave);
            file.Write(sp);
        }
    }

    public string CsvTxtPreviewer(string path)
    {
        using var binaryReader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read));

        int readed = 0;
        string res = "";
        if (binaryReader.BaseStream.CanSeek)
        {
            long fileLength = binaryReader.BaseStream.Length;

            if (fileLength <= 65_536)
            {
                res = File.ReadAllText(path);
            }
            else
            {
                char[] buffer = new char[32_768];
                binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
                readed = binaryReader.ReadBlock(buffer, 0, buffer.Length);

                StringBuilder sb = new StringBuilder();
                sb.Append(new string('=', 25));
                sb.Append("File cut off to 65 KB");
                sb.AppendLine(new string('=', 25));
                sb.Append(buffer.AsSpan()[..readed]);
                sb.AppendLine();
                string rep = new string('=', 100);
                sb.AppendLine(rep);
                sb.Append(new string('=', 25));
                sb.Append("File cut off to 65 KB");
                sb.AppendLine(new string('=', 25));
                sb.AppendLine(rep);

                binaryReader.BaseStream.Seek(binaryReader.BaseStream.Length - buffer.Length, SeekOrigin.Begin);
                sb.Append(binaryReader.ReadToEnd());
                res = sb.ToString();
            }
        }

        return res;
    }
}
