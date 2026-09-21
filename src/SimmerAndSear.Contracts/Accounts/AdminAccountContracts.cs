using System.ComponentModel.DataAnnotations;

namespace SimmerAndSear.Contracts.Accounts;

//References
//Microsoft, 2026. Model validation in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2025. Records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record> [Accessed 21 September 2026].

//OWASP Foundation, n.d. Mass Assignment - OWASP Cheat Sheet Series [Source code].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Mass_Assignment_Cheat_Sheet.html> [Accessed 21 September 2026].

public sealed class CreateAdminAccountRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }
}

public sealed class UpdateAdminAccountRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }
}

public sealed record AdminAccountResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLogin);
