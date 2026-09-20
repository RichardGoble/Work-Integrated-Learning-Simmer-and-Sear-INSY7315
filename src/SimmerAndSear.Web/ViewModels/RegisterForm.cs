using System.ComponentModel.DataAnnotations;
using SimmerAndSear.Contracts.Accounts;

namespace SimmerAndSear.Web.ViewModels;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Model validation in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0> [Accessed 20 September 2026].
//
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public sealed class RegisterForm
{
    [Required(ErrorMessage = "Enter your full name."), StringLength(160, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12,
        ErrorMessage = "Use a password of at least 12 characters.")]
    public string Password { get; set; } = string.Empty;

    public RegisterRequest? ToRequest()
    {
        var name = FullName.Trim();
        var split = name.LastIndexOf(' ');

        if (split <= 0)
        {
            return null;
        }

        return new RegisterRequest
        {
            FirstName = name[..split].Trim(),
            LastName = name[(split + 1)..].Trim(),
            Email = Email.Trim(),
            Password = Password
        };
    }
}
