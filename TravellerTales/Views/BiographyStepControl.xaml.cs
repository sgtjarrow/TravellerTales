using System.Windows;
using System.Windows.Controls;
using TravellerTales.Models;
using TravellerTales.Services;

namespace TravellerTales.Views;

public partial class BiographyStepControl : UserControl
{
    private Character _character = new();
    private bool _isLoading;

    public event EventHandler? ValidityChanged;

    public BiographyStepControl()
    {
        InitializeComponent();
        RaceComboBox.ItemsSource = Enum.GetValues<RaceType>();
        HeritageComboBox.ItemsSource = Enum.GetValues<HeritageType>();
        GenderComboBox.ItemsSource = Enum.GetValues<GenderType>();
    }

    public void LoadCharacter(Character character)
    {
        _character = character;
        ApplyDefaults(character);
        _isLoading = true;

        NameTextBox.Text = character.Name;
        RaceComboBox.SelectedItem = character.Race;
        HeritageComboBox.SelectedItem = character.Heritage;
        GenderComboBox.SelectedItem = character.Gender;
        AgeTextBox.Text = character.Age.ToString();
        HeightStepper.Value = Math.Max(1, character.HeightInches);
        WeightStepper.Value = Math.Max(1, character.WeightPounds);
        EyeColorTextBox.Text = character.EyeColor;
        HairColorTextBox.Text = character.HairColor ?? string.Empty;
        FurPatternTextBox.Text = character.FurPattern ?? string.Empty;
        FurPrimaryColorTextBox.Text = character.FurPrimaryColor ?? string.Empty;
        FurSecondaryColorTextBox.Text = character.FurSecondaryColor ?? string.Empty;
        DescriptionTextBox.Text = character.Description;
        NotesTextBox.Text = character.Notes;

        _isLoading = false;
        UpdateRaceSpecificFields(clearHiddenValues: false);
        UpdateMetricReadouts();
        Validate(showMessage: false);
    }

    public bool TryCommitRequired()
    {
        CaptureFields();
        return Validate(showMessage: true);
    }

    public void CommitPartial()
    {
        CaptureFields();
        Validate(showMessage: false);
    }

    public bool IsComplete()
    {
        CaptureFields();
        return Validate(showMessage: false);
    }

    private void CaptureFields()
    {
        _character.Name = NameTextBox.Text.Trim();
        _character.Race = RaceComboBox.SelectedItem is RaceType race ? race : RaceType.Human;
        _character.Heritage = HeritageComboBox.SelectedItem is HeritageType heritage ? heritage : null;
        _character.Gender = GenderComboBox.SelectedItem is GenderType gender ? gender : GenderType.Male;
        _character.Age = 18;
        _character.HeightInches = Math.Max(1, HeightStepper.Value);
        _character.WeightPounds = Math.Max(1, WeightStepper.Value);
        _character.EyeColor = EyeColorTextBox.Text.Trim();
        _character.HairColor = NullIfWhiteSpace(HairColorTextBox.Text);
        _character.FurPattern = NullIfWhiteSpace(FurPatternTextBox.Text);
        _character.FurPrimaryColor = NullIfWhiteSpace(FurPrimaryColorTextBox.Text);
        _character.FurSecondaryColor = NullIfWhiteSpace(FurSecondaryColorTextBox.Text);
        _character.Description = DescriptionTextBox.Text.Trim();
        _character.Notes = NotesTextBox.Text.Trim();

        ApplyRaceFieldClearing();
        UpdateMetricReadouts();
    }

    private bool Validate(bool showMessage)
    {
        var message = GetValidationMessage();
        if (showMessage || string.IsNullOrWhiteSpace(message))
        {
            ValidationMessage.Text = message;
        }

        return string.IsNullOrWhiteSpace(message);
    }

    private string GetValidationMessage()
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(CharacterFileService.SanitizeCharacterName(NameTextBox.Text)))
        {
            return "Name must contain at least one letter or number.";
        }

        if (CharacterFileService.FinalCharacterExists(NameTextBox.Text))
        {
            return "A finalized character with this name already exists.";
        }

        if (RaceComboBox.SelectedItem is not RaceType)
        {
            return "Race is required.";
        }

        if (GenderComboBox.SelectedItem is not GenderType)
        {
            return "Gender is required.";
        }

        if (HeightStepper.Value < 1)
        {
            return "Height must be a positive whole number.";
        }

        if (WeightStepper.Value < 1)
        {
            return "Weight must be a positive whole number.";
        }

        if (string.IsNullOrWhiteSpace(EyeColorTextBox.Text))
        {
            return "Eye color is required.";
        }

        if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
        {
            return "Description is required.";
        }

        return string.Empty;
    }

    private void OnRaceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        UpdateRaceSpecificFields(clearHiddenValues: true);
        OnFieldChanged(sender, e);
    }

    private void OnFieldChanged(object sender, EventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        ValidationMessage.Text = string.Empty;
        UpdateMetricReadouts();
        ValidityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateRaceSpecificFields(bool clearHiddenValues)
    {
        var race = RaceComboBox.SelectedItem is RaceType selectedRace ? selectedRace : RaceType.Human;
        var isHuman = race == RaceType.Human;

        HumanDetailsCard.Visibility = isHuman ? Visibility.Visible : Visibility.Collapsed;
        NonHumanDetailsCard.Visibility = isHuman ? Visibility.Collapsed : Visibility.Visible;

        if (!clearHiddenValues)
        {
            return;
        }

        if (isHuman)
        {
            FurPatternTextBox.Text = string.Empty;
            FurPrimaryColorTextBox.Text = string.Empty;
            FurSecondaryColorTextBox.Text = string.Empty;
        }
        else
        {
            HeritageComboBox.SelectedItem = null;
            HairColorTextBox.Text = string.Empty;
        }
    }

    private void UpdateMetricReadouts()
    {
        HeightMetersText.Text = $"{HeightStepper.Value * 0.0254:0.00}";
        WeightKilogramsText.Text = $"{WeightStepper.Value * 0.45359237:0.00}";
    }

    private void ApplyRaceFieldClearing()
    {
        if (_character.Race == RaceType.Human)
        {
            _character.FurPattern = null;
            _character.FurPrimaryColor = null;
            _character.FurSecondaryColor = null;
        }
        else
        {
            _character.Heritage = null;
            _character.HairColor = null;
        }
    }

    private static string? NullIfWhiteSpace(string value)
    {
        var trimmedValue = value.Trim();
        return string.IsNullOrWhiteSpace(trimmedValue) ? null : trimmedValue;
    }

    private static void ApplyDefaults(Character character)
    {
        character.Heritage ??= HeritageType.Anglo;
        character.Age = 18;

        if (character.HeightInches < 1)
        {
            character.HeightInches = 1;
        }

        if (character.WeightPounds < 1)
        {
            character.WeightPounds = 1;
        }
    }
}
