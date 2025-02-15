using Avalonia.Collections;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Common.Contracts;
using JustyBase.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JustyBase.ViewModels.Documents;

public sealed partial class ImportViewModel : DocumentBaseVM
{
    private static IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    public ImportViewModel(IAvaloniaSpecificHelpers avaloniaSpecificHelpers, IGeneralApplicationData generalApplicationData, IMessageForUserTools messageForUserTools)
    {
        _avaloniaSpecificHelpers = avaloniaSpecificHelpers;
        _generalApplicationData = generalApplicationData;
        _messageForUserTools = messageForUserTools;
        Title = "Import";
        TabsWarningMessage = "";
        StartEnabled = true;
        OpenFileForImportCommand = new AsyncRelayCommand(async () =>
        {
            IReadOnlyList<IStorageFile> openFile = await ChoseFile();
            if (openFile.Count == 1)
            {
                await OpenMethod(openFile[0].Path.LocalPath);
            }
        });

        ImportItemCollections = [];
        ImportItems = new DataGridCollectionView(ImportItemCollections);

        DatabaseItems = [];
        SchemaItems = [];
        TableItems = [];

        _dispatcherTimer.Tick += DispatcherTimer_Tick;
    }

    private static async Task<IReadOnlyList<IStorageFile>> ChoseFile()
    {
        return await _avaloniaSpecificHelpers.GetStorageProvider().OpenFilePickerAsync(
            new FilePickerOpenOptions()
            {
                AllowMultiple = false,
                FileTypeFilter = new FilePickerFileType[]
                {
                    new("common files") { Patterns = ["*.xlsx;*.xlsb;*.csv;*.csv.br;*.dat.br;*.csv.gz;*.dat.gz;*.csv.zst;*.dat.zst"] } ,
                    new("all files") { Patterns = ["*"] }
                }
            });
    }

    private void DispatcherTimer_Tick(object? sender, EventArgs e)
    {
        lock (this)
        {
            foreach (var importItem in _importsInProgress)
            {
                importItem.Elapsed = (DateTime.Now - importItem.StartTime).ToString(@"hh\:mm\:ss");
            }
        }
        ImportItems.Refresh();
    }

    public DataGridCollectionView ImportItems { get; set; }

    private readonly DispatcherTimer _dispatcherTimer = new()
    {
        Interval = TimeSpan.FromSeconds(0.5)
    };
}