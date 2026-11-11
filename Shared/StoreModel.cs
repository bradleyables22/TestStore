using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace TestStore.Shared;

public sealed record Product(string Id, string Name, string Category, decimal Price, string Color, string Description);

public static class Products
{
    public static readonly Product[] All = [
        new("mug", "Everyday mug", "DESK", 18m, "#e5c6a5", "A generously sized ceramic mug for the next cup of coffee."),
        new("notebook", "Pocket notebook", "PAPER", 9m, "#b8c4a5", "Blank pages, a soft cover, and room for a small idea."),
        new("tote", "Weekend tote", "CARRY", 24m, "#b7c9d1", "A lightweight cotton bag for whatever the day brings."),
        new("bottle", "Trail bottle", "OUTDOORS", 28m, "#d5b8b8", "An insulated bottle for your desk or your next walk.")
    ];
    public static Product? Find(string? id) => All.FirstOrDefault(p => p.Id == id);
}

public sealed class StoreRequest
{
    public string CartId { get; set; } = "";
    // A route on the containing document origin, supplied by its shell.
    public string CartPath { get; set; } = "";
    public string? ProductId { get; set; }
    public string? OperationId { get; set; }
    public int Quantity { get; set; }
    public void Validate()
    {
        if (!Guid.TryParseExact(CartId, "N", out _)) throw new ArgumentException("Invalid demo cart ID.");
    }
}
