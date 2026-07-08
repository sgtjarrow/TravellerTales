using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TravellerTales.Models;
using TravellerTales.Services;

namespace TravellerTales.Views;

public partial class BiographyStepControl : UserControl
{
    private Character _character = new();
    private bool _isLoading;
    private static readonly Brush ValidBorderBrush = (Brush)new BrushConverter().ConvertFromString("#5FD9FF")!;
    private static readonly Brush InvalidBorderBrush = (Brush)new BrushConverter().ConvertFromString("#D93C3C")!;
    private static readonly Dictionary<(RaceType Race, GenderType Gender), BodyRange> BodyRanges = new()
    {
        [(RaceType.Human, GenderType.Male)] = new(52, 86, 149, 249),
        [(RaceType.Human, GenderType.Female)] = new(48, 79, 129, 215),
        [(RaceType.Aslan, GenderType.Male)] = new(50, 125, 248, 713),
        [(RaceType.Aslan, GenderType.Female)] = new(41, 86, 203, 500),
        [(RaceType.Vargr, GenderType.Male)] = new(27, 75, 53, 181),
        [(RaceType.Vargr, GenderType.Female)] = new(27, 75, 45, 125)
    };
    private static readonly FurColorType[] AslanFurColors =
    [
        FurColorType.Black,
        FurColorType.Cream,
        FurColorType.DarkBrown,
        FurColorType.Golden,
        FurColorType.LightBrown,
        FurColorType.Sandy,
        FurColorType.Tawny,
        FurColorType.White
    ];
    private static readonly FurColorType[] VargrFurColors =
    [
        FurColorType.Black,
        FurColorType.Brown,
        FurColorType.Cream,
        FurColorType.DarkGray,
        FurColorType.Gray,
        FurColorType.RedBrown,
        FurColorType.Silver,
        FurColorType.Tan,
        FurColorType.White
    ];
    private static readonly FurPatternType[] AslanFurPatterns =
    [
        FurPatternType.ManeDark,
        FurPatternType.ManeLight,
        FurPatternType.Rosetted,
        FurPatternType.Solid,
        FurPatternType.Spotted,
        FurPatternType.Striped,
        FurPatternType.Tufted,
        FurPatternType.Albino
    ];
    private static readonly FurPatternType[] VargrFurPatterns =
    [
        FurPatternType.Agouti,
        FurPatternType.Blanket,
        FurPatternType.Brindle,
        FurPatternType.Masked,
        FurPatternType.Merle,
        FurPatternType.Sable,
        FurPatternType.Solid,
        FurPatternType.WolfGray,
        FurPatternType.Albino
    ];

    public event EventHandler? ValidityChanged;

    public BiographyStepControl()
    {
        InitializeComponent();
        RaceComboBox.ItemsSource = SortComboValues(Enum.GetValues<RaceType>());
        HeritageComboBox.ItemsSource = SortComboValues(Enum.GetValues<HeritageType>());
        GenderComboBox.ItemsSource = SortComboValues(Enum.GetValues<GenderType>());
        EyeColorComboBox.ItemsSource = SortComboValues(Enum.GetValues<EyeColorType>());
        SkinColorComboBox.ItemsSource = SortComboValues(Enum.GetValues<SkinColorType>());
        HairColorComboBox.ItemsSource = SortComboValues(Enum.GetValues<HairColorType>());
        VargrRoleComboBox.ItemsSource = SortComboValues(Enum.GetValues<VargrRoleType>());
    }

