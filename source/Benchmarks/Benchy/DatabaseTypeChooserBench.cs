using BenchmarkDotNet.Attributes;
using JustyBase.Tools.ImportHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchy;

[MemoryDiagnoser]
public class DatabaseTypeChooserBench
{
    public string Path { get; set; } = @"D:\DEV\sqls\CsvReader\200kFile.csv";

    [Benchmark]
    public void Met1()
    {
        DatabaseTypeChooser databaseTypeChooser = new DatabaseTypeChooser();

        var excelReader = new CsvReader();
        excelReader.Open(Path, true, encoding: Encoding.UTF8);
        databaseTypeChooser.ExcelTypeDetection(excelReader, "xyz");
        excelReader.Dispose();
    }
}
