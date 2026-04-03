using NbnStock.Core.Data;
using NbnStock.Core.Models;
using NbnStock.Core.Repositories;
using System.Windows;

namespace NbnStock.Windows
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DatabaseInitialiser.Initialise();

            var stockRepo = new StockRepository();

            //stockRepo.AddStockItem(new StockItem
            //{
            //    ItemCode = "ODU-NBN-001",
            //    Name = "Outdoor Unit",
            //    Category = "NBN Hardware",
            //    Quantity = 32,
            //    Unit = "Each",
            //    MinimumStock = 4,
            //    IsSerialised = true,
            //    SupplyType = SupplyType.NbnSupplied,
            //    Notes = "Initial test stock item",
            //    LastUpdatedUtc = DateTime.UtcNow
            //});

            var allItems = stockRepo.GetAllStockItems();
            var nbnSuppliedItems = stockRepo.GetStockItemsBySupplyType(SupplyType.NbnSupplied);
        }
    }
}