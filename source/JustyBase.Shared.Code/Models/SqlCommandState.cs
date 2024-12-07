using System;
using System.Collections.Generic;
using System.Text;

namespace JustyBase.Shared.Models;
internal enum SqlCommandState
{
    created,
    started,
    abortedP1,
    abortedP2,
    executed,
    finished
}
