using System.Buffers;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using JustyBase.PluginCommon.Enums;

namespace JustyBase.Common.Tools;

public static class CompresionExt
{
    public static CompressionEnum GetCsvCompressionEnum(this string str)
    {
        if (str.EndsWith(".br", StringComparison.OrdinalIgnoreCase))
        {
            return CompressionEnum.Brotli;
        }
        else if (str.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            return CompressionEnum.Gzip;
        }
        else if (str.EndsWith(".zst", StringComparison.OrdinalIgnoreCase))
        {
            return CompressionEnum.Zstd;
        }
        else if (str.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return CompressionEnum.Zip;
        }
        else
        {
            return CompressionEnum.None;
        }
    }
}

public static class AdHocCompressionHelper
{
    public static async Task Extract(string path, Action<long, long> showProgressAction)
    {
        await Extract(path, showProgressAction, path.GetCsvCompressionEnum());
    }

    public static async Task Compress(string path, string mode, Action<long, long> showProgress)
    {
        if (mode == "zst" || mode == "br" || mode == "gz")
        {
            await Task.Run(async () =>
            {

                if (!File.Exists(path) && Directory.Exists(path))
                {
                    await CompressDir(path, mode, showProgress);
                    return;
                }

                Stream streamToCompress = File.OpenRead(path);
                FileStream newFileStream = File.Create(path + $".{mode}");
                long oldFileLen = streamToCompress.Length;

                Stream? compressedStream = null;
                try
                {
                    if (mode == "zst")
                    {
                        compressedStream = new ZstdSharp.CompressionStream(newFileStream);
                    }
                    else if (mode == "br")
                    {
                        compressedStream = new BrotliStream(newFileStream, CompressionMode.Compress);
                    }
                    else if (mode == "gz")
                    {
                        compressedStream = new GZipStream(newFileStream, CompressionMode.Compress);
                    }

                    if (compressedStream is not null)
                    {
                        byte[] buffer = ArrayPool<byte>.Shared.Rent(8_192);
                        try
                        {
                            long procesedBytes = 0;
                            int len;
                            while ((len = streamToCompress.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                compressedStream.Write(buffer, 0, len);
                                procesedBytes += len;
                                showProgress?.Invoke(procesedBytes, oldFileLen);
                            }
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
                finally
                {
                    newFileStream?.Dispose();
                    streamToCompress?.Dispose();
                    compressedStream?.Dispose();
                }
            });
        }
    }

    private static async Task CompressDir(string path, string mode, Action<long, long> showProgress)
    {
        if (mode == "zst" || mode == "br" || mode == "gz")
        {
            await Task.Run(() =>
            {

                FileStream newFileStream = File.Create(path + $".tar.{mode}");
                Stream? compressedStream = null;
                showProgress(50, 100);
                try
                {
                    if (mode == "zst")
                    {
                        compressedStream = new ZstdSharp.CompressionStream(newFileStream);
                    }
                    else if (mode == "br")
                    {
                        compressedStream = new BrotliStream(newFileStream, CompressionMode.Compress);
                    }
                    else if (mode == "gz")
                    {
                        compressedStream = new GZipStream(newFileStream, CompressionMode.Compress);
                    }
                    if (compressedStream is not null)
                    {
                        TarFile.CreateFromDirectory(path, compressedStream, true);
                    }
                }
                finally
                {
                    compressedStream?.Dispose();
                    showProgress(100, 100);
                }
            });
        }
    }

    public static async Task Extract(string path, Action<long, long> showProgress, CompressionEnum compression)
    {
        if (path.EndsWith(".tar.br", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".tar.zst", StringComparison.OrdinalIgnoreCase))
        {
            await ExtractToDir(path, showProgress, compression);
            return;
        }

        await Task.Run(() =>
        {
            using var fs = File.OpenRead(path);
            long fileLength = fs.Length;
            Stream? compressedStream = null;
            try
            {
                switch (compression)
                {
                    case CompressionEnum.Zstd:
                        compressedStream = new ZstdSharp.DecompressionStream(new BufferedStream(fs));
                        break;
                    case CompressionEnum.Brotli:
                        compressedStream = new BrotliStream(new BufferedStream(fs), CompressionMode.Decompress);
                        break;
                    case CompressionEnum.Gzip:
                        compressedStream = new GZipStream(new BufferedStream(fs), CompressionMode.Decompress);
                        break;
                }

                string newPath = "";
                if (path.EndsWith(".br", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                {
                    newPath = path[0..^3];
                }
                else if (path.EndsWith(".zst", StringComparison.OrdinalIgnoreCase))
                {
                    newPath = path[0..^4];
                }
                else
                {
                    newPath = path + "_extracted";
                }

                if (compressedStream is not null)
                {
                    using var sw = File.OpenWrite(newPath);
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(8_192);
                    try
                    {
                        int readed = 0;
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        while ((readed = compressedStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            sw.Write(buffer, 0, readed);
                            if (stopwatch.ElapsedMilliseconds >= 200)
                            {
                                showProgress(fs.Position, fileLength);
                                stopwatch.Restart();
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
            }
            finally
            {
                compressedStream?.Dispose();
            }
        });
    }

    private static async Task ExtractToDir(string path, Action<long, long> showProgress, CompressionEnum compression)
    {
        await Task.Run(() =>
        {
            using var fs = File.OpenRead(path);
            long fileLength = fs.Length;
            Stream? compressedStream = null;
            try
            {
                switch (compression)
                {
                    case CompressionEnum.Zstd:
                        compressedStream = new ZstdSharp.DecompressionStream(new BufferedStream(fs));
                        break;
                    case CompressionEnum.Brotli:
                        compressedStream = new BrotliStream(new BufferedStream(fs), CompressionMode.Decompress);
                        break;
                    case CompressionEnum.Gzip:
                        compressedStream = new GZipStream(new BufferedStream(fs), CompressionMode.Decompress);
                        break;
                }
                showProgress(50, 100);
                string newPath = "";
                if (path.EndsWith(".tar.br", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
                {
                    newPath = path[0..^7];
                }
                else if (path.EndsWith(".tar.zst", StringComparison.OrdinalIgnoreCase))
                {
                    newPath = path[0..^8];
                }

                if (compressedStream is not null && path is not null)
                {
                    var dn = Path.GetDirectoryName(path);
                    if (dn is not null)
                    {
                        TarFile.ExtractToDirectory(compressedStream, dn, false);
                    }
                    showProgress(100, 100);
                }
            }
            finally
            {
                compressedStream?.Dispose();
            }
        });
    }
}
