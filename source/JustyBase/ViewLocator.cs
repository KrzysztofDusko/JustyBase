using Avalonia.Controls.Templates;
using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Core;
using JustyBase.Editor;
using JustyBase.Models.Tools;
using JustyBase.ViewModels.Documents;
using JustyBase.ViewModels.Tools;
using JustyBase.Views.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace JustyBase;

public class ViewLocator : IDataTemplate
{
    private static readonly Lock SyncFromRecycle = new();
    private static readonly Dictionary<object, Views.Documents.SqlDocumentView> DocumentViewCacheDictionary = [];
    private static readonly Dictionary<object, SqlResultsView> SqlResultsViewCacheDictionary = [];

    public Control Build(object dataViewModel)
    {
        switch (dataViewModel)
        {
            case SqlResultsViewModel when SqlResultsViewCacheDictionary.TryGetValue(dataViewModel, out var recycledInstance) && recycledInstance.Parent is null:
                return recycledInstance;
            case SqlResultsViewModel:
                {
                    var newInstance = new SqlResultsView();
                    lock (SyncFromRecycle)
                    {
                        SqlResultsViewCacheDictionary[dataViewModel] = newInstance;
                    }
                    return newInstance;
                }
            case SqlDocumentViewModel when DocumentViewCacheDictionary.TryGetValue(dataViewModel, out var recycledInstance):
                {
                    if (recycledInstance.Parent is null)
                    {
                        return recycledInstance;
                    }
                    Debug.Assert(false);
                    //object newInstance = Activator.CreateInstance(type);
                    Views.Documents.SqlDocumentView newInstance = new();

                    lock (SyncFromRecycle)
                    {
                        DocumentViewCacheDictionary[dataViewModel] = newInstance;
                    }

                    var destionationTextEditor = newInstance.Find<SqlCodeEditor>("SqlEditor");
                    var sourceTextExitor = recycledInstance.Find<SqlCodeEditor>("SqlEditor");

                    if (destionationTextEditor is null || sourceTextExitor is null)
                    {
                        return new TextBlock { Text = "Create Instance Failed: " + "destionationTextEditor or sourceTextExitor is null" };
                    }
                    AvaloniaEditExtensions.MakeSimillar(sourceTextExitor, destionationTextEditor);
                    return newInstance;
                }
            case SqlDocumentViewModel:
                {
                    object newInstance = new Views.Documents.SqlDocumentView();
                    lock (SyncFromRecycle)
                    {
                        DocumentViewCacheDictionary[dataViewModel] = (Views.Documents.SqlDocumentView)newInstance;
                    }
                    return (Control)newInstance;
                }
        }

        var name = dataViewModel.GetType().FullName?.Replace("ViewModel", "View");
        if (name is null)
        {
            return new TextBlock { Text = "Invalid Data Type" };
        }

        var type = Type.GetType(name);
        if (type is null) return new TextBlock { Text = "Not Found: " + name };
        object instance = Activator.CreateInstance(type);
        if (instance is DbSchemaModel) // https://github.com/KrzysztofDusko/JustyBase/issues/242
        {
            return new TextBox
            {
                [!TextBox.TextProperty] = new Binding(nameof(DbSchemaModel.Name)),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        if (instance is not null)
        {
            return (Control)instance;
        }
        return new TextBlock { Text = "Create Instance Failed: " + type.FullName };

    }

    public static void RemoveFromCache(IDockable dock)
    {
        lock (SyncFromRecycle)
        {
            DocumentViewCacheDictionary.Remove(dock);
            SqlResultsViewCacheDictionary.Remove(dock);
        }
    }

    public bool Match(object data)
    {
        //return data is ReactiveObject || data is IDockable;
        return data is ObservableObject or IDockable;
    }
}