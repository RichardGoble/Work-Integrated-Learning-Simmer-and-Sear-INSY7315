using System.Collections.Generic;

namespace SimmerAndSear.Web.ViewModels.Components;

public record CardVm(
    string Title,
    string Href,
    string ImageUrl,
    string AltText,
    string? Meta = null,
    string? Badge = null
);

public record FieldVm(
    string Label,
    string Name,
    string Type = "text",
    string? Value = null,
    string? Hint = null,
    string? Error = null,
    bool Required = false
);

public record AlertVm(
    string Severity,
    string Title,
    string Message
);

public record PaginationVm(
    int Current,
    int TotalPages,
    string BaseUrl
);

public record EmptyStateVm(
    string Heading,
    string Body,
    string? ActionText = null,
    string? ActionHref = null
);

public record LoadingVm(
    string Label = "Loading..."
);

public record BreadcrumbItem(string Label, string Href);
public record BreadcrumbVm(
    List<BreadcrumbItem> Items
);

public record ModalVm(
    string Id,
    string Title,
    string Body
);
