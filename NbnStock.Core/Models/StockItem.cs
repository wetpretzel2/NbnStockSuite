namespace NbnStock.Core.Models;

public class StockItem
{
    public int Id { get; set; }
    public string ItemCode { get; set; }
    public SupplyType SupplyType { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; }
    public int MinimumStock { get; set; }
    public bool IsSerialised { get; set; }
    public string Notes { get; set; }
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}