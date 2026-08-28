using NbnStock.Core.Models;
using NbnStock.Core.Repositories;

namespace NbnStock.Core.Services;

public class InventoryService
{
    private readonly StockRepository _stockRepository = new();
    private readonly SerialisedUnitRepository _serialisedUnitRepository = new();

    public List<StockItem> GetCurrentInventory()
    {
        var stockItems = _stockRepository.GetAllStockItems();

        var onHandUnits =
            _serialisedUnitRepository.GetSerialisedUnitsByStatus(UnitStatus.OnHand);

        foreach (var item in stockItems.Where(item => item.IsSerialised))
        {
            item.Quantity =
                onHandUnits.Count(unit => unit.StockItemId == item.Id);
        }

        return stockItems;
    }
}