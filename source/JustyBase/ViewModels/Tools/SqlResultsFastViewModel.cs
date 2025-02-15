using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using JustyBase.Common.Contracts;
using JustyBase.ViewModels.Documents;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JustyBase.ViewModels.Tools;

public partial class SqlResultsFastViewModel : Tool
{
    private readonly IGeneralApplicationData _generalApplicationData;
    private readonly IMessageForUserTools _messageForUserTools;
    public SqlResultsFastViewModel(IFactory factory, IGeneralApplicationData generalApplicationData, IMessageForUserTools messageForUserTools)
    {
        this.Factory = factory;
        _generalApplicationData = generalApplicationData;
        _messageForUserTools = messageForUserTools;

    }

    public ObservableCollection<SqlResultsViewModel> SqlResultsViewModels { get; } = [];
    public ObservableCollection<SqlResultsFastTile> SqlResultsTitles { get; } = [];

    private readonly Dictionary<string, string> _lastVisibleResultForSql = [];


    [ObservableProperty]
    public partial bool IsTabStripEnabled { get; set; } = true;

    public void Add(SqlResultsViewModel sqlResults, SqlDocumentViewModel sqlDocumentViewModel, bool makeVisible)
    {
        IsTabStripEnabled = false;
        _messageForUserTools.DispatcherActionInstance(() =>
        {
            _lastVisibleResultForSql[sqlDocumentViewModel.Id] = sqlResults.Id;
            SqlResultsViewModels.Add(sqlResults);
            sqlResults.IsResultVisible = makeVisible;
            var tb = new SqlResultsFastTile(x => this.RemoveOneResult(x))
            {
                ResTitle = sqlResults.Title,
                ReferencedSqlResult = sqlResults,
                ParentDocument = sqlDocumentViewModel,
                IsTitleVisible = makeVisible
            };
            SqlResultsTitles.Add(tb);
            if (makeVisible)
            {
                SelectedTabIndex = SqlResultsTitles.Count - 1;
            }
        });
        IsTabStripEnabled = true;
    }

    private readonly object _sync = new();
    public IEnumerable<SqlResultsViewModel> GetDocumentResults(SqlDocumentViewModel sqlDocumentViewModel)
    {
        lock (_sync)///???
        {
            foreach (var item in SqlResultsTitles)
            {
                if (item.ParentDocument == sqlDocumentViewModel)
                {
                    yield return item.ReferencedSqlResult;
                }
            }
        }
    }


    public void ClearFromDocument(string documentId, bool clearDockedAlso)
    {
        if (clearDockedAlso)
        {
            _generalApplicationData.Config.DoGcCollect = false;
        }

        _messageForUserTools.DispatcherActionInstance(() =>
        {
            for (int i = 0; i < SqlResultsTitles.Count; i++)
            {
                var dc = SqlResultsTitles[i];
                if (dc.ParentDocument.Id == documentId && (clearDockedAlso || !dc.ReferencedSqlResult.IsDocked))
                {
                    dc.ReferencedSqlResult.IsResultVisible = false;
                    ViewLocator.RemoveFromCache(dc.ReferencedSqlResult);
                    dc.ReferencedSqlResult.DoCleanup();
                    dc.ReferencedSqlResult.OnClose();
                    SqlResultsTitles.RemoveAt(i);
                    SqlResultsViewModels.Remove(dc.ReferencedSqlResult);
                    i--;
                }
            }
        });
        if (clearDockedAlso)
        {
            _generalApplicationData.Config.DoGcCollect = true;
        }
    }

