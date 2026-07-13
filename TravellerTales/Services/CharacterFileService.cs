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
        Converters =
        {
            new EyeColorTypeJsonConverter(),
            new NullableEnumJsonConverter<HairColorType>(),
            new NullableEnumJsonConverter<FurPatternType>(),
            new NullableEnumJsonConverter<FurColorType>(),
            new JsonStringEnumConverter()
        }
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
        var state = JsonSerializer.Deserialize<CharacterCreationState>(json, JsonOptions);
        state?.Character.NormalizeAfterLoad();
        return state;
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

    public static string GetFinalCharacterPath(Character character)
    {
        return Path.Combine(AppPaths.CharactersDirectory, $"{character.SanitizedDisplayName}.json");
    }

    public static bool FinalCharacterExists(string name)
    {
        var sanitizedName = SanitizeCharacterName(name);
        return !string.IsNullOrWhiteSpace(sanitizedName) && File.Exists(GetFinalCharacterPath(name));
    }

    public static bool FinalCharacterExists(Character character)
    {
        return !string.IsNullOrWhiteSpace(character.SanitizedDisplayName) &&
               File.Exists(GetFinalCharacterPath(character));
    }

    public static string SaveFinalCharacter(Character character)
    {
        Directory.CreateDirectory(AppPaths.CharactersDirectory);

        if (FinalCharacterExists(character))
        {
            throw new InvalidOperationException("A Character with this name already exists.");
        }

        var path = GetFinalCharacterPath(character);
        var json = JsonSerializer.Serialize(character, JsonOptions);
        File.WriteAllText(path, json);
        return path;
    }

    public static IReadOnlySet<string> GetReferencedFinalHomeworldIds()
    {
        Directory.CreateDirectory(AppPaths.CharactersDirectory);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.EnumerateFiles(AppPaths.CharactersDirectory, "*.json"))
        {
            if (string.Equals(path, PausedCreationPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(path);
                var character = JsonSerializer.Deserialize<Character>(json, JsonOptions);
                if (character?.CreationMetadata?.Status == CharacterCreationStatus.Complete &&
                    !string.IsNullOrWhiteSpace(character.HomeworldId))
                {
                    ids.Add(character.HomeworldId);
                }
            }
            catch (Exception)
            {
                continue;
            }
        }

        return ids;
    }
}

public sealed class EyeColorTypeJsonConverter : JsonConverter<EyeColorType>
{
    public override EyeColorType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return Enum.TryParse<EyeColorType>(value, ignoreCase: true, out var eyeColor)
                ? eyeColor
                : EyeColorType.Brown;
        }

        if (reader.TokenType == JsonTokenType.Number &&
            reader.TryGetInt32(out var numericValue) &&
            Enum.IsDefined(typeof(EyeColorType), numericValue))
        {
            return (EyeColorType)numericValue;
        }

        return EyeColorType.Brown;
    }

    public override void Write(Utf8JsonWriter writer, EyeColorType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public sealed class NullableEnumJsonConverter<TEnum> : JsonConverter<TEnum?>
    where TEnum : struct, Enum
{
    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return Enum.TryParse<TEnum>(value, ignoreCase: true, out var enumValue)
                ? enumValue
                : null;
        }

        if (reader.TokenType == JsonTokenType.Number &&
            reader.TryGetInt32(out var numericValue) &&
            Enum.IsDefined(typeof(TEnum), numericValue))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), numericValue);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
