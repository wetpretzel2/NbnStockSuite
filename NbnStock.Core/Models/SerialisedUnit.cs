using System;


namespace NbnStock.Core.Models
{
    public class SerialisedUnit
    {
        public int Id { get; set; }
        public int StockItemId { get; set; }
        public string SerialNumber { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}
