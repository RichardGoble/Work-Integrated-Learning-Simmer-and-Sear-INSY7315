namespace SimmerAndSear.Domain.Enums;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Enumeration types - C# reference [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public enum UserType { Client, Admin }
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Enumeration types - C# reference [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public enum RoleName { Customer, SalesAdmin, MasterAdmin }
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Enumeration types - C# reference [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public enum Permission { CatalogueManage, InquiryManage, AccountManage, AuditRead, SettingsManage }
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Enumeration types - C# reference [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public enum OrderType { Checkout, QuoteRequest }

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Enumeration types - C# reference [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public enum OrderStatus { Pending, AwaitingPayment, Paid, Processing, Shipped, Delivered, Cancelled, Refunded }

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Enumeration types - C# reference [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public enum PaymentStatus { Initiated, Confirmed, Failed, Refunded }
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Enumeration types - C# reference [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public enum ReviewStatus { Pending, Approved, Rejected }

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Enumeration types - C# reference [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public enum PostStatus { Draft, Published, Archived }

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Enumeration types - C# reference [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public enum BookingStatus { Requested, Confirmed, Completed, Cancelled }

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Enumeration types - C# reference [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public enum ContactStatus { New, InProgress, Resolved, Closed }
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Enumeration types - C# reference [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public enum AuditAction { Create, Update, Delete, Publish, Login, AccessDenied, Logout }
