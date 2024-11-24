using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchy;

public class FindWhiteCharBench
{
    private static readonly char[] _whiteCharsArray = ['\r', '\n', '\t', ' '];

    private static readonly SearchValues<char> _whiteChars = SearchValues.Create(_whiteCharsArray);

    [Params("1234456789 10111213", "123445678912344567891234456789123445678912344567891234456789 22222")]
    public string Text { get; set; }

    [Benchmark]
    public void FindByStandard()
    {
        Text.IndexOfAny(_whiteCharsArray);
    }

    [Benchmark]//much faster - as excepted
    public void FindBySearchValues()
    {
        Text.AsSpan().IndexOfAny(_whiteChars);
    }
}

