namespace MealPlanner.Abstractions;

/// <summary>
/// Nutritional information for a recipe (per serving).
/// </summary>
public sealed record NutritionInfo
{
    public double? Calories { get; init; }
    public double? ProteinGrams { get; init; }
    public double? FatGrams { get; init; }
    public double? CarbGrams { get; init; }
    public double? FiberGrams { get; init; }
}
