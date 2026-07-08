using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TravellerTales.Models;

namespace TravellerTales.Services;

public static class CharacterFileService
{
    private const string PausedCreationFileName = "CharacterCreationPaused.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string PausedCreationPath => Path.Combine(AppPaths.CharactersDirectory, PausedCreationFileName);

    public static bool HasPausedCreation()
    {
        return File.Exists(PausedCreationPath);
    }

    public static CharacterCreationState? LoadPausedCreation()
    {
        if (!HasPausedCreation())
        {
            return null;
        }

        var json = File.ReadAllText(PausedCreationPath);
        return JsonSerializer.Deserialize<CharacterCreationState>(json, JsonOptions);
    }

    public static void SavePausedCreation(CharacterCreationState state)
    {
        Directory.CreateDirectory(AppPaths.CharactersDirectory);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(PausedCreationPath, json);
    }

    public static void DeletePausedCreation()
    {
        if (HasPausedCreation())
        {
            File.Delete(PausedCreationPath);
        }
    }

    public static string SanitizeCharacterName(string name)
    {
        var builder = new StringBuilder();

        foreach (var character in name)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    public static string GetFinalCharacterPath(string name)
    {
        return Path.Combine(AppPaths.CharactersDirectory, $"{SanitizeCharacterName(name)}.json");
    }

    public static bool FinalCharacterExists(string name)
    {
        var sanitizedName = SanitizeCharacterName(name);
        return !string.IsNullOrWhiteSpace(sanitizedName) && File.Exists(GetFinalCharacterPath(name));
    }
}
