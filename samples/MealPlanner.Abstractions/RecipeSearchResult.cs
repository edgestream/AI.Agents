namespace MealPlanner.Abstractions;

/// <summary>
/// A lightweight recipe summary returned by search operations.
/// </summary>
public sealed record RecipeSearchResult
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Image { get; init; }
    public double? Rating { get; init; }
    public int? ReviewCount { get; init; }
    public string? SourceUrl { get; init; }
}
