namespace MealPlanner.Abstractions;

/// <summary>
/// A meal plan composed of one or more days, each with assigned recipes.
/// </summary>
public sealed record MealPlan
{
    public required string Title { get; init; }
    public IReadOnlyList<MealPlanDay> Days { get; init; } = [];
    public string? Notes { get; init; }
}

/// <summary>
/// A single day in a meal plan with meals organized by course.
/// </summary>
public sealed record MealPlanDay
{
    public required string Label { get; init; }
    public IReadOnlyList<MealPlanEntry> Meals { get; init; } = [];
}

/// <summary>
/// An individual meal entry within a day.
/// </summary>
public sealed record MealPlanEntry
{
    public required string Course { get; init; }
    public required Recipe Recipe { get; init; }
    public int? Servings { get; init; }
    public string? AgentNotes { get; init; }
}
