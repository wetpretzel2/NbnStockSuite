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

            // Get everything first so we can grab a real item to test with
            var allItems = stockRepo.GetAllStockItems();

            if (allItems.Count > 0)
            {
                var firstItem = allItems[0];

                // 1. Test GetStockItemById
                var itemById = stockRepo.GetStockItemById(firstItem.Id);

                // 2. Test GetStockItemByCode
                var itemByCode = stockRepo.GetStockItemByCode(firstItem.ItemCode);

                // 3. Test UpdateStockQuantity
                stockRepo.UpdateStockQuantity(firstItem.Id, 99);
                var afterUpdate = stockRepo.GetStockItemById(firstItem.Id);

                // 4. Test AdjustStockQuantity
                stockRepo.AdjustStockQuantity(firstItem.Id, -4);
                var afterAdjust = stockRepo.GetStockItemById(firstItem.Id);
            }

            var nbnSuppliedItems = stockRepo.GetStockItemsBySupplyType(SupplyType.NbnSupplied);
        }
    }
}