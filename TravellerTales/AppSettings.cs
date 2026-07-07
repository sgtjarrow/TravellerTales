using System.IO;
using System.Text.Json;

namespace TravellerTales;

public sealed class AppSettings
{
    public int SplashDurationSeconds { get; init; } = 3;
    public DataSettings Data { get; init; } = new();

    public static AppSettings Load()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
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
}

public sealed class DataSettings
{
    public string ApplicationFolderName { get; init; } = "Traveller Tales";
    public string CharactersFolderName { get; init; } = "Characters";
}
