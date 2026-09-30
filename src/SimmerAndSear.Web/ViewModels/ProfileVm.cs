using SimmerAndSear.Contracts.Admin;
using SimmerAndSear.Contracts.Commerce;
using SimmerAndSear.Contracts.Paging;

namespace SimmerAndSear.Web.ViewModels;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2025. Records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record> [Accessed 30 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 30 September 2026].
// ────────────────────────────────────────────────────────────────────────────

public sealed record ProfileVm(
    string Name,
    string Email,
    string Role,
    IReadOnlyList<OrderResponse> Orders,
    IReadOnlyList<ReviewResponse> Reviews)
{
    public bool IsStaff => Role is "MasterAdmin" or "SalesAdmin";

    public static ProfileVm Empty(string name, string email, string role) =>
        new(name, email, role, [], []);
}
