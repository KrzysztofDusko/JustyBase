using System;
using System.Collections.Generic;
using System.Text;

namespace JustyBase.Common.Models;
public enum SqlCommandState
{
    created,
    started,
    abortedP1,
    abortedP2,
    executed,
    finished
}