    public void LoadCharacter(Character character)
    {
        _character = character;
        ApplyDefaults(character);
        _isLoading = true;

        SetNameFields(character);
        RaceComboBox.SelectedItem = character.Race;
        HeritageComboBox.SelectedItem = character.Heritage;
        GenderComboBox.SelectedItem = character.Gender;
        SkinColorComboBox.SelectedItem = character.SkinColor;
        AgeTextBox.Text = character.Age.ToString();
        ApplyBodyRange();
        HeightStepper.Value = Math.Max(1, character.HeightInches);
        WeightStepper.Value = Math.Max(1, character.WeightPounds);
        EyeColorComboBox.SelectedItem = character.EyeColor;
        HairColorComboBox.SelectedItem = character.HairColor;
        DescriptionTextBox.Text = character.Description;
        NotesTextBox.Text = character.Notes;

        UpdateRaceSpecificFields(clearHiddenValues: false);
        FurPatternComboBox.SelectedItem = character.FurPattern;
        FurPrimaryColorComboBox.SelectedItem = character.FurPrimaryColor;
        FurSecondaryColorComboBox.SelectedItem = character.FurSecondaryColor;
        UpdateAppearanceLocks();
        UpdateMetricReadouts();
        UpdatePortrait();
        _isLoading = false;
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
        CaptureNameFields(_character);
        _character.Race = RaceComboBox.SelectedItem is RaceType race ? race : RaceType.Human;
        _character.Heritage = HeritageComboBox.SelectedItem is HeritageType heritage ? heritage : null;
        _character.Gender = GenderComboBox.SelectedItem is GenderType gender ? gender : GenderType.Male;
        _character.Age = 18;
        _character.HeightInches = Math.Max(1, HeightStepper.Value);
        _character.WeightPounds = Math.Max(1, WeightStepper.Value);
        _character.EyeColor = EyeColorComboBox.SelectedItem is EyeColorType eyeColor
            ? eyeColor
            : EyeColorType.Brown;
        _character.SkinColor = SkinColorComboBox.SelectedItem is SkinColorType skinColor
            ? skinColor
            : SkinColorType.Fair;
        _character.HairColor = HairColorComboBox.SelectedItem is HairColorType hairColor ? hairColor : null;
        _character.FurPattern = FurPatternComboBox.SelectedItem is FurPatternType furPattern ? furPattern : null;
        _character.FurPrimaryColor = FurPrimaryColorComboBox.SelectedItem is FurColorType primaryColor ? primaryColor : null;
        _character.FurSecondaryColor = FurSecondaryColorComboBox.SelectedItem is FurColorType secondaryColor ? secondaryColor : null;
        _character.Description = DescriptionTextBox.Text.Trim();
        _character.Notes = NotesTextBox.Text.Trim();

        if (_character.Race == RaceType.Human && _character.SkinColor == SkinColorType.Albino)
        {
            _character.EyeColor = EyeColorType.Red;
            _character.HairColor = HairColorType.White;
        }
        else if (_character.Race != RaceType.Human && _character.FurPattern == FurPatternType.Albino)
        {
            _character.EyeColor = EyeColorType.Red;
            _character.FurPrimaryColor = FurColorType.White;
            _character.FurSecondaryColor = FurColorType.White;
        }
        else if (_character.FurPattern == FurPatternType.Solid)
        {
            _character.FurSecondaryColor = null;
        }

        ApplyRaceFieldClearing();
        UpdateMetricReadouts();
    }

    private bool Validate(bool showMessage)
    {
        var message = GetValidationMessage();
        UpdateRequiredFieldBorders();

        if (showMessage || string.IsNullOrWhiteSpace(message))
        {
            ValidationMessage.Text = message;
        }

        return string.IsNullOrWhiteSpace(message);
    }

    private string GetValidationMessage()
    {
        var candidate = BuildCharacterNameFromControls();

        if (!candidate.IsNameComplete)
        {
            return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(candidate.SanitizedDisplayName))
        {
            return "Name must contain at least one letter or number.";
        }

        if (CharacterFileService.FinalCharacterExists(candidate))
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

        return string.Empty;
    }

