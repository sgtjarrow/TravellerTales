using System.IO;

namespace TravellerTales;

public static class AppPaths
{
    public static string ApplicationDataDirectory { get; private set; } = string.Empty;
    public static string CharactersDirectory { get; private set; } = string.Empty;
    public static string HomeworldsDirectory { get; private set; } = string.Empty;

    public static void Initialize(DataSettings settings)
    {
        ApplicationDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            settings.ApplicationFolderName);

        CharactersDirectory = Path.Combine(ApplicationDataDirectory, settings.CharactersFolderName);
        HomeworldsDirectory = Path.Combine(ApplicationDataDirectory, settings.HomeworldsFolderName);

        Directory.CreateDirectory(ApplicationDataDirectory);
        Directory.CreateDirectory(CharactersDirectory);
        Directory.CreateDirectory(HomeworldsDirectory);
    }
}
