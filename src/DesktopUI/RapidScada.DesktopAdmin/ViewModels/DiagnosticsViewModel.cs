using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RapidScada.DesktopAdmin.ViewModels;

public partial class DiagnosticsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DiagnosticTest> _tests = new();

    [ObservableProperty]
    private string _diagnosticLog = string.Empty;

    [ObservableProperty]
    private bool _isRunning = false;

    public DiagnosticsViewModel()
    {
        InitializeTests();
    }

    private void InitializeTests()
    {
        Tests.Clear();
        Tests.Add(new DiagnosticTest 
        { 
            Name = "PostgreSQL Connection",
            Description = "Test database connectivity",
            TestType = TestType.Database
        });
        Tests.Add(new DiagnosticTest 
        { 
            Name = "Identity Service (Port 5003)",
            Description = "Check if Identity service is responding",
            TestType = TestType.ServiceHealth
        });
        Tests.Add(new DiagnosticTest 
        { 
            Name = "WebAPI Service (Port 5001)",
            Description = "Check if WebAPI is responding",
            TestType = TestType.ServiceHealth
        });
        Tests.Add(new DiagnosticTest 
        { 
            Name = "Realtime Service (Port 5005)",
            Description = "Check if Realtime service is responding",
            TestType = TestType.ServiceHealth
        });
        Tests.Add(new DiagnosticTest 
        { 
            Name = "Network Connectivity",
            Description = "Test internet connection",
            TestType = TestType.Network
        });
        Tests.Add(new DiagnosticTest 
        { 
            Name = "Disk Space",
            Description = "Check available disk space",
            TestType = TestType.System
        });
    }

    [RelayCommand]
    private async Task RunAllTests()
    {
        IsRunning = true;
        DiagnosticLog = string.Empty;
        AppendLog("=== Starting Diagnostic Tests ===\n");

        foreach (var test in Tests)
        {
            await RunTest(test);
            await Task.Delay(500); // Small delay between tests
        }

        AppendLog("\n=== Diagnostic Tests Complete ===");
        IsRunning = false;
    }

    [RelayCommand]
    private async Task RunTest(DiagnosticTest test)
    {
        test.Status = TestStatus.Running;
        AppendLog($"\n▶ Running: {test.Name}");

        try
        {
            bool passed = test.TestType switch
            {
                TestType.Database => await TestDatabase(),
                TestType.ServiceHealth => await TestServiceHealth(test.Name),
                TestType.Network => await TestNetwork(),
                TestType.System => await TestSystem(),
                _ => false
            };

            test.Status = passed ? TestStatus.Passed : TestStatus.Failed;
            test.LastRun = DateTime.Now;

            AppendLog(passed ? "  ✓ PASSED" : "  ✗ FAILED");
        }
        catch (Exception ex)
        {
            test.Status = TestStatus.Failed;
            test.LastRun = DateTime.Now;
            test.ErrorMessage = ex.Message;
            AppendLog($"  ✗ ERROR: {ex.Message}");
        }
    }

    private async Task<bool> TestDatabase()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "psql",
                Arguments = "-h localhost -U scada -d rapidscada -c \"SELECT 1;\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.EnvironmentVariables["PGPASSWORD"] = "scada123";

            var process = Process.Start(startInfo);
            await process!.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                AppendLog("  Database connection successful");
                return true;
            }
            else
            {
                AppendLog("  Database connection failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            AppendLog($"  Error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> TestServiceHealth(string testName)
    {
        try
        {
            int port = testName switch
            {
                "Identity Service (Port 5003)" => 5003,
                "WebAPI Service (Port 5001)" => 5001,
                "Realtime Service (Port 5005)" => 5005,
                _ => 0
            };

            if (port == 0) return false;

            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(3);

            var response = await client.GetAsync($"https://localhost:{port}/health");
            
            if (response.IsSuccessStatusCode)
            {
                AppendLog($"  Service responding on port {port}");
                return true;
            }
            else
            {
                AppendLog($"  Service not responding on port {port}");
                return false;
            }
        }
        catch
        {
            AppendLog("  Service not available");
            return false;
        }
    }

    private async Task<bool> TestNetwork()
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 3000);

            if (reply.Status == IPStatus.Success)
            {
                AppendLog($"  Network OK (ping: {reply.RoundtripTime}ms)");
                return true;
            }
            else
            {
                AppendLog("  Network unreachable");
                return false;
            }
        }
        catch (Exception ex)
        {
            AppendLog($"  Network error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> TestSystem()
    {
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
            if (drive != null)
            {
                var freeGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                var totalGB = drive.TotalSize / (1024.0 * 1024 * 1024);
                var percentFree = (freeGB / totalGB) * 100;

                AppendLog($"  Disk: {freeGB:F1} GB free of {totalGB:F1} GB ({percentFree:F1}% free)");

                return percentFree > 10; // Pass if more than 10% free
            }

            return false;
        }
        catch (Exception ex)
        {
            AppendLog($"  System check error: {ex.Message}");
            return false;
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        DiagnosticLog = string.Empty;
        foreach (var test in Tests)
        {
            test.Status = TestStatus.NotRun;
        }
    }

    private void AppendLog(string message)
    {
        DiagnosticLog += $"{message}\n";
    }
}

public partial class DiagnosticTest : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private TestType _testType;

    [ObservableProperty]
    private TestStatus _status = TestStatus.NotRun;

    [ObservableProperty]
    private DateTime? _lastRun;

    [ObservableProperty]
    private string? _errorMessage;

    public string StatusText => Status switch
    {
        TestStatus.NotRun => "Not Run",
        TestStatus.Running => "Running...",
        TestStatus.Passed => "Passed",
        TestStatus.Failed => "Failed",
        _ => "Unknown"
    };

    public string StatusColor => Status switch
    {
        TestStatus.Passed => "#10b981",
        TestStatus.Failed => "#ef4444",
        TestStatus.Running => "#f59e0b",
        _ => "#9ca3af"
    };
}

public enum TestType
{
    Database,
    ServiceHealth,
    Network,
    System
}

public enum TestStatus
{
    NotRun,
    Running,
    Passed,
    Failed
}