    private void OnRaceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        EyeColorComboBox.SelectedItem = EyeColorType.Brown;
        UpdateRaceSpecificFields(clearHiddenValues: true);
        UpdateNamePanels();
        ApplyBodyRange();
        UpdatePortrait();
        OnFieldChanged(sender, e);
    }

    private void OnGenderChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        ApplyBodyRange();
        UpdatePortrait();
        OnFieldChanged(sender, e);
    }

    private void OnGenerateName(object sender, RoutedEventArgs e)
    {
        CaptureFields();
        CharacterNameGenerator.Generate(_character);
        _isLoading = true;
        SetNameFields(_character);
        _isLoading = false;
        CaptureFields();
        Validate(showMessage: false);
        OnFieldChanged(sender, e);
    }

    private void OnSkinColorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        if (SkinColorComboBox.SelectedItem is SkinColorType.Albino)
        {
            EyeColorComboBox.SelectedItem = EyeColorType.Red;
            HairColorComboBox.SelectedItem = HairColorType.White;
        }

        UpdateAppearanceLocks();
        OnFieldChanged(sender, e);
    }

    private void OnFurPatternChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        if (FurPatternComboBox.SelectedItem is FurPatternType.Albino)
        {
            EyeColorComboBox.SelectedItem = EyeColorType.Red;
            FurPrimaryColorComboBox.SelectedItem = FurColorType.White;
            FurSecondaryColorComboBox.SelectedItem = FurColorType.White;
        }
        else if (FurPatternComboBox.SelectedItem is FurPatternType.Solid)
        {
            FurSecondaryColorComboBox.SelectedItem = null;
        }

        UpdateAppearanceLocks();
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
        UpdateRequiredFieldBorders();
        ValidityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateRaceSpecificFields(bool clearHiddenValues)
    {
        var race = RaceComboBox.SelectedItem is RaceType selectedRace ? selectedRace : RaceType.Human;
        var isHuman = race == RaceType.Human;

        HumanDetailsCard.Visibility = isHuman ? Visibility.Visible : Visibility.Collapsed;
        NonHumanDetailsCard.Visibility = isHuman ? Visibility.Collapsed : Visibility.Visible;
        UpdateNamePanels();

        if (isHuman && HeritageComboBox.SelectedItem is null)
        {
            HeritageComboBox.SelectedItem = HeritageType.Anglo;
        }

        UpdateFurComboSources(race);

        if (!clearHiddenValues)
        {
            if (isHuman)
            {
                SetHumanDefaultsIfNeeded();
            }
            else
            {
                SetNonHumanDefaultsIfNeeded();
            }

            UpdateAppearanceLocks();
            return;
        }

        if (isHuman)
        {
            FurPatternComboBox.SelectedItem = null;
            FurPrimaryColorComboBox.SelectedItem = null;
            FurSecondaryColorComboBox.SelectedItem = null;
            SetHumanDefaultsIfNeeded();
        }
        else
        {
            HeritageComboBox.SelectedItem = null;
            HairColorComboBox.SelectedItem = null;
            SetNonHumanDefaultsIfNeeded();
        }

        UpdateAppearanceLocks();
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

    private void UpdateFurComboSources(RaceType race)
    {
        var existingPattern = FurPatternComboBox.SelectedItem as FurPatternType?;
        var existingPrimary = FurPrimaryColorComboBox.SelectedItem as FurColorType?;
        var existingSecondary = FurSecondaryColorComboBox.SelectedItem as FurColorType?;

        var patterns = race == RaceType.Aslan ? AslanFurPatterns : VargrFurPatterns;
        var colors = race == RaceType.Aslan ? AslanFurColors : VargrFurColors;

        FurPatternComboBox.ItemsSource = SortComboValues(patterns);
        FurPrimaryColorComboBox.ItemsSource = SortComboValues(colors);
        FurSecondaryColorComboBox.ItemsSource = SortComboValues(colors);

        FurPatternComboBox.SelectedItem = existingPattern.HasValue && patterns.Contains(existingPattern.Value)
            ? existingPattern.Value
            : null;
        FurPrimaryColorComboBox.SelectedItem = existingPrimary.HasValue && colors.Contains(existingPrimary.Value)
            ? existingPrimary.Value
            : null;
        FurSecondaryColorComboBox.SelectedItem = existingSecondary.HasValue && colors.Contains(existingSecondary.Value)
            ? existingSecondary.Value
            : null;
    }

    private void UpdatePortrait()
    {
        var race = RaceComboBox.SelectedItem is RaceType selectedRace ? selectedRace : RaceType.Human;
        var gender = GenderComboBox.SelectedItem is GenderType selectedGender ? selectedGender : GenderType.Male;

        var fileName = (race, gender) switch
        {
            (RaceType.Human, GenderType.Female) => "human_female.png",
            (RaceType.Aslan, GenderType.Male) => "aslan_male.png",
            (RaceType.Aslan, GenderType.Female) => "aslan_female.png",
            (RaceType.Vargr, GenderType.Male) => "vargr_male.png",
            (RaceType.Vargr, GenderType.Female) => "vargr_female.png",
            _ => "human_male.png"
        };

        CharacterPortraitImage.Source = new BitmapImage(
            new Uri($"pack://application:,,,/Assets/Characters/{fileName}", UriKind.Absolute));
    }

    private void UpdateRequiredFieldBorders()
    {
        UpdateNameFieldBorders();
        SetControlValidity(RaceComboBox, RaceComboBox.SelectedItem is RaceType);
        SetControlValidity(GenderComboBox, GenderComboBox.SelectedItem is GenderType);
        HeightStepperBorder.BorderBrush = HeightStepper.Value > 0 ? ValidBorderBrush : InvalidBorderBrush;
        WeightStepperBorder.BorderBrush = WeightStepper.Value > 0 ? ValidBorderBrush : InvalidBorderBrush;
    }

    private void SetNameFields(Character character)
    {
        HumanFirstNameTextBox.Text = character.HumanFirstName;
        HumanMiddleNameTextBox.Text = character.HumanMiddleName;
        HumanLastNameTextBox.Text = character.HumanLastName;
        HumanSuffixTextBox.Text = character.HumanSuffix;
        AslanFamilyNameTextBox.Text = character.AslanFamilyName;
        AslanPersonalNameTextBox.Text = character.AslanPersonalName;
        VargrClanNameTextBox.Text = character.VargrClanName;
        VargrRoleComboBox.SelectedItem = character.VargrRole;
        VargrPersonalNameTextBox.Text = character.VargrPersonalName;
        UpdateNamePanels();
    }

    private void CaptureNameFields(Character character)
    {
        character.HumanFirstName = HumanFirstNameTextBox.Text.Trim();
        character.HumanMiddleName = HumanMiddleNameTextBox.Text.Trim();
        character.HumanLastName = HumanLastNameTextBox.Text.Trim();
        character.HumanSuffix = HumanSuffixTextBox.Text.Trim();
        character.AslanFamilyName = AslanFamilyNameTextBox.Text.Trim();
        character.AslanPersonalName = AslanPersonalNameTextBox.Text.Trim();
        character.VargrClanName = VargrClanNameTextBox.Text.Trim();
        character.VargrRole = VargrRoleComboBox.SelectedItem is VargrRoleType role ? role : null;
        character.VargrPersonalName = VargrPersonalNameTextBox.Text.Trim();
    }

    private Character BuildCharacterNameFromControls()
    {
        var character = new Character
        {
            Race = RaceComboBox.SelectedItem is RaceType race ? race : RaceType.Human
        };
        CaptureNameFields(character);
        return character;
    }

    private void UpdateNamePanels()
    {
        var race = RaceComboBox.SelectedItem is RaceType selectedRace ? selectedRace : RaceType.Human;
        HumanNamePanel.Visibility = race == RaceType.Human ? Visibility.Visible : Visibility.Collapsed;
        AslanNamePanel.Visibility = race == RaceType.Aslan ? Visibility.Visible : Visibility.Collapsed;
        VargrNamePanel.Visibility = race == RaceType.Vargr ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateNameFieldBorders()
    {
        var race = RaceComboBox.SelectedItem is RaceType selectedRace ? selectedRace : RaceType.Human;

        SetControlValidity(HumanFirstNameTextBox, race != RaceType.Human || !string.IsNullOrWhiteSpace(HumanFirstNameTextBox.Text));
        SetControlValidity(HumanMiddleNameTextBox, true);
        SetControlValidity(HumanLastNameTextBox, race != RaceType.Human || !string.IsNullOrWhiteSpace(HumanLastNameTextBox.Text));
        SetControlValidity(HumanSuffixTextBox, true);

        SetControlValidity(AslanFamilyNameTextBox, race != RaceType.Aslan || !string.IsNullOrWhiteSpace(AslanFamilyNameTextBox.Text));
        SetControlValidity(AslanPersonalNameTextBox, race != RaceType.Aslan || !string.IsNullOrWhiteSpace(AslanPersonalNameTextBox.Text));

        SetControlValidity(VargrClanNameTextBox, race != RaceType.Vargr || !string.IsNullOrWhiteSpace(VargrClanNameTextBox.Text));
        SetControlValidity(VargrRoleComboBox, race != RaceType.Vargr || VargrRoleComboBox.SelectedItem is VargrRoleType);
        SetControlValidity(VargrPersonalNameTextBox, race != RaceType.Vargr || !string.IsNullOrWhiteSpace(VargrPersonalNameTextBox.Text));
    }

    private void SetHumanDefaultsIfNeeded()
    {
        if (SkinColorComboBox.SelectedItem is null)
        {
            SkinColorComboBox.SelectedItem = SkinColorType.Tan;
        }

        if (HeritageComboBox.SelectedItem is null)
        {
            HeritageComboBox.SelectedItem = HeritageType.Anglo;
        }

        if (HairColorComboBox.SelectedItem is null)
        {
            HairColorComboBox.SelectedItem = HairColorType.Brown;
        }
    }

    private void SetNonHumanDefaultsIfNeeded()
    {
        if (FurPatternComboBox.SelectedItem is null)
        {
            FurPatternComboBox.SelectedItem = FurPatternType.Solid;
        }

        if (FurPrimaryColorComboBox.SelectedItem is null)
        {
            FurPrimaryColorComboBox.SelectedItem = FurColorType.Brown;
        }

        if (FurPatternComboBox.SelectedItem is FurPatternType.Solid)
        {
            FurSecondaryColorComboBox.SelectedItem = null;
        }
    }

    private void UpdateAppearanceLocks()
    {
        var isHuman = RaceComboBox.SelectedItem is not RaceType race || race == RaceType.Human;
        var isHumanAlbino = isHuman && SkinColorComboBox.SelectedItem is SkinColorType.Albino;
        var isNonHumanAlbino = !isHuman && FurPatternComboBox.SelectedItem is FurPatternType.Albino;
        var isSolid = !isHuman && FurPatternComboBox.SelectedItem is FurPatternType.Solid;

        if (isHumanAlbino)
        {
            EyeColorComboBox.SelectedItem = EyeColorType.Red;
            HairColorComboBox.SelectedItem = HairColorType.White;
        }

        if (isNonHumanAlbino)
        {
            EyeColorComboBox.SelectedItem = EyeColorType.Red;
            FurPrimaryColorComboBox.SelectedItem = FurColorType.White;
            FurSecondaryColorComboBox.SelectedItem = FurColorType.White;
        }
        else if (isSolid)
        {
            FurSecondaryColorComboBox.SelectedItem = null;
        }

        SetComboLocked(EyeColorComboBox, isHumanAlbino || isNonHumanAlbino);
        SetComboLocked(HairColorComboBox, isHumanAlbino);
        SetComboLocked(FurPrimaryColorComboBox, isNonHumanAlbino);
        SetComboLocked(FurSecondaryColorComboBox, isNonHumanAlbino);
        FurSecondaryColorComboBox.IsEnabled = !isSolid;
    }

    private static void SetComboLocked(ComboBox comboBox, bool locked)
    {
        comboBox.IsHitTestVisible = !locked;
        comboBox.Focusable = !locked;
    }

    private void ApplyBodyRange()
    {
        var race = RaceComboBox.SelectedItem is RaceType selectedRace ? selectedRace : RaceType.Human;
        var gender = GenderComboBox.SelectedItem is GenderType selectedGender ? selectedGender : GenderType.Male;
        var range = BodyRanges[(race, gender)];

        HeightStepper.Minimum = range.MinimumHeightInches;
        HeightStepper.Maximum = range.MaximumHeightInches;
        WeightStepper.Minimum = range.MinimumWeightPounds;
        WeightStepper.Maximum = range.MaximumWeightPounds;
        HeightStepper.Value = Math.Clamp(HeightStepper.Value, range.MinimumHeightInches, range.MaximumHeightInches);
        WeightStepper.Value = Math.Clamp(WeightStepper.Value, range.MinimumWeightPounds, range.MaximumWeightPounds);
        UpdateMetricReadouts();
    }

    private static void SetControlValidity(Control control, bool isValid)
    {
        control.BorderBrush = isValid ? ValidBorderBrush : InvalidBorderBrush;
    }

    private static void ApplyDefaults(Character character)
    {
        character.Heritage ??= HeritageType.Anglo;
        character.EyeColor = Enum.IsDefined(character.EyeColor) ? character.EyeColor : EyeColorType.Brown;
        character.SkinColor = Enum.IsDefined(character.SkinColor) ? character.SkinColor : SkinColorType.Tan;
        character.HairColor ??= HairColorType.Brown;
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

    private static IReadOnlyList<TEnum> SortComboValues<TEnum>(IEnumerable<TEnum> values)
        where TEnum : struct, Enum
    {
        return values
            .OrderBy(value => value.ToString() == "Albino" ? 1 : 0)
            .ThenBy(value => value.ToString())
            .ToArray();
    }

    private sealed record BodyRange(
        int MinimumHeightInches,
        int MaximumHeightInches,
        int MinimumWeightPounds,
        int MaximumWeightPounds);
}
