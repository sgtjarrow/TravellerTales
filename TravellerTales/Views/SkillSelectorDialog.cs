using System.Windows;
using TravellerTales.Models;
using TravellerTales.Services;

namespace TravellerTales.Views;

public sealed record SkillSelectorRequest(
    Character Character,
    IEnumerable<SkillName> AllowedSkills,
    SkillAdjustmentOperation Operation,
    int Value,
    SkillAdjustmentChoiceKind ChoiceKind,
    bool AllowMultiple = false,
    bool EnforceCreationCap = true,
    bool RequireSpecialtyForTraining = true);

public sealed record SkillSelectorResult(IReadOnlyList<SelectedSkillTarget> Targets);

public sealed record SelectedSkillTarget(SkillName SkillName, string SpecialtyId);

public static class SkillSelectorDialog
{
    public static SkillSelectorResult? Show(Window owner, SkillSelectorRequest request)
    {
        var catalog = SpecialtyCatalogService.LoadCatalog();
        var window = new SkillSelectorDialogWindow(request, catalog)
        {
            Owner = owner
        };

        var dialogResult = window.ShowDialog();
        if (dialogResult != true)
        {
            return null;
        }

        SpecialtyCatalogService.SaveCatalog(catalog);
        return window.Result;
    }
}
