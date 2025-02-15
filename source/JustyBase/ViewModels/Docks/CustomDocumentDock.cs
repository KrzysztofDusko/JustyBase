using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using JustyBase.Common.Contracts;
using JustyBase.ViewModels.Documents;
using System.Linq;

namespace JustyBase.ViewModels.Docks;

public sealed class CustomDocumentDock : DocumentDock
{
    private readonly IGeneralApplicationData _generalApplicationData;
    public CustomDocumentDock()
    {
        _generalApplicationData = App.GetRequiredService<IGeneralApplicationData>();
        CreateDocument = new RelayCommand(CreateNewDocument);
    }

    private void CreateNewDocument()
    {
        if (!CanCreateDocument)
        {
            return;
        }

        var index = VisibleDockables?.Count + 1;

        string title = $"Document{index}";

        while (VisibleDockables.Select(x => x.Title.Trim('*')).Contains(title))
        {
            index++;
            title = $"Document{index}";
        }


        string docId = _generalApplicationData.AddNewDocument(title);
        int fontSize = _generalApplicationData.GetDocumentVmById(docId).FontSize;
        var document = App.GetRequiredService<SqlDocumentViewModel>();
        document.Id = docId;
        document.Title = title;
        document.FontSize = fontSize;

        if (this.ActiveDockable is SqlDocumentViewModel sqlDocumentViewModel)
        {
            document.SelectedConnectionIndex = sqlDocumentViewModel.SelectedConnectionIndex;
            document.SelectedDatabase = sqlDocumentViewModel.SelectedDatabase;
        }

        _generalApplicationData.GetDocumentVmById(docId).HotDocumentViewModel = document;

        Factory?.AddDockable(this, document);
        Factory?.SetActiveDockable(document);
        Factory?.SetFocusedDockable(this, document);
    }
}
