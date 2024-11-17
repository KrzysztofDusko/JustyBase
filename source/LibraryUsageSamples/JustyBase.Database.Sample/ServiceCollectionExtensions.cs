using JustyBase.Database.Sample.Contracts;
using JustyBase.Database.Sample.Services;
using JustyBase.Database.Sample.ViewModels;
using JustyBase.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JustyBase.Database.Sample;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddSingleton<IAvaloniaSpecificHelpers, AvaloniaSpecificHelpers>();
        collection.AddSingleton<IDatabaseHelperService, DatabaseHelperService>();
        collection.AddTransient<MainWindowViewModel>();
    }
}