﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JustyBase.Services;

public interface IClipboardService
{
    Task<string> GetTextAsync();
    Task SetTextAsync(string txt);
    Task<object> GetDataAsync(string format);
    Task<string[]> GetFormatsAsync();
}
