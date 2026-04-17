using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RapidScada.DesktopAdmin.Services;

public interface IConfigurationService
{
    Task<List<ConfigurationFile>> DiscoverConfigurationFilesAsync(string basePath);
    Task<string> LoadConfigurationAsync(string filePath);
    Task<bool> SaveConfigurationAsync(string filePath, string content, bool createBackup = true);
    Task<bool> ValidateJsonAsync(string content);
    Task<string> FormatJsonAsync(string content);
    Task<Dictionary<string, object>> ParseConfigurationAsync(string content);
    Task<bool> UpdateConfigValueAsync(string filePath, string jsonPath, object value);
}

public class ConfigurationService : IConfigurationService
{
    public async Task<List<ConfigurationFile>> DiscoverConfigurationFilesAsync(string basePath)
    {
        var configFiles = new List<ConfigurationFile>();

        try
        {
            // Find all appsettings.json files
            var searchPaths = new[]
            {
                Path.Combine(basePath, "Services"),
                Path.Combine(basePath, "Presentation"),
                Path.Combine(basePath, "Infrastructure")
            };

            foreach (var searchPath in searchPaths)
            {
                if (Directory.Exists(searchPath))
                {
                    var files = Directory.GetFiles(searchPath, "appsettings*.json", SearchOption.AllDirectories);
                    
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        var relativePath = Path.GetRelativePath(basePath, file);
                        
                        configFiles.Add(new ConfigurationFile
                        {
                            FilePath = file,
                            FileName = fileInfo.Name,
                            RelativePath = relativePath,
                            ServiceName = ExtractServiceName(relativePath),
                            LastModified = fileInfo.LastWriteTime,
                            SizeInBytes = fileInfo.Length
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error discovering config files: {ex.Message}");
        }

        return await Task.FromResult(configFiles.OrderBy(f => f.ServiceName).ToList());
    }

    public async Task<string> LoadConfigurationAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return string.Empty;

            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            return $"// Error loading file: {ex.Message}";
        }
    }

    public async Task<bool> SaveConfigurationAsync(string filePath, string content, bool createBackup = true)
    {
        try
        {
            // Validate JSON first
            JsonDocument.Parse(content);

            // Create backup if requested
            if (createBackup && File.Exists(filePath))
            {
                var backupPath = $"{filePath}.backup.{DateTime.Now:yyyyMMddHHmmss}";
                File.Copy(filePath, backupPath, true);

                // Keep only last 5 backups
                CleanupOldBackups(filePath);
            }

            // Save new content
            await File.WriteAllTextAsync(filePath, content);
            return true;
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Invalid JSON format");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error saving configuration: {ex.Message}");
        }
    }

    public async Task<bool> ValidateJsonAsync(string content)
    {
        try
        {
            JsonDocument.Parse(content);
            return await Task.FromResult(true);
        }
        catch
        {
            return await Task.FromResult(false);
        }
    }

    public async Task<string> FormatJsonAsync(string content)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(content);
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            return await Task.FromResult(JsonSerializer.Serialize(jsonDoc, options));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error formatting JSON: {ex.Message}");
        }
    }

    public async Task<Dictionary<string, object>> ParseConfigurationAsync(string content)
    {
        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            return await Task.FromResult(config ?? new Dictionary<string, object>());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing configuration: {ex.Message}");
            return await Task.FromResult(new Dictionary<string, object>());
        }
    }

    public async Task<bool> UpdateConfigValueAsync(string filePath, string jsonPath, object value)
    {
        try
        {
            var content = await LoadConfigurationAsync(filePath);
            var doc = JsonDocument.Parse(content);
            
            // This is a simplified implementation
            // Full implementation would use JsonPath or similar
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedContent = JsonSerializer.Serialize(doc, options);
            
            return await SaveConfigurationAsync(filePath, updatedContent);
        }
        catch
        {
            return false;
        }
    }

    private string ExtractServiceName(string relativePath)
    {
        var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        // Try to find service name from path
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "Services" || parts[i] == "Presentation")
            {
                if (i + 1 < parts.Length)
                    return parts[i + 1];
            }
        }

        return Path.GetFileNameWithoutExtension(relativePath);
    }

    private void CleanupOldBackups(string originalFilePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(originalFilePath);
            if (directory == null) return;

            var fileName = Path.GetFileName(originalFilePath);
            var backupFiles = Directory.GetFiles(directory, $"{fileName}.backup.*")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Skip(5); // Keep only 5 most recent

            foreach (var oldBackup in backupFiles)
            {
                try
                {
                    File.Delete(oldBackup);
                }
                catch { }
            }
        }
        catch { }
    }
}

public class ConfigurationFile
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public long SizeInBytes { get; set; }
    
    public string SizeFormatted => SizeInBytes < 1024 
        ? $"{SizeInBytes} B" 
        : $"{SizeInBytes / 1024.0:F1} KB";
}
