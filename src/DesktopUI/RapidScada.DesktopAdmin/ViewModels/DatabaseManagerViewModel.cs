using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RapidScada.DesktopAdmin.ViewModels;

public partial class DatabaseManagerViewModel : ObservableObject
{
    [ObservableProperty]
    private string _connectionString = "Host=localhost;Port=5432;Database=rapidscada;Username=scada;Password=scada123";

    [ObservableProperty]
    private string _databaseStatus = "Checking...";

    [ObservableProperty]
    private bool _isDatabaseOnline = false;

    [ObservableProperty]
    private ObservableCollection<MigrationInfo> _migrations = new();

    [ObservableProperty]
    private string _lastBackupDate = "Never";

    [ObservableProperty]
    private string _databaseSize = "0 MB";

    [ObservableProperty]
    private string _outputLog = string.Empty;

    public DatabaseManagerViewModel()
    {
        CheckDatabaseConnection();
        LoadMigrations();
    }

    [RelayCommand]
    private async Task CheckDatabaseConnection()
    {
        DatabaseStatus = "Checking connection...";
        AppendLog("Checking database connection...");

        try
        {
            // Simple connection test using psql
            var startInfo = new ProcessStartInfo
            {
                FileName = "psql",
                Arguments = $"-h localhost -U scada -d rapidscada -c \"SELECT version();\"",
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
                DatabaseStatus = "Connected";
                IsDatabaseOnline = true;
                AppendLog("✓ Database connection successful");
            }
            else
            {
                DatabaseStatus = "Connection failed";
                IsDatabaseOnline = false;
                AppendLog("✗ Database connection failed");
            }
        }
        catch (Exception ex)
        {
            DatabaseStatus = $"Error: {ex.Message}";
            IsDatabaseOnline = false;
            AppendLog($"✗ Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ApplyMigrations()
    {
        AppendLog("Applying database migrations...");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "ef database update --project ../../../Presentation/RapidScada.WebApi",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            
            process!.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    AppendLog(e.Data);
            };

            process.BeginOutputReadLine();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                AppendLog("✓ Migrations applied successfully");
                await LoadMigrations();
            }
            else
            {
                AppendLog("✗ Migration failed");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"✗ Error applying migrations: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CreateBackup()
    {
        var backupFile = $"rapidscada_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
        AppendLog($"Creating backup: {backupFile}");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = $"-h localhost -U scada -d rapidscada -f {backupFile}",
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
                LastBackupDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                AppendLog($"✓ Backup created: {backupFile}");
            }
            else
            {
                AppendLog("✗ Backup failed");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"✗ Error creating backup: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ResetDatabase()
    {
        AppendLog("⚠ WARNING: Resetting database will delete ALL data!");
        AppendLog("Dropping database...");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "psql",
                Arguments = "-h localhost -U postgres -c \"DROP DATABASE IF EXISTS rapidscada; CREATE DATABASE rapidscada;\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            await process!.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                AppendLog("✓ Database reset");
                await ApplyMigrations();
            }
            else
            {
                AppendLog("✗ Reset failed");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"✗ Error resetting database: {ex.Message}");
        }
    }

    private async Task LoadMigrations()
    {
        // This would normally query the database
        Migrations.Clear();
        Migrations.Add(new MigrationInfo { Name = "InitialCreate", AppliedDate = DateTime.Now.AddDays(-10) });
        Migrations.Add(new MigrationInfo { Name = "AddDevices", AppliedDate = DateTime.Now.AddDays(-8) });
        Migrations.Add(new MigrationInfo { Name = "AddTags", AppliedDate = DateTime.Now.AddDays(-5) });
        await Task.CompletedTask;
    }

    private void AppendLog(string message)
    {
        OutputLog += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
    }
}

public class MigrationInfo
{
    public string Name { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }
}
