using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RapidScada.DesktopAdmin.ViewModels;

public partial class ConfigurationViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ConfigFile> _configFiles = new();

    [ObservableProperty]
    private ConfigFile? _selectedConfigFile;

    [ObservableProperty]
    private string _configContent = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasUnsavedChanges = false;

    public ConfigurationViewModel()
    {
        LoadConfigFiles();
    }

    private void LoadConfigFiles()
    {
        ConfigFiles.Clear();

        var services = new[]
        {
            ("RapidScada.Identity", "../../../Services/RapidScada.Identity/appsettings.json"),
            ("RapidScada.WebApi", "../../../Presentation/RapidScada.WebApi/appsettings.json"),
            ("RapidScada.Realtime", "../../../Services/RapidScada.Realtime/appsettings.json"),
            ("RapidScada.Communicator", "../../../Services/RapidScada.Communicator/appsettings.json"),
            ("RapidScada.Archiver", "../../../Services/RapidScada.Archiver/appsettings.json")
        };

        foreach (var (name, path) in services)
        {
            ConfigFiles.Add(new ConfigFile
            {
                ServiceName = name,
                FilePath = path,
                FileName = "appsettings.json"
            });
        }

        if (ConfigFiles.Count > 0)
        {
            SelectedConfigFile = ConfigFiles[0];
            LoadConfigContent();
        }
    }

    partial void OnSelectedConfigFileChanged(ConfigFile? value)
    {
        if (value != null)
        {
            LoadConfigContent();
        }
    }

    [RelayCommand]
    private void LoadConfigContent()
    {
        if (SelectedConfigFile == null) return;

        try
        {
            if (File.Exists(SelectedConfigFile.FilePath))
            {
                ConfigContent = File.ReadAllText(SelectedConfigFile.FilePath);
                SelectedConfigFile.LastModified = File.GetLastWriteTime(SelectedConfigFile.FilePath);
                StatusMessage = $"Loaded {SelectedConfigFile.ServiceName}";
                HasUnsavedChanges = false;
            }
            else
            {
                ConfigContent = "// File not found";
                StatusMessage = $"File not found: {SelectedConfigFile.FilePath}";
            }
        }
        catch (Exception ex)
        {
            ConfigContent = $"// Error loading file: {ex.Message}";
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveConfig()
    {
        if (SelectedConfigFile == null) return;

        try
        {
            // Validate JSON first
            JsonDocument.Parse(ConfigContent);

            // Create backup
            var backupPath = SelectedConfigFile.FilePath + ".backup";
            if (File.Exists(SelectedConfigFile.FilePath))
            {
                File.Copy(SelectedConfigFile.FilePath, backupPath, true);
            }

            // Save new content
            await File.WriteAllTextAsync(SelectedConfigFile.FilePath, ConfigContent);
            
            SelectedConfigFile.LastModified = DateTime.Now;
            StatusMessage = $"✓ Saved {SelectedConfigFile.ServiceName}";
            HasUnsavedChanges = false;
        }
        catch (JsonException ex)
        {
            StatusMessage = $"✗ Invalid JSON: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private void FormatJson()
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(ConfigContent);
            var options = new JsonSerializerOptions { WriteIndented = true };
            ConfigContent = JsonSerializer.Serialize(jsonDoc, options);
            StatusMessage = "✓ JSON formatted";
            HasUnsavedChanges = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Format error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ValidateJson()
    {
        try
        {
            JsonDocument.Parse(ConfigContent);
            StatusMessage = "✓ JSON is valid";
        }
        catch (JsonException ex)
        {
            StatusMessage = $"✗ Invalid JSON: {ex.Message}";
        }
    }

    partial void OnConfigContentChanged(string value)
    {
        HasUnsavedChanges = true;
    }
}

public partial class ConfigFile : ObservableObject
{
    [ObservableProperty]
    private string _serviceName = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private DateTime? _lastModified;
}
