using SimmerAndSear.Domain.Abstractions;

namespace SimmerAndSear.Domain.Entities;

public sealed class Cart : IEntity
{
    public string Id { get; set; } = string.Empty;

    public string? ClientId { get; set; }

    public string? SessionId { get; set; }

    public List<CartItem> Items { get; set; } = [];

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public bool IsEmpty => Items.Count == 0;

    public int TotalQuantity => Items.Sum(i => i.Quantity);


    public decimal Subtotal => Items.Sum(i => i.LineTotal);

    public bool ExceedsThreshold(decimal threshold) => threshold > 0 && Subtotal >= threshold;


    public void AddItem(string productId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        var existing = Items.FirstOrDefault(i => i.ProductId == productId);

        if (existing is null)
        {
            Items.Add(new CartItem
            {
                ProductId = productId,
                ProductName = productName,
                UnitPrice = unitPrice,
                Quantity = quantity,
                AddedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Quantity += quantity;
            existing.UnitPrice = unitPrice;
        }

        Touch();
    }

    public void SetQuantity(string productId, int quantity)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == productId);

        if (existing is null)
        {
            return;
        }

        if (quantity <= 0)
        {
            Items.Remove(existing);
        }
        else
        {
            existing.Quantity = quantity;
        }

        Touch();
    }

    public void RemoveItem(string productId)
    {
        Items.RemoveAll(i => i.ProductId == productId);
        Touch();
    }

    public void Clear()
    {
        Items.Clear();
        Touch();
    }

    public void MergeFrom(Cart other)
    {
        foreach (var item in other.Items)
        {
            AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
        }

        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}

public sealed class CartItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public decimal LineTotal => UnitPrice * Quantity;
}
