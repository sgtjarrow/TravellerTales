using System.IO;

namespace TravellerTales;

public static class AppPaths
{
    public static string ApplicationDataDirectory { get; private set; } = string.Empty;
    public static string CharactersDirectory { get; private set; } = string.Empty;

    public static void Initialize(DataSettings settings)
    {
        ApplicationDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            settings.ApplicationFolderName);

        CharactersDirectory = Path.Combine(ApplicationDataDirectory, settings.CharactersFolderName);

        Directory.CreateDirectory(ApplicationDataDirectory);
        Directory.CreateDirectory(CharactersDirectory);
    }
}
