using System.IO;
using System.Text.Json;

namespace TravellerTales;

public sealed class AppSettings
{
    public int SplashDurationSeconds { get; set; } = 2;
    public string ApplicationVersion { get; set; } = "0.3.0";
    public string BuildDate { get; set; } = "2026-Jul-08";
    public DataSettings Data { get; set; } = new();

    public static AppSettings Load()
    {
        var configPath = GetConfigPath();
        if (!File.Exists(configPath))
        {
            return new AppSettings();
        }

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new AppSettings();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(GetConfigPath(), json);
    }

    private static string GetConfigPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }
}

public sealed class DataSettings
{
    public string ApplicationFolderName { get; set; } = "Traveller Tales";
    public string CharactersFolderName { get; set; } = "Characters";
    public string HomeworldsFolderName { get; set; } = "Homeworlds";
}
