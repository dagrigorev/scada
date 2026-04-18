using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RapidScada.DesktopAdmin.ViewModels;

public partial class ServiceManagerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ServiceInfo> _services;

    public ServiceManagerViewModel()
    {
        Services = new ObservableCollection<ServiceInfo>
        {
            new ServiceInfo 
            { 
                Name = "RapidScada.Identity",
                DisplayName = "Identity Service",
                Port = 5003,
                Status = ServiceStatus.Unknown,
                Description = "Authentication and authorization service"
            },
            new ServiceInfo 
            { 
                Name = "RapidScada.WebApi",
                DisplayName = "Web API",
                Port = 5001,
                Status = ServiceStatus.Unknown,
                Description = "Main REST API service"
            },
            new ServiceInfo 
            { 
                Name = "RapidScada.Realtime",
                DisplayName = "Realtime Service",
                Port = 5005,
                Status = ServiceStatus.Unknown,
                Description = "SignalR real-time communication hub"
            },
            new ServiceInfo 
            { 
                Name = "RapidScada.Communicator",
                DisplayName = "Communicator",
                Port = 5007,
                Status = ServiceStatus.Unknown,
                Description = "Device polling and data collection"
            },
            new ServiceInfo 
            { 
                Name = "RapidScada.Archiver",
                DisplayName = "Archiver",
                Port = 5009,
                Status = ServiceStatus.Unknown,
                Description = "Historical data archival service"
            }
        };

        RefreshServicesStatus();
    }

    [RelayCommand]
    private async Task RefreshServicesStatus()
    {
        foreach (var service in Services)
        {
            service.Status = await CheckServiceStatus(service.Port);
            service.LastChecked = DateTime.Now;
        }
    }

    [RelayCommand]
    private async Task StartService(ServiceInfo service)
    {
        service.Status = ServiceStatus.Starting;
        
        try
        {
            // Start the dotnet process for this service
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project ../../../Services/{service.Name}/{service.Name}.csproj",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(startInfo);
            
            // Wait a bit and check status
            await Task.Delay(3000);
            service.Status = await CheckServiceStatus(service.Port);
        }
        catch (Exception ex)
        {
            service.Status = ServiceStatus.Error;
            service.LastError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task StopService(ServiceInfo service)
    {
        service.Status = ServiceStatus.Stopping;
        
        try
        {
            // Find and kill processes listening on the port
            var processes = Process.GetProcesses()
                .Where(p => p.ProcessName.Contains("dotnet") || p.ProcessName.Contains(service.Name));

            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch { }
            }

            await Task.Delay(1000);
            service.Status = ServiceStatus.Stopped;
        }
        catch (Exception ex)
        {
            service.Status = ServiceStatus.Error;
            service.LastError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RestartService(ServiceInfo service)
    {
        await StopService(service);
        await Task.Delay(2000);
        await StartService(service);
    }

    private async Task<ServiceStatus> CheckServiceStatus(int port)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            
            var response = await client.GetAsync($"https://localhost:{port}/health");
            return response.IsSuccessStatusCode ? ServiceStatus.Running : ServiceStatus.Stopped;
        }
        catch
        {
            return ServiceStatus.Stopped;
        }
    }
}

public partial class ServiceInfo : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private ServiceStatus _status;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private DateTime? _lastChecked;

    [ObservableProperty]
    private string? _lastError;

    public string StatusText => Status switch
    {
        ServiceStatus.Running => "Running",
        ServiceStatus.Stopped => "Stopped",
        ServiceStatus.Starting => "Starting...",
        ServiceStatus.Stopping => "Stopping...",
        ServiceStatus.Error => "Error",
        _ => "Unknown"
    };

    public string StatusColor => Status switch
    {
        ServiceStatus.Running => "#10b981",
        ServiceStatus.Stopped => "#ef4444",
        ServiceStatus.Starting => "#f59e0b",
        ServiceStatus.Stopping => "#f59e0b",
        ServiceStatus.Error => "#dc2626",
        _ => "#9ca3af"
    };

    public bool CanStart => Status == ServiceStatus.Stopped || Status == ServiceStatus.Error;
    public bool CanStop => Status == ServiceStatus.Running;
    public bool CanRestart => Status == ServiceStatus.Running;
}

public enum ServiceStatus
{
    Unknown,
    Running,
    Stopped,
    Starting,
    Stopping,
    Error
}
