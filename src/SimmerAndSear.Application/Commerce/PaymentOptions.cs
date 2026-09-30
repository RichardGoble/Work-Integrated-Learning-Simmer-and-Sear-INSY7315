namespace SimmerAndSear.Application.Commerce;

public sealed class PaymentOptions
{
    public const string SectionName = "Payments";

    public const string SimulatedProvider = "SIMULATED";
    public bool AllowSimulator { get; set; }
}
