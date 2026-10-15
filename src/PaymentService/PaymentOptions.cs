namespace PaymentService;

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    public decimal FailForOrdersAbove { get; set; } = 10000m;
}
