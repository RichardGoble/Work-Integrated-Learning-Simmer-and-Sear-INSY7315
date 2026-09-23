using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Engagement;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Application.Engagement;

public sealed class BookingService(
    IBookingRepository bookingRepo,
    IOrderRepository orderRepo)
{
    public async Task<(BookingResponse? Response, string? Error)> CreateAsync(
        CreateBookingRequest request,
        string? currentUserId,
        string? currentUserName,
        string? currentUserEmail,
        CancellationToken ct = default)
    {
        if (request.PreferredDateTime <= DateTime.UtcNow)
        {
            return (null, "Preferred consultation date and time must be in the future.");
        }

        if (string.IsNullOrWhiteSpace(request.Purpose))
        {
            return (null, "Consultation purpose is required.");
        }

        string clientName;
        string clientEmail;
        string? clientPhone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        string? clientId;
        string? orderId = null;

        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            // Authenticated customer: trust CurrentUser context
            clientId = currentUserId;
            clientName = !string.IsNullOrWhiteSpace(currentUserName) ? currentUserName : (request.FullName?.Trim() ?? "Valued Customer");
            clientEmail = !string.IsNullOrWhiteSpace(currentUserEmail) ? currentUserEmail : (request.Email?.Trim() ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(request.OrderId))
            {
                var order = await orderRepo.GetForClientAsync(request.OrderId.Trim(), currentUserId, ct);
                if (order is null)
                {
                    return (null, "Linked order was not found or does not belong to your account.");
                }
                orderId = order.Id;
            }
        }
        else
        {
            // Anonymous visitor: cannot attach an arbitrary order
            if (!string.IsNullOrWhiteSpace(request.OrderId))
            {
                return (null, "Anonymous bookings cannot be linked to orders. Please sign in to link your quote.");
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return (null, "Name is required for consultation booking.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return (null, "Email is required for consultation booking.");
            }

            clientId = null;
            clientName = request.FullName.Trim();
            clientEmail = request.Email.Trim().ToLowerInvariant();
        }

        var booking = new Booking
        {
            ClientId = clientId,
            ClientName = clientName,
            ClientEmail = clientEmail,
            ClientPhone = clientPhone,
            PreferredDateTime = request.PreferredDateTime.ToUniversalTime(),
            Purpose = request.Purpose.Trim(),
            OrderId = orderId,
            Status = BookingStatus.Requested,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await bookingRepo.CreateAsync(booking, ct);

        return (Map(booking), null);
    }

    public async Task<List<BookingResponse>> ListAsync(CancellationToken ct = default)
    {
        var items = await bookingRepo.ListAsync(ct);
        return items.Select(Map).ToList();
    }

    public async Task<BookingResponse?> GetAsync(string id, CancellationToken ct = default)
    {
        var booking = await bookingRepo.GetAsync(id, ct);
        return booking is null ? null : Map(booking);
    }

    public async Task<BookingResponse?> UpdateStatusAsync(string id, BookingStatus status, CancellationToken ct = default)
    {
        var booking = await bookingRepo.GetAsync(id, ct);
        if (booking is null) return null;

        await bookingRepo.UpdateStatusAsync(id, status, ct);
        booking.Status = status;
        booking.UpdatedAt = DateTime.UtcNow;

        return Map(booking);
    }

    private static BookingResponse Map(Booking b) => new(
        b.Id,
        b.ClientId,
        b.ClientName,
        b.ClientEmail,
        b.ClientPhone,
        b.PreferredDateTime,
        b.Purpose,
        b.OrderId,
        b.Status.ToString(),
        b.CreatedAt,
        b.UpdatedAt
    );
}
