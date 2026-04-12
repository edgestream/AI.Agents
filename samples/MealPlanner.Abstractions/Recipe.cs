namespace MealPlanner.Abstractions;

/// <summary>
/// Represents a single recipe with full details.
/// </summary>
public sealed record Recipe
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Image { get; init; }
    public double? Rating { get; init; }
    public int? ReviewCount { get; init; }
    public TimeSpan? PrepTime { get; init; }
    public TimeSpan? CookTime { get; init; }
    public int? Servings { get; init; }
    public IReadOnlyList<string> Ingredients { get; init; } = [];
    public IReadOnlyList<string> Instructions { get; init; } = [];
    public string? SourceUrl { get; init; }
    public NutritionInfo? Nutrition { get; init; }
}
