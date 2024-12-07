using System;

namespace JustyBase.Models.Tools;

public enum LogMessageType
{
    ok,
    warning,
    error,
    inProgress
}
public sealed class StringPair
{
    public required DateTime PairTitle { get; set; }
    public required string PairMessage { get; set; }
}

