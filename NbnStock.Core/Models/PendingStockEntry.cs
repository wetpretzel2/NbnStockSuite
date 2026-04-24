using System;
using System.Collections.Generic;
using System.Text;

namespace NbnStock.Core.Models
{
        public class PendingStockEntry
        {
            public string ItemCode { get; set; }
            public string Name { get; set; }
            public bool IsSerialised { get; set; }
            public int Quantity { get; set; }
            public string SerialNumber { get; set; } // Will be blank for consumables
        }
    
}
