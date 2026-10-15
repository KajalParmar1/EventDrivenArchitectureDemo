namespace InventoryService;

public sealed class InventoryOptions
{
    public const string SectionName = "Inventory";

    public int FailWhenTotalQuantityExceeds { get; set; } = 1000;
}
