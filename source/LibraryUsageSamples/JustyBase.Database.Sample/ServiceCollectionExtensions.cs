using JustyBase.Common.Contracts;
using JustyBase.Common.Helpers;
using JustyBase.Database.Sample.ViewModels;
using JustyBase.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JustyBase.Database.Sample;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddSingleton<IEncryptionHelper, WindowsLinuxEncryptionHelper>();
        collection.AddSingleton<IAvaloniaSpecificHelpers, AvaloniaSpecificHelpers>();
        collection.AddTransient<ConnectionDataViewModel>();
        collection.AddTransient<MainWindowViewModel>();
    }
}