    public void RemoveOneResult(string resultId)
    {
        _messageForUserTools.DispatcherActionInstance(() =>
        {
            for (int i = 0; i < SqlResultsTitles.Count; i++)
            {
                var dc = SqlResultsTitles[i];
                if (dc.ReferencedSqlResult.Id == resultId)
                {
                    dc.ReferencedSqlResult.IsResultVisible = false;
                    ViewLocator.RemoveFromCache(dc.ReferencedSqlResult);
                    dc.ReferencedSqlResult.DoCleanup();
                    dc.ReferencedSqlResult.OnClose();
                    SqlResultsTitles.RemoveAt(i);
                    SqlResultsViewModels.Remove(dc.ReferencedSqlResult);
                    int ind = SqlResultsTitles.Count - 1;
                    if (ind >= 0)
                    {
                        SelectedTabIndex = ind;
                    }
                    break;
                }
            }
        });
    }


    private SqlDocumentViewModel _prevResultsDocumentId;

    public void ShowDocumentResult(SqlDocumentViewModel sqlDocumentViewModel)
    {
        if (_prevResultsDocumentId == sqlDocumentViewModel)
        {
            return; // do nothing
        }
        _prevResultsDocumentId = sqlDocumentViewModel;
        //_messageForUserTools.DispatcherActionInstance(() =>
        //{
        int nm = 0;
        foreach (SqlResultsFastTile item in SqlResultsTitles)
        {
            if (item.ParentDocument == sqlDocumentViewModel)
            {
                item.IsTitleVisible = true;
                item.ReferencedSqlResult.IsResultVisible = false;
                if (_lastVisibleResultForSql.TryGetValue(sqlDocumentViewModel.Id, out var tmp1) && tmp1 == item.ReferencedSqlResult.Id)
                {
                    item.ReferencedSqlResult.IsResultVisible = true;
                    if (SelectedTabIndex != nm)
                    {
                        SelectedTabIndex = nm;
                    }
                }
                //else
                //{
                //    item.ReferencedSqlResult.IsResultVisible = false;
                //}
            }
            else
            {
                item.IsTitleVisible = false;
                item.ReferencedSqlResult.IsResultVisible = false;
            }
            nm++;
        }
        //});
    }

    public void HideAllResult()
    {
        //_messageForUserTools.DispatcherActionInstance(() =>
        //{
        foreach (var item in SqlResultsTitles)
        {
            item.IsTitleVisible = false;
            item.ReferencedSqlResult.IsResultVisible = false;
        }
        //});
    }

    public int SelectedTabIndex
    {
        get;
        set
        {
            if (value >= SqlResultsTitles.Count)
            {
                return;
            }
            SetProperty(ref field, value);
            if (SelectedTabIndex != -1)
            {
                for (int i = 0; i < SqlResultsViewModels.Count; i++)
                {
                    SqlResultsViewModels[i].IsResultVisible = false;
                }
                if (SelectedTabIndex < SqlResultsTitles.Count)
                {
                    try
                    {
                        var tmpObj = SqlResultsTitles[SelectedTabIndex];
                        var tmpRes = tmpObj.ReferencedSqlResult;
                        var tmpSql = tmpObj.ParentDocument;
                        _lastVisibleResultForSql[tmpRes.RelatedSqlDocumentId] = tmpRes.Id;
                        if ((this.Factory as DockFactory).IsActiveDockable(tmpSql) && !tmpRes.IsResultVisible)
                        {
                            tmpRes.IsResultVisible = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
                    }
                }
            }
        }
    } = 0;
}


public partial class SqlResultsFastTile : ObservableObject
{

    private readonly Action<string> _removeOneResult;
    public SqlResultsFastTile(Action<string> removeOneResult)
    {
        _removeOneResult = removeOneResult;
    }

    public string ResTitle { get; set; }
    public SqlResultsViewModel ReferencedSqlResult { get; set; }
    public SqlDocumentViewModel ParentDocument { get; set; }

    [ObservableProperty]
    public partial bool IsTitleVisible { get; set; } = true;

    [RelayCommand]
    private void RemoveResult()
    {
        _removeOneResult(ReferencedSqlResult.Id);
    }

    [RelayCommand]
    private void DockUndockResult()
    {
        ReferencedSqlResult.IsDocked = !ReferencedSqlResult.IsDocked;
    }

}