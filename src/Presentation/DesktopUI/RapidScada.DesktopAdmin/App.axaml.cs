using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RapidScada.DesktopAdmin.Services;
using RapidScada.DesktopAdmin.ViewModels;
using RapidScada.DesktopAdmin.Views;

namespace RapidScada.DesktopAdmin;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Setup DI
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            desktop.MainWindow = new MainWindow
            {
                DataContext = serviceProvider.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<ServiceManagerViewModel>();
        services.AddTransient<DatabaseManagerViewModel>();
        services.AddTransient<ConfigurationViewModel>();
        services.AddTransient<DiagnosticsViewModel>();

        // Services
        services.AddSingleton<IServiceControlService, ServiceControlService>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IDiagnosticService, DiagnosticService>();
    }
}
