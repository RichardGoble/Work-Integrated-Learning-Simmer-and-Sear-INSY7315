using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Commerce;


//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public interface IOrderFulfilmentStrategy
{
    
    string Name { get; }

   
    Order CreateOrder(Cart cart, string clientId, ShippingDetails shipping, SiteSettings settings);

   
    string NextStepMessage { get; }
}
