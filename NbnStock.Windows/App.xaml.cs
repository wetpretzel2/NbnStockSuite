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

            var allItems = stockRepo.GetAllStockItems();

            if (allItems.Count > 0)
            {
                var firstItem = allItems[0];

                var itemById = stockRepo.GetStockItemById(firstItem.Id);
                var itemByCode = stockRepo.GetStockItemByCode(firstItem.ItemCode);

                stockRepo.UpdateStockQuantity(firstItem.Id, 99);
                var afterUpdate = stockRepo.GetStockItemById(firstItem.Id);

                stockRepo.AdjustStockQuantity(firstItem.Id, -4);
                var afterAdjust = stockRepo.GetStockItemById(firstItem.Id);

                stockRepo.ReceiveStock(firstItem.Id, 10);
                var afterReceive = stockRepo.GetStockItemById(firstItem.Id);

                stockRepo.ConsumeStock(firstItem.Id, 3);
                var afterConsume = stockRepo.GetStockItemById(firstItem.Id);
            }

            var nbnSuppliedItems = stockRepo.GetStockItemsBySupplyType(SupplyType.NbnSupplied);
        }
    }
}