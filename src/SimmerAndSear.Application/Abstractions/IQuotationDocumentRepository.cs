using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
//References
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 21 September 2026].

//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 21 September 2026].

public interface IQuotationDocumentRepository
{
    Task<QuotationDocument?> GetByOrderAsync(string orderId, CancellationToken ct = default);

    Task<bool> TryCreateAsync(QuotationDocument document, CancellationToken ct = default);
}
