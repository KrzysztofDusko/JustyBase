using Avalonia.Controls;
using Avalonia.Input;
using JustyBase.Services;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System;
using JustyBase.Database.Sample.ViewModels;

namespace JustyBase.Database.Sample.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DragEffects & (DragDropEffects.Link);
        if (!e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DragEffects & (DragDropEffects.Link);
        
        if (e.Data.Contains(DataFormats.Files))
        {
            if (this.DataContext is not null)
            {
                var filenameX = e.Data.GetFiles();
                if (filenameX is not null)
                {
                    try
                    {
                        List<string> filenamesToOpen = filenameX.Select(o => o.Path.LocalPath).ToList();
                        foreach (var filePath in filenamesToOpen)
                        {
                            (this.DataContext as MainWindowViewModel)?.ImportFromPath(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (this.DataContext is MainWindowViewModel mainWindowViewModel)
                        {
                            mainWindowViewModel.Info += ex.Message;
                        }
                    }
                }
            }
        }
    }
}