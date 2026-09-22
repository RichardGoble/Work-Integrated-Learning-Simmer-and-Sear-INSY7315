using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
//References
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 21 September 2026].

public interface IQuotationPdfRenderer
{
    byte[] Render(Order order, string customerEmail, DateTime validUntil);
}
