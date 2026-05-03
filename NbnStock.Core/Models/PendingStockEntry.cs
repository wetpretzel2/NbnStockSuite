namespace NbnStock.Core.Models
{
    public class PendingStockEntry
    {
        public int StockItemId { get; set; }
        public string ItemCode { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsSerialised { get; set; }
        public int Quantity { get; set; }
        public string SerialNumber { get; set; } = "";
    }
}