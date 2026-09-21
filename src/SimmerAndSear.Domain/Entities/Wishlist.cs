using SimmerAndSear.Domain.Abstractions;

namespace SimmerAndSear.Domain.Entities;
//References
//Microsoft, 2026. Classes, structs, and records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/> [Accessed 21 September 2026].

//MongoDB, n.d. Embedded Data Versus References [Source code].
//Available at: <https://www.mongodb.com/docs/manual/data-modeling/concepts/embedding-vs-references/> [Accessed 21 September 2026].


public sealed class Wishlist : IEntity
{
    public const int MaxItems = 100;

    public string Id { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public List<WishlistItem> Items { get; set; } = [];

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool Add(string productId, string productName, decimal unitPrice)
    {
        if (Items.Any(i => i.ProductId == productId))
        {
            return true;
        }

        if (Items.Count >= MaxItems)
        {
            return false;
        }

        Items.Add(new WishlistItem
        {
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            AddedAt = DateTime.UtcNow
        });

        Touch();
        return true;
    }

    public bool Remove(string productId)
    {
        var removed = Items.RemoveAll(i => i.ProductId == productId) > 0;

        if (removed)
        {
            Touch();
        }

        return removed;
    }

    public void Clear()
    {
        Items.Clear();
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}

public sealed class WishlistItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
