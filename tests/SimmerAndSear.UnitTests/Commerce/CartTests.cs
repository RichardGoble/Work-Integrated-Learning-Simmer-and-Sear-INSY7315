using Shouldly;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.UnitTests.Commerce;


public sealed class CartTests
{
    private static Cart CartWith(params (decimal Price, int Qty)[] lines)
    {
        var cart = new Cart();

        for (var i = 0; i < lines.Length; i++)
        {
            cart.AddItem($"product-{i}", $"Product {i}", lines[i].Price, lines[i].Qty);
        }

        return cart;
    }

    [Fact]
    public void Subtotal_is_the_sum_of_the_lines()
    {
        CartWith((100m, 2), (49.50m, 4)).Subtotal.ShouldBe(398.00m);
    }

    [Fact]
    public void Adding_the_same_product_twice_merges_into_one_line()
    {
        var cart = CartWith((100m, 2));
        cart.AddItem("product-0", "Product 0", 100m, 3);

        cart.Items.Count.ShouldBe(1);
        cart.Items[0].Quantity.ShouldBe(5);
        cart.Subtotal.ShouldBe(500m);
    }

    [Fact]
    public void A_quantity_of_zero_or_less_is_ignored_rather_than_creating_an_empty_line()
    {
        var cart = new Cart();
        cart.AddItem("product-0", "Product 0", 100m, 0);
        cart.AddItem("product-1", "Product 1", 100m, -3);

        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Setting_a_quantity_to_zero_removes_the_line()
    {
        var cart = CartWith((100m, 2), (50m, 1));
        cart.SetQuantity("product-0", 0);

        cart.Items.Count.ShouldBe(1);
        cart.Subtotal.ShouldBe(50m);
    }


    [Theory]
    [InlineData(4999.99, false)]   // below
    [InlineData(5000.00, true)]    // exactly at - inclusive, so a bulk order
    [InlineData(5000.01, true)]    // above
    public void The_threshold_is_inclusive(decimal subtotal, bool expected)
    {
        CartWith((subtotal, 1)).ExceedsThreshold(5000m).ShouldBe(expected);
    }

    [Fact]
    public void An_unset_threshold_never_diverts_an_order()
    {

        CartWith((10_000m, 1)).ExceedsThreshold(0m).ShouldBeFalse();
    }

    [Fact]
    public void Merging_an_anonymous_cart_adds_quantities_rather_than_replacing_them()
    {
        var signedIn = CartWith((100m, 2));
        var anonymous = CartWith((100m, 3));

        signedIn.MergeFrom(anonymous);

       
        signedIn.Items.Count.ShouldBe(1);
        signedIn.Items[0].Quantity.ShouldBe(5);
    }

    [Fact]
    public void Merging_brings_across_products_the_signed_in_cart_did_not_have()
    {
        var signedIn = new Cart();
        signedIn.AddItem("wok", "Carbon Steel Wok", 679m, 1);

        var anonymous = new Cart();
        anonymous.AddItem("skillet", "Cast Iron Skillet", 949m, 2);

        signedIn.MergeFrom(anonymous);

        signedIn.Items.Count.ShouldBe(2);
        signedIn.Subtotal.ShouldBe(679m + 1898m);
    }
}
