using System.Collections.Generic;

namespace SimmerAndSear.Web.ViewModels.Components;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# record types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public record CardVm(
    string Title,
    string Href,
    string ImageUrl,
    string AltText,
    string? Meta = null,
    string? Badge = null
);

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# record types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public record FieldVm(
    string Label,
    string Name,
    string Type = "text",
    string? Value = null,
    string? Hint = null,
    string? Error = null,
    bool Required = false
);

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# record types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public record AlertVm(
    string Severity,
    string Title,
    string Message
);

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# record types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public record PaginationVm(
    int Current,
    int TotalPages,
    string BaseUrl
);

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# record types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public record EmptyStateVm(
    string Heading,
    string Body,
    string? ActionText = null,
    string? ActionHref = null
);

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# record types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public record LoadingVm(
    string Label = "Loading..."
);

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# record types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public record BreadcrumbItem(string Label, string Href);
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# record types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public record BreadcrumbVm(
    List<BreadcrumbItem> Items
);

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# record types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public record ModalVm(
    string Id,
    string Title,
    string Body
);
