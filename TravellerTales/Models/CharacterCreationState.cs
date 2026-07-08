namespace TravellerTales.Models;

public sealed class CharacterCreationState
{
    public int CurrentStepIndex { get; set; }
    public Character Character { get; set; } = new();
}
