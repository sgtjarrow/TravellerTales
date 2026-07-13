using System.Globalization;
using System.IO;
using TravellerTales;
using TravellerTales.Models;
using TravellerTales.Services;
using TravellerTales.Views;
using Xunit;

namespace TravellerTales.Tests;

public sealed class FileLifecycleTests : IDisposable
{
    private readonly string _applicationFolderName = $"TravellerTalesTests-{Guid.NewGuid():N}";

    public FileLifecycleTests()
    {
        AppPaths.Initialize(new DataSettings
        {
            ApplicationFolderName = _applicationFolderName,
            CharactersFolderName = "Characters",
            HomeworldsFolderName = "Homeworlds"
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(AppPaths.ApplicationDataDirectory))
        {
            Directory.Delete(AppPaths.ApplicationDataDirectory, recursive: true);
        }
    }

    [Fact]
    public void DisabledReasonToolTipConverter_BlankReasonReturnsNull()
    {
        var converter = new DisabledReasonToolTipConverter();

        Assert.Null(converter.Convert(string.Empty, typeof(object), null, CultureInfo.InvariantCulture));
        Assert.Equal(
            "Already at the character creation maximum.",
            converter.Convert("Already at the character creation maximum.", typeof(object), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void HomeworldDuplicateValidation_IgnoresOrphanedHomeworldFiles()
    {
        HomeworldFileService.SaveHomeworld(new Homeworld { Id = "orphan", Name = "Duplicate" }, new HashSet<string>());

        var exists = HomeworldFileService.HomeworldExists("Duplicate", "candidate", new HashSet<string>());

        Assert.False(exists);
    }

    [Fact]
    public void HomeworldDuplicateValidation_BlocksReferencedHomeworldFiles()
    {
        HomeworldFileService.SaveHomeworld(new Homeworld { Id = "referenced", Name = "Duplicate" }, new HashSet<string>());
        CharacterFileService.SaveFinalCharacter(CreateCompletedCharacter("Ada", "Reference", "referenced"));

        var exists = HomeworldFileService.HomeworldExists(
            "Duplicate",
            "candidate",
            CharacterFileService.GetReferencedFinalHomeworldIds());

        Assert.True(exists);
    }

    [Fact]
    public void FinalSave_SavesHomeworldAndCharacterThenRemovesPausedCreation()
    {
        var state = new CharacterCreationState
        {
            Character = CreateCompletedCharacter("Final", "Traveller", string.Empty)
        };
        state.Character.Homeworld = new Homeworld { Name = "Finalhome" };
        CharacterFileService.SavePausedCreation(state);

        HomeworldFileService.SaveHomeworld(state.Character.Homeworld, CharacterFileService.GetReferencedFinalHomeworldIds());
        state.Character.HomeworldId = state.Character.Homeworld.Id;
        CharacterFileService.SaveFinalCharacter(state.Character);
        CharacterFileService.DeletePausedCreation();

        Assert.False(CharacterFileService.HasPausedCreation());
        Assert.True(File.Exists(HomeworldFileService.GetHomeworldPath("Finalhome")));
        Assert.True(File.Exists(CharacterFileService.GetFinalCharacterPath(state.Character)));
    }

    [Fact]
    public void DeleteOrphanedHomeworlds_DeletesUnreferencedAndPreservesReferenced()
    {
        HomeworldFileService.SaveHomeworld(new Homeworld { Id = "referenced", Name = "Referenced" }, new HashSet<string>());
        HomeworldFileService.SaveHomeworld(new Homeworld { Id = "orphan", Name = "Orphan" }, new HashSet<string>());
        CharacterFileService.SaveFinalCharacter(CreateCompletedCharacter("Home", "Owner", "referenced"));

        var result = HomeworldFileService.DeleteOrphanedHomeworlds(CharacterFileService.GetReferencedFinalHomeworldIds());

        Assert.Equal(1, result.DeletedCount);
        Assert.True(File.Exists(HomeworldFileService.GetHomeworldPath("Referenced")));
        Assert.False(File.Exists(HomeworldFileService.GetHomeworldPath("Orphan")));
    }

    [Fact]
    public void DeleteOrphanedHomeworlds_PausedCreationDoesNotProtectHomeworld()
    {
        var homeworld = new Homeworld { Id = "paused", Name = "Paused" };
        HomeworldFileService.SaveHomeworld(homeworld, new HashSet<string>());
        CharacterFileService.SavePausedCreation(new CharacterCreationState
        {
            Character = new Character
            {
                HomeworldId = homeworld.Id,
                Homeworld = homeworld
            }
        });

        var result = HomeworldFileService.DeleteOrphanedHomeworlds(CharacterFileService.GetReferencedFinalHomeworldIds());

        Assert.Equal(1, result.DeletedCount);
        Assert.False(File.Exists(HomeworldFileService.GetHomeworldPath("Paused")));
    }

    private static Character CreateCompletedCharacter(string firstName, string lastName, string homeworldId)
    {
        return new Character
        {
            HumanFirstName = firstName,
            HumanLastName = lastName,
            HomeworldId = homeworldId,
            CreationMetadata = new CharacterCreationMetadata
            {
                Status = CharacterCreationStatus.Complete,
                CreateFinalizedDateTime = DateTime.Now
            }
        };
    }
}
