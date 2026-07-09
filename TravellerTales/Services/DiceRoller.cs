namespace TravellerTales.Services;

public static class DiceRoller
{
    public static int Roll(int sides)
    {
        if (sides < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sides), sides, "Die must have at least one side.");
        }

        return sides == 10
            ? Random.Shared.Next(0, 10)
            : Random.Shared.Next(1, sides + 1);
    }

    public static int Roll(int count, int sides, int modifier = 0)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "At least one die must be rolled.");
        }

        var total = modifier;

        for (var index = 0; index < count; index++)
        {
            total += Roll(sides);
        }

        return total;
    }
}
