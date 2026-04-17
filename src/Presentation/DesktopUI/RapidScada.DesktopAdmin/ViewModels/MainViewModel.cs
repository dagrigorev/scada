using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RapidScada.DesktopAdmin.Views;

namespace RapidScada.DesktopAdmin.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _currentView;

    [RelayCommand]
    private void NavigateToServiceManager()
    {
        CurrentView = new ServiceManagerView
        {
            DataContext = new ServiceManagerViewModel()
        };
    }

    [RelayCommand]
    private void NavigateToDatabaseManager()
    {
        CurrentView = CreatePlaceholderView("Database Manager", "Manage database migrations, backups, and maintenance");
    }

    [RelayCommand]
    private void NavigateToConfiguration()
    {
        CurrentView = CreatePlaceholderView("Configuration", "Edit appsettings.json for all services");
    }

    [RelayCommand]
    private void NavigateToDiagnostics()
    {
        CurrentView = CreatePlaceholderView("Diagnostics", "Test connections and monitor performance");
    }

    [RelayCommand]
    private void NavigateToLogs()
    {
        CurrentView = CreatePlaceholderView("System Logs", "View real-time logs from all services");
    }

    private object CreatePlaceholderView(string title, string description)
    {
        return new Avalonia.Controls.StackPanel
        {
            Spacing = 20,
            Children =
            {
                new Avalonia.Controls.TextBlock
                {
                    Text = title,
                    FontSize = 24,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Foreground = Avalonia.Media.Brushes.White
                },
                new Avalonia.Controls.TextBlock
                {
                    Text = description,
                    FontSize = 14,
                    Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#9ca3af"))
                },
                new Avalonia.Controls.Border
                {
                    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1f2937")),
                    CornerRadius = new Avalonia.CornerRadius(8),
                    Padding = new Avalonia.Thickness(20),
                    Child = new Avalonia.Controls.TextBlock
                    {
                        Text = "This view is under construction.\nImplementation coming soon!",
                        FontSize = 16,
                        Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#9ca3af")),
                        TextAlignment = Avalonia.Media.TextAlignment.Center
                    }
                }
            }
        };
    }

    public MainViewModel()
    {
        // Default to Service Manager
        NavigateToServiceManager();
    }
}
