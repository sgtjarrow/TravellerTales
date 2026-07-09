using System.IO;
using System.Text;
using System.Text.Json;
using TravellerTales.Models;

namespace TravellerTales.Services;

public static class HomeworldFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SaveHomeworld(Homeworld homeworld)
    {
        Directory.CreateDirectory(AppPaths.HomeworldsDirectory);

        if (HomeworldExists(homeworld.Name, homeworld.Id))
        {
            throw new InvalidOperationException("A Homeworld with this name already exists.");
        }

        if (string.IsNullOrWhiteSpace(homeworld.Id))
        {
            homeworld.Id = Guid.NewGuid().ToString("N");
        }

        var path = GetHomeworldPath(homeworld);
        var json = JsonSerializer.Serialize(homeworld, JsonOptions);
        File.WriteAllText(path, json);
        DeleteRenamedHomeworldFiles(homeworld.Id, path);
        return path;
    }

    public static bool HomeworldExists(string name, string? allowedId = null)
    {
        var sanitizedName = SanitizeHomeworldName(name);

        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return false;
        }

        var path = GetHomeworldPath(name);

        if (!File.Exists(path))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(allowedId))
        {
            return true;
        }

        var existing = LoadHomeworld(path);
        return existing is null || !string.Equals(existing.Id, allowedId, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetHomeworldPath(string name)
    {
        return Path.Combine(AppPaths.HomeworldsDirectory, $"{SanitizeHomeworldName(name)}.json");
    }

    public static string GetHomeworldPath(Homeworld homeworld)
    {
        return GetHomeworldPath(homeworld.Name);
    }

    public static string SanitizeHomeworldName(string name)
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

    private static void DeleteRenamedHomeworldFiles(string id, string currentPath)
    {
        foreach (var path in Directory.EnumerateFiles(AppPaths.HomeworldsDirectory, "*.json"))
        {
            if (string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var existing = LoadHomeworld(path);

            if (existing is not null && string.Equals(existing.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(path);
            }
        }
    }

    private static Homeworld? LoadHomeworld(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Homeworld>(json, JsonOptions);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
