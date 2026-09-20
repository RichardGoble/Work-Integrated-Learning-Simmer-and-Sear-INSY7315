using System.Globalization;

namespace SimmerAndSear.Web.ViewModels;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2025. Standard numeric format strings [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings> [Accessed 18 September 2026].
//
//Microsoft, 2025. Globalization and localization in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────

public static class Money
{
    public static string Format(decimal amount, string currency = "ZAR") =>
        Symbol(currency) + amount.ToString("N2", CultureInfo.InvariantCulture);

    /// <summary>Whole-rand form, for round figures such as the bulk-order threshold.</summary>
    public static string FormatWhole(decimal amount, string currency = "ZAR") =>
        Symbol(currency) + amount.ToString("N0", CultureInfo.InvariantCulture);

    private static string Symbol(string currency) => currency switch
    {
        "ZAR" => "R",
        "USD" => "$",
        "GBP" => "£",
        "EUR" => "€",
        _ => currency + " "
    };
}
