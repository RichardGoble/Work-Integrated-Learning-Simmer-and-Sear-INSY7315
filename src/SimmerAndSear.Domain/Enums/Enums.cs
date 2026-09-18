namespace SimmerAndSear.Domain.Enums;
public enum UserType { Client, Admin }
public enum RoleName { Customer, SalesAdmin, MasterAdmin }
public enum Permission { CatalogueManage, InquiryManage, AccountManage, AuditRead, SettingsManage }
public enum OrderType { Checkout, QuoteRequest }

public enum OrderStatus { Pending, AwaitingPayment, Paid, Processing, Shipped, Delivered, Cancelled, Refunded }

public enum PaymentStatus { Initiated, Confirmed, Failed, Refunded }
public enum ReviewStatus { Pending, Approved, Rejected }

public enum PostStatus { Draft, Published, Archived }

public enum BookingStatus { Requested, Confirmed, Completed, Cancelled }

public enum ContactStatus { New, InProgress, Resolved, Closed }
public enum AuditAction { Create, Update, Delete, Publish, Login, AccessDenied, Logout }
