using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Benchy;
using JustyBase.Tools.ImportHelpers;
using Parquet.Schema;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Text;

CreateCleanSqlBench createCleanSqlBench = new CreateCleanSqlBench();

#if DEBUG
foreach (var item in createCleanSqlBench.StringsToTest)
{
    createCleanSqlBench.ActualString = item;
    var std = createCleanSqlBench.CreateCleanSql();
    var better = createCleanSqlBench.CreateCleanSqlBetter();
    var betterv2 = createCleanSqlBench.CreateCleanSqlBetterV2();
    var betterv3 = createCleanSqlBench.CreateCleanSqlBetterV3();
    Debug.Assert(std == better);
    Debug.Assert(std == betterv2);
    Debug.Assert(std == betterv3);
}
foreach (var item in Directory.GetFiles("D:\\DEV\\sqls\\", "*.sql", SearchOption.AllDirectories))
{
    createCleanSqlBench.ActualString = File.ReadAllText(item);
    var std = createCleanSqlBench.CreateCleanSql();
    var better = createCleanSqlBench.CreateCleanSqlBetter();
    var betterv2 = createCleanSqlBench.CreateCleanSqlBetterV2();
    var betterv3 = createCleanSqlBench.CreateCleanSqlBetterV3();
    Debug.Assert(std == better);
    Debug.Assert(std == betterv2);
    Debug.Assert(std == betterv3);
}
#endif

_ = BenchmarkRunner.Run<CreateCleanSqlBench>();

Console.WriteLine("done");
