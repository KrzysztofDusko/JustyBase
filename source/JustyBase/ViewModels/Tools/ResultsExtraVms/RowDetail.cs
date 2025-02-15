using System;
using System.Collections.Generic;

namespace JustyBase.ViewModels.Tools;

public sealed class RowDetail
{
    public string Name { get; set; }

    public bool IsColumnVisible
    {
        get;
        set
        {
            field = value;
            ChangeColVisiblity?.Invoke();
        }
    } = true;
    public List<string> FieldsValues { get; set; }
    public string TypeName { get; set; }
    public Action ChangeColVisiblity { get; set; }
}

