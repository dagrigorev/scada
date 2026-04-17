using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace RapidScada.DesktopAdmin.Services;

public interface IDatabaseService
{
    Task<bool> TestConnectionAsync(string connectionString);
    Task<DatabaseInfo> GetDatabaseInfoAsync(string connectionString);
    Task<bool> ApplyMigrationsAsync(string projectPath, Action<string>? logCallback = null);
    Task<bool> CreateBackupAsync(string connectionString, string backupPath, Action<string>? logCallback = null);
    Task<bool> RestoreBackupAsync(string connectionString, string backupPath, Action<string>? logCallback = null);
    Task<bool> ResetDatabaseAsync(string databaseName, Action<string>? logCallback = null);
    Task<List<MigrationInfo>> GetMigrationsAsync(string projectPath);
}

public class DatabaseService : IDatabaseService
{
    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            var parts = ParseConnectionString(connectionString);
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "psql",
                Arguments = $"-h {parts.Host} -p {parts.Port} -U {parts.Username} -d {parts.Database} -c \"SELECT 1;\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.EnvironmentVariables["PGPASSWORD"] = parts.Password;

            var process = Process.Start(startInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<DatabaseInfo> GetDatabaseInfoAsync(string connectionString)
    {
        var info = new DatabaseInfo();

        try
        {
            var parts = ParseConnectionString(connectionString);

            // Get database size
            var sizeQuery = "SELECT pg_database_size(current_database());";
            var sizeResult = await ExecuteScalarQueryAsync(parts, sizeQuery);
            if (long.TryParse(sizeResult, out long sizeBytes))
            {
                info.SizeInBytes = sizeBytes;
                info.SizeInMB = sizeBytes / 1024.0 / 1024.0;
            }

            // Get table count
            var tableCountQuery = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';";
            var tableCountResult = await ExecuteScalarQueryAsync(parts, tableCountQuery);
            if (int.TryParse(tableCountResult, out int tableCount))
            {
                info.TableCount = tableCount;
            }

            // Get connection count
            var connQuery = "SELECT COUNT(*) FROM pg_stat_activity WHERE datname = current_database();";
            var connResult = await ExecuteScalarQueryAsync(parts, connQuery);
            if (int.TryParse(connResult, out int connCount))
            {
                info.ActiveConnections = connCount;
            }

            info.IsOnline = true;
        }
        catch
        {
            info.IsOnline = false;
        }

        return info;
    }

    public async Task<bool> ApplyMigrationsAsync(string projectPath, Action<string>? logCallback = null)
    {
        try
        {
            logCallback?.Invoke("Starting migration process...");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"ef database update --project \"{projectPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory
            };

            var process = Process.Start(startInfo);
            if (process == null) return false;

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    logCallback?.Invoke(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    logCallback?.Invoke($"ERROR: {e.Data}");
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            logCallback?.Invoke(process.ExitCode == 0 ? "✓ Migrations applied successfully" : "✗ Migration failed");

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            logCallback?.Invoke($"✗ Exception: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateBackupAsync(string connectionString, string backupPath, Action<string>? logCallback = null)
    {
        try
        {
            var parts = ParseConnectionString(connectionString);
            logCallback?.Invoke($"Creating backup: {backupPath}");

            var startInfo = new ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = $"-h {parts.Host} -p {parts.Port} -U {parts.Username} -d {parts.Database} -f \"{backupPath}\" -F p",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.EnvironmentVariables["PGPASSWORD"] = parts.Password;

            var process = Process.Start(startInfo);
            if (process == null) return false;

            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                logCallback?.Invoke($"✓ Backup created successfully: {backupPath}");
                return true;
            }
            else
            {
                logCallback?.Invoke($"✗ Backup failed: {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            logCallback?.Invoke($"✗ Exception: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RestoreBackupAsync(string connectionString, string backupPath, Action<string>? logCallback = null)
    {
        try
        {
            var parts = ParseConnectionString(connectionString);
            logCallback?.Invoke($"Restoring from: {backupPath}");

            var startInfo = new ProcessStartInfo
            {
                FileName = "psql",
                Arguments = $"-h {parts.Host} -p {parts.Port} -U {parts.Username} -d {parts.Database} -f \"{backupPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.EnvironmentVariables["PGPASSWORD"] = parts.Password;

            var process = Process.Start(startInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                logCallback?.Invoke("✓ Backup restored successfully");
                return true;
            }
            else
            {
                logCallback?.Invoke("✗ Restore failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            logCallback?.Invoke($"✗ Exception: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ResetDatabaseAsync(string databaseName, Action<string>? logCallback = null)
    {
        try
        {
            logCallback?.Invoke($"⚠ WARNING: Dropping database '{databaseName}'");

            // Drop database
            var dropInfo = new ProcessStartInfo
            {
                FileName = "psql",
                Arguments = $"-h localhost -U postgres -c \"DROP DATABASE IF EXISTS {databaseName};\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var dropProcess = Process.Start(dropInfo);
            if (dropProcess != null)
            {
                await dropProcess.WaitForExitAsync();
            }

            logCallback?.Invoke("Creating new database...");

            // Create database
            var createInfo = new ProcessStartInfo
            {
                FileName = "psql",
                Arguments = $"-h localhost -U postgres -c \"CREATE DATABASE {databaseName};\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var createProcess = Process.Start(createInfo);
            if (createProcess != null)
            {
                await createProcess.WaitForExitAsync();
                
                if (createProcess.ExitCode == 0)
                {
                    logCallback?.Invoke("✓ Database reset successfully");
                    return true;
                }
            }

            logCallback?.Invoke("✗ Reset failed");
            return false;
        }
        catch (Exception ex)
        {
            logCallback?.Invoke($"✗ Exception: {ex.Message}");
            return false;
        }
    }

    public async Task<List<MigrationInfo>> GetMigrationsAsync(string projectPath)
    {
        var migrations = new List<MigrationInfo>();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"ef migrations list --project \"{projectPath}\" --json",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            if (process == null) return migrations;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // Parse JSON output (simplified - actual format may vary)
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("(Applied)") || line.Contains("(Pending)"))
                    {
                        migrations.Add(new MigrationInfo
                        {
                            Name = line.Trim(),
                            IsApplied = line.Contains("(Applied)"),
                            AppliedDate = line.Contains("(Applied)") ? DateTime.Now : null
                        });
                    }
                }
            }
        }
        catch { }

        return migrations;
    }

    private async Task<string> ExecuteScalarQueryAsync(ConnectionParts parts, string query)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "psql",
            Arguments = $"-h {parts.Host} -p {parts.Port} -U {parts.Username} -d {parts.Database} -t -c \"{query}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.EnvironmentVariables["PGPASSWORD"] = parts.Password;

        var process = Process.Start(startInfo);
        if (process == null) return string.Empty;

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output.Trim();
    }

    private ConnectionParts ParseConnectionString(string connectionString)
    {
        var parts = new ConnectionParts();
        var pairs = connectionString.Split(';');

        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                var key = keyValue[0].Trim().ToLower();
                var value = keyValue[1].Trim();

                switch (key)
                {
                    case "host":
                        parts.Host = value;
                        break;
                    case "port":
                        parts.Port = value;
                        break;
                    case "database":
                        parts.Database = value;
                        break;
                    case "username":
                    case "user id":
                        parts.Username = value;
                        break;
                    case "password":
                        parts.Password = value;
                        break;
                }
            }
        }

        return parts;
    }

    private class ConnectionParts
    {
        public string Host { get; set; } = "localhost";
        public string Port { get; set; } = "5432";
        public string Database { get; set; } = "rapidscada";
        public string Username { get; set; } = "scada";
        public string Password { get; set; } = "";
    }
}

public class DatabaseInfo
{
    public bool IsOnline { get; set; }
    public long SizeInBytes { get; set; }
    public double SizeInMB { get; set; }
    public int TableCount { get; set; }
    public int ActiveConnections { get; set; }
}

public class MigrationInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsApplied { get; set; }
    public DateTime? AppliedDate { get; set; }
}
