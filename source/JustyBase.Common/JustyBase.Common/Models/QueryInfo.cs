﻿using JustyBase.Shared.Models;
using System.Collections.Generic;
using System.Data.Common;

namespace JustyBase.Models.Tools;
public sealed class QueryInfo
{
    public bool FullFinish { get; set; }
    public Dictionary<DbCommand, SqlCommandState> DbCommands { get; set; }
}
