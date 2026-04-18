using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace RapidScada.DesktopAdmin.Services;

public interface IDiagnosticService
{
    Task<DiagnosticResult> TestDatabaseConnectionAsync(string connectionString);
    Task<DiagnosticResult> TestServiceHealthAsync(string serviceName, int port);
    Task<DiagnosticResult> TestNetworkConnectivityAsync();
    Task<DiagnosticResult> CheckDiskSpaceAsync();
    Task<DiagnosticResult> CheckSystemResourcesAsync();
    Task<List<DiagnosticResult>> RunAllDiagnosticsAsync(string connectionString, Dictionary<string, int> services);
}

public class DiagnosticService : IDiagnosticService
{
    public async Task<DiagnosticResult> TestDatabaseConnectionAsync(string connectionString)
    {
        var result = new DiagnosticResult
        {
            TestName = "Database Connection",
            Category = "Database"
        };

        var startTime = DateTime.Now;

        try
        {
            // Parse connection string
            var parts = ParseConnectionString(connectionString);

            var startInfo = new ProcessStartInfo
            {
                FileName = "psql",
                Arguments = $"-h {parts["Host"]} -p {parts["Port"]} -U {parts["Username"]} -d {parts["Database"]} -c \"SELECT version();\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (parts.ContainsKey("Password"))
                startInfo.EnvironmentVariables["PGPASSWORD"] = parts["Password"];

            var process = Process.Start(startInfo);
            if (process == null)
            {
                result.Success = false;
                result.Message = "Failed to start psql process";
                return result;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            result.Duration = DateTime.Now - startTime;

            if (process.ExitCode == 0)
            {
                result.Success = true;
                result.Message = "Database connection successful";
                result.Details = output.Split('\n').FirstOrDefault()?.Trim() ?? "";
            }
            else
            {
                result.Success = false;
                result.Message = "Database connection failed";
                result.Details = error;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error: {ex.Message}";
            result.Duration = DateTime.Now - startTime;
        }

        return result;
    }

    public async Task<DiagnosticResult> TestServiceHealthAsync(string serviceName, int port)
    {
        var result = new DiagnosticResult
        {
            TestName = $"{serviceName} Health Check",
            Category = "Service"
        };

        var startTime = DateTime.Now;

        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var healthUrl = $"https://localhost:{port}/health";
            var response = await client.GetAsync(healthUrl);

            result.Duration = DateTime.Now - startTime;

            if (response.IsSuccessStatusCode)
            {
                result.Success = true;
                result.Message = $"Service is healthy (Port {port})";
                result.Details = $"Response time: {result.Duration.TotalMilliseconds:F0}ms";
            }
            else
            {
                result.Success = false;
                result.Message = $"Service returned {response.StatusCode}";
                result.Details = $"Port {port}";
            }
        }
        catch (TaskCanceledException)
        {
            result.Success = false;
            result.Message = "Service not responding (timeout)";
            result.Details = $"Port {port}";
            result.Duration = DateTime.Now - startTime;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Service not available";
            result.Details = $"Port {port}: {ex.Message}";
            result.Duration = DateTime.Now - startTime;
        }

        return result;
    }

    public async Task<DiagnosticResult> TestNetworkConnectivityAsync()
    {
        var result = new DiagnosticResult
        {
            TestName = "Network Connectivity",
            Category = "Network"
        };

        var startTime = DateTime.Now;

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 5000);

            result.Duration = DateTime.Now - startTime;

            if (reply.Status == IPStatus.Success)
            {
                result.Success = true;
                result.Message = "Network is reachable";
                result.Details = $"Ping to 8.8.8.8: {reply.RoundtripTime}ms";
            }
            else
            {
                result.Success = false;
                result.Message = $"Network unreachable: {reply.Status}";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Network test failed";
            result.Details = ex.Message;
            result.Duration = DateTime.Now - startTime;
        }

        return result;
    }

    public async Task<DiagnosticResult> CheckDiskSpaceAsync()
    {
        var result = new DiagnosticResult
        {
            TestName = "Disk Space",
            Category = "System"
        };

        var startTime = DateTime.Now;

        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
            
            if (drives.Count == 0)
            {
                result.Success = false;
                result.Message = "No drives found";
                return result;
            }

            var systemDrive = drives.First();
            var freeGB = systemDrive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            var totalGB = systemDrive.TotalSize / (1024.0 * 1024 * 1024);
            var percentFree = (freeGB / totalGB) * 100;

            result.Duration = DateTime.Now - startTime;
            result.Details = $"{systemDrive.Name}: {freeGB:F1} GB free of {totalGB:F1} GB ({percentFree:F1}% free)";

            if (percentFree > 20)
            {
                result.Success = true;
                result.Message = "Sufficient disk space";
            }
            else if (percentFree > 10)
            {
                result.Success = true;
                result.Message = "Disk space is low";
            }
            else
            {
                result.Success = false;
                result.Message = "Critical: Disk space is very low";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Disk check failed";
            result.Details = ex.Message;
            result.Duration = DateTime.Now - startTime;
        }

        return await Task.FromResult(result);
    }

    public async Task<DiagnosticResult> CheckSystemResourcesAsync()
    {
        var result = new DiagnosticResult
        {
            TestName = "System Resources",
            Category = "System"
        };

        var startTime = DateTime.Now;

        try
        {
            var process = Process.GetCurrentProcess();
            
            var memoryMB = process.WorkingSet64 / 1024.0 / 1024.0;
            var cpuTime = process.TotalProcessorTime;
            
            result.Duration = DateTime.Now - startTime;
            result.Details = $"Memory: {memoryMB:F1} MB\nCPU Time: {cpuTime.TotalSeconds:F1}s";
            result.Success = true;
            result.Message = "System resources OK";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Resource check failed";
            result.Details = ex.Message;
            result.Duration = DateTime.Now - startTime;
        }

        return await Task.FromResult(result);
    }

    public async Task<List<DiagnosticResult>> RunAllDiagnosticsAsync(
        string connectionString, 
        Dictionary<string, int> services)
    {
        var results = new List<DiagnosticResult>();

        // Database test
        results.Add(await TestDatabaseConnectionAsync(connectionString));

        // Service health tests
        foreach (var service in services)
        {
            results.Add(await TestServiceHealthAsync(service.Key, service.Value));
        }

        // Network test
        results.Add(await TestNetworkConnectivityAsync());

        // Disk space test
        results.Add(await CheckDiskSpaceAsync());

        // System resources test
        results.Add(await CheckSystemResourcesAsync());

        return results;
    }

    private Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var parts = new Dictionary<string, string>();
        var pairs = connectionString.Split(';');

        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();
                parts[key] = value;
            }
        }

        return parts;
    }
}

public class DiagnosticResult
{
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string StatusIcon => Success ? "✓" : "✗";
    public string StatusColor => Success ? "#10b981" : "#ef4444";
}
