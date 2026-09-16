namespace SimmerAndSear.Domain.Enums;

/// <summary>Whether a user account is a customer or a member of staff.</summary>
public enum UserType { Client, Admin }

/// <summary>The three roles defined in the Task 1 requirements.</summary>
public enum RoleName { Customer, SalesAdmin, MasterAdmin }

/// <summary>
/// Permissions evaluated server-side before any administrative operation.
/// A refused check writes an <see cref="AuditAction.AccessDenied"/> entry
/// before returning 403.
/// </summary>
public enum Permission { CatalogueManage, InquiryManage, AccountManage, AuditRead, SettingsManage }

/// <summary>
/// Which fulfilment path an order took. The branch is decided by the configurable
/// bulk-order threshold held in site settings.
/// </summary>
public enum OrderType { Checkout, QuoteRequest }

public enum OrderStatus { Pending, AwaitingPayment, Paid, Processing, Shipped, Delivered, Cancelled, Refunded }

public enum PaymentStatus { Initiated, Confirmed, Failed, Refunded }

/// <summary>A published review edited by its author re-enters <see cref="Pending"/>.</summary>
public enum ReviewStatus { Pending, Approved, Rejected }

public enum PostStatus { Draft, Published, Archived }

public enum BookingStatus { Requested, Confirmed, Completed, Cancelled }

public enum ContactStatus { New, InProgress, Resolved, Closed }

/// <summary>Recorded on every mutation, and on every refused authorisation.</summary>
public enum AuditAction { Create, Update, Delete, Publish, Login, AccessDenied }
