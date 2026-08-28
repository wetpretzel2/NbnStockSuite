using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NbnStock.Core.Data;
using NbnStock.Core.Models;
using NbnStock.Core.Services;


namespace NbnStock.Desktop.ViewModels;

public class InventoryViewModel : INotifyPropertyChanged
{
    private string _statusMessage = "Ready";
    private StockItem? _selectedStockItem;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<StockItem> StockItems { get; } = [];
    public InventoryViewModel()
    {
        LoadStockItems();
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value)
                return;

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public StockItem? SelectedStockItem
    {
        get => _selectedStockItem;
        set
        {
            if (_selectedStockItem == value)
                return;

            _selectedStockItem = value;
            OnPropertyChanged();
        }
    }

    public void LoadStockItems()
    {
        try
        {
            var inventoryService = new InventoryService();
            var stockItems = inventoryService.GetCurrentInventory();
            var sortedItems = stockItems
                .OrderByDescending(item => item.IsSerialised)
                .ThenBy(item => item.SupplyType == SupplyType.TechSupplied ? 1 : 0)
                .ThenBy(item => GetCategorySortWeight(item.Category))
                .ThenBy(item => item.Name)
                .ToList();

            StockItems.Clear();

            foreach (var stockItem in sortedItems)
            {
                StockItems.Add(stockItem);
            }

            StatusMessage =
                $"Loaded {stockItems.Count} stock items from {DatabaseInitialiser.DatabasePath}";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Failed to load stock items: {ex.Message}";
        }
    }
    private static int GetCategorySortWeight(string? category)
    {
        return category?.ToLowerInvariant() switch
        {
            "mounts" => 1,
            "cabling" => 2,
            "hardware" => 3,
            _ => 4
        };
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}