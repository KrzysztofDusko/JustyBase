using Dock.Model.Core;
using JustyBase.Common;
using JustyBase.Common.Contracts;
using JustyBase.Common.Helpers;
using JustyBase.Common.Services;
using JustyBase.Helpers;
using JustyBase.Helpers.Interactions;
using JustyBase.PluginCommon.Contracts;
using JustyBase.Services;
using JustyBase.Themes;
using JustyBase.ViewModels;
using JustyBase.ViewModels.Documents;
using JustyBase.ViewModels.Tools;
using Microsoft.Extensions.DependencyInjection;
using SqlEditor.Avalonia.AvaloniaSpecificHelpers;

namespace JustyBase;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddSingleton<IEncryptionHelper, WindowsEncryptionHelper>();
        collection.AddSingleton<IThemeManager, FluentThemeManager>();
        collection.AddSingleton<IOtherHelpers, OtherHelpers>();
        collection.AddSingleton<ISimpleLogger, EmptyLogger>();
        collection.AddSingleton<IMessageForUserTools, MessageForUserTools>();
        collection.AddSingleton<IGeneralApplicationData, GeneralApplicationData>();
        collection.AddSingleton<IAvaloniaSpecificHelpers, AvaloniaSpecificHelpers>();
        collection.AddSingleton<IClipboardService, ClipboardService>();
        collection.AddSingleton<ISearchInFiles, SearchInFiles>();
        collection.AddSingleton<AutocompleteService>();
        collection.AddSingleton<AboutViewModel>();
        collection.AddSingleton<HistoryService>();
        collection.AddSingleton<IFactory, DockFactory>();
        collection.AddSingleton<VariablesViewModel>();
        collection.AddSingleton<ImportViewModel>();
        collection.AddSingleton<LogToolViewModel>();
        collection.AddTransient<SettingsViewModel>();
        collection.AddTransient<MainWindowViewModel>();
        collection.AddTransient<FileExplorerViewModel>();
        collection.AddTransient<SqlResultsFastViewModel>();
        collection.AddTransient<DbSchemaViewModel>();
        collection.AddTransient<AddNewConnectionViewModel>();
        collection.AddTransient<SqlDocumentViewModel>();
        collection.AddTransient<SqlResultsViewModel>();
        collection.AddTransient<HistoryViewModel>();
    }
}