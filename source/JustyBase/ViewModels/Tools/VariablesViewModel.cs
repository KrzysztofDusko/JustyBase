using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using JustyBase.Common.Contracts;
using JustyBase.Models.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JustyBase.ViewModels.Tools;

public partial class VariablesViewModel : Tool
{
    public ObservableCollection<VariableModel> VariableList { get; set; } = [];

    private static readonly Dictionary<string, Func<string>> FixedVariables = new()
    {
        { "&yesterday", () => $"'{DateTime.Today.AddDays(-1):yyyy-MM-dd}'" },
        { "&yesterday_id", () => $"{DateTime.Today.AddDays(-1):yyyyMMdd}" },
        { "&today", () => $"'{DateTime.Today:yyyy-MM-dd}'" },
        { "&today_id", () => $"'{DateTime.Today:yyyyMMdd}'" },
        { "&now", () => $"'{DateTime.Now:yyyy-MM-dd HH:mm:ss}'" },
        { "&now_utc", () => $"'{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}'" },
        { "&prev_month_last_day", () => $"'{DateTime.Today.AddDays(-DateTime.Today.Day):yyyy-MM-dd}'" },
        { "&prev_month_last_day_id", () => $"{DateTime.Today.AddDays(-DateTime.Today.Day):yyyyMMdd}" }
    };

    public VariablesViewModel(IFactory factory)
    {
        Factory = factory;
        foreach (var item in FixedVariables)
        {
            VariableList.Add(new VariableModel { VariableName = item.Key, ComputeVariableValueFunc = item.Value, VariableComputedValue = null });
        }
        RefreshAllVariables();
        Task.Run(VariableRefresh);
    }

    [ObservableProperty]
    public partial VariableModel SelectedVariable { get; set; }

    public void RemoveSelectedVariable()
    {
        RemoveVariable(SelectedVariable);
        UpdateVariablesCompletition();
    }


    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(15));

    private void RefreshAllVariables()
    {
        foreach (var item in VariableList)
        {
            item.RefreshValue();
        }
    }

    private async Task VariableRefresh()
    {
        while (await _periodicTimer.WaitForNextTickAsync())
        {
            RefreshAllVariables();
        }
        UpdateVariablesCompletition();
    }

    private DockFactory ActualDockFactory => Factory as DockFactory;
    public void DataGridDoubleClicked()
    {
        ActualDockFactory?.InsertTextToActiveDocument(SelectedVariable.VariableName, true);
    }

    public void AddVariableFromEditorOrByPlus(string variableName, string variableValue)
    {
        lock (_lock)
        {
            variableName = $"&{variableName}";
            var foundedVariable = VariableList.FirstOrDefault(x => x.VariableName.Equals(variableName, StringComparison.OrdinalIgnoreCase));
            if (foundedVariable is not null)
            {
                foundedVariable.VariableName = variableName;// to update name case if it was changed
                foundedVariable.VariableComputedValue = variableValue;
                foundedVariable.ComputeVariableValueFunc = null;
            }
            else
            {
                VariableList.Add(new VariableModel { VariableName = variableName, ComputeVariableValueFunc = null, VariableComputedValue = variableValue });
            }
            UpdateVariablesCompletition();
        }
    }

    private void RemoveVariable(VariableModel variable)
    {
        lock (_lock)
        {
            VariableList.Remove(variable);
            UpdateVariablesCompletition();
        }
    }

    private readonly Lock _lock = new();
    public Dictionary<string, string> UpdateVariablesCompletition()
    {
        var dictionary = App.GetRequiredService<IGeneralApplicationData>().VariablesDictionary;
        dictionary.Clear();
        foreach (var item in VariableList)
        {
            dictionary.TryAdd(item.VariableName, item.VariableComputedValue);
        }
        return dictionary;
    }
}
