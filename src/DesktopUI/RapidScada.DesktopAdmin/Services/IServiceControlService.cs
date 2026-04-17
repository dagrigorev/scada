using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RapidScada.DesktopAdmin.Services;

public interface IServiceControlService
{
    Task<ServiceStatus> GetServiceStatusAsync(string serviceName, int port);
    Task<bool> StartServiceAsync(string serviceName, string projectPath);
    Task<bool> StopServiceAsync(string serviceName, int port);
    Task<bool> RestartServiceAsync(string serviceName, string projectPath, int port);
    Task<List<ServiceProcess>> GetRunningServicesAsync();
}

public class ServiceControlService : IServiceControlService
{
    private readonly Dictionary<string, Process> _runningProcesses = new();

    public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName, int port)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);

            var healthUrl = $"https://localhost:{port}/health";
            var response = await client.GetAsync(healthUrl);

            if (response.IsSuccessStatusCode)
            {
                return ServiceStatus.Running;
            }

            return ServiceStatus.Stopped;
        }
        catch (TaskCanceledException)
        {
            return ServiceStatus.Stopped;
        }
        catch (Exception)
        {
            return ServiceStatus.Stopped;
        }
    }

    public async Task<bool> StartServiceAsync(string serviceName, string projectPath)
    {
        try
        {
            // Stop any existing instance first
            if (_runningProcesses.ContainsKey(serviceName))
            {
                try
                {
                    _runningProcesses[serviceName].Kill();
                    _runningProcesses.Remove(serviceName);
                }
                catch { }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project {projectPath}",
                UseShellExecute = false,
                CreateNoWindow = false, // Show console for debugging
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory
            };

            var process = Process.Start(startInfo);
            if (process != null)
            {
                _runningProcesses[serviceName] = process;

                // Setup output handlers
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Debug.WriteLine($"[{serviceName}] {e.Data}");
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Debug.WriteLine($"[{serviceName} ERROR] {e.Data}");
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait a bit for startup
                await Task.Delay(2000);

                return !process.HasExited;
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error starting service {serviceName}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopServiceAsync(string serviceName, int port)
    {
        try
        {
            // Try to stop our tracked process first
            if (_runningProcesses.ContainsKey(serviceName))
            {
                try
                {
                    var process = _runningProcesses[serviceName];
                    if (!process.HasExited)
                    {
                        process.Kill();
                        await process.WaitForExitAsync();
                    }
                    _runningProcesses.Remove(serviceName);
                }
                catch { }
            }

            // Also find and kill any other processes using this port
            var processes = await FindProcessesByPortAsync(port);
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    await process.WaitForExitAsync();
                }
                catch { }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping service {serviceName}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RestartServiceAsync(string serviceName, string projectPath, int port)
    {
        var stopped = await StopServiceAsync(serviceName, port);
        if (stopped)
        {
            await Task.Delay(2000); // Wait before restart
            return await StartServiceAsync(serviceName, projectPath);
        }
        return false;
    }

    public async Task<List<ServiceProcess>> GetRunningServicesAsync()
    {
        var services = new List<ServiceProcess>();

        var allProcesses = Process.GetProcesses();
        foreach (var process in allProcesses)
        {
            try
            {
                if (process.ProcessName.Contains("dotnet") || 
                    process.ProcessName.Contains("RapidScada"))
                {
                    services.Add(new ServiceProcess
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        StartTime = process.StartTime,
                        WorkingSet = process.WorkingSet64 / 1024 / 1024 // MB
                    });
                }
            }
            catch { }
        }

        return await Task.FromResult(services);
    }

    private async Task<List<Process>> FindProcessesByPortAsync(int port)
    {
        var processes = new List<Process>();

        try
        {
            // On Windows, use netstat
            if (OperatingSystem.IsWindows())
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains($":{port} "))
                        {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0 && int.TryParse(parts[^1], out int pid))
                            {
                                try
                                {
                                    processes.Add(Process.GetProcessById(pid));
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            // On Linux/Mac, use lsof
            else
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "lsof",
                    Arguments = $"-i :{port} -t",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    var pids = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pidStr in pids)
                    {
                        if (int.TryParse(pidStr, out int pid))
                        {
                            try
                            {
                                processes.Add(Process.GetProcessById(pid));
                            }
                            catch { }
                        }
                    }
                }
            }
        }
        catch { }

        return processes;
    }
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

public class ServiceProcess
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public long WorkingSet { get; set; }
}
