using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Data;

public static class CategoryDefaults
{
    public static readonly (string Name, string Color)[] Presets =
    {
        ("Travel", "#3f51b5"),
        ("Meals", "#f44336"),
        ("Office Supplies", "#ff9800"),
        ("Software", "#4caf50"),
        ("Utilities", "#009688"),
        ("Other", "#9e9e9e")
    };

    public static IEnumerable<Category> ForUser(int userId) =>
        Presets.Select(p => new Category { UserId = userId, Name = p.Name, Color = p.Color });
}
