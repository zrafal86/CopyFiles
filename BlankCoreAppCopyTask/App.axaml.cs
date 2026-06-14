using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BlankCoreAppCopyTask.Services;
using BlankCoreAppCopyTask.ViewModels;
using BlankCoreAppCopyTask.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BlankCoreAppCopyTask;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var folderPickerService = new AvaloniaFolderPickerService();

        var services = new ServiceCollection();
        services.AddSingleton<IHashCalculator, HashService>();
        services.AddKeyedSingleton<ISynchronization, SynchronizationMultiThread>("VerMultiThread");
        services.AddKeyedSingleton<ISynchronization, SynchronizationOneThread>("VerOneThread");
        services.AddSingleton<IFolderPickerService>(folderPickerService);
        services.AddTransient<MainWindowViewModel>();

        var provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = provider.GetRequiredService<MainWindowViewModel>();
            var window = new MainWindow { DataContext = vm };
            window.Loaded += (_, _) => folderPickerService.Configure(window.StorageProvider);
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
