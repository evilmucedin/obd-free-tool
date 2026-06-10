using System.Text.Json;
using System.Text.Json.Serialization;

namespace ObdFree.Core.Config;

/// <summary>
/// Loads and saves <see cref="AppConfig"/> as JSON. Defaults to a per-user file
/// under the OS config directory (e.g. <c>~/.config/obd-free-tool/config.json</c>
/// on Linux/macOS, <c>%APPDATA%\obd-free-tool\config.json</c> on Windows).
/// </summary>
public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;

    /// <summary>Creates a store at the default per-user path, or a custom path (for tests).</summary>
    /// <param name="path">Optional override for the config file path.</param>
    public ConfigStore(string? path = null) => _path = path ?? DefaultPath;

    /// <summary>Gets the default per-user config file path.</summary>
    public static string DefaultPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "obd-free-tool",
        "config.json");

    /// <summary>Gets the path this store reads from and writes to.</summary>
    public string Path => _path;

    /// <summary>Loads the config, returning defaults if the file is missing or invalid.</summary>
    /// <returns>The loaded (or default) config.</returns>
    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new AppConfig();
            }

            string json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppConfig();
        }
    }

    /// <summary>Saves the config, creating the directory if needed.</summary>
    /// <param name="config">The config to persist.</param>
    public void Save(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        string? dir = System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(config, JsonOptions));
    }
}
