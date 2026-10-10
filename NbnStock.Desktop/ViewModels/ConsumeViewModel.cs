
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NbnStock.Core.Models;
using NbnStock.Core.Repositories;

namespace NbnStock.Desktop.ViewModels;

public class ConsumeViewModel : INotifyPropertyChanged
{
    private StockItem? _selectedStockItem;
    private string _quantityInput = "";
    private string _serialInput = "";
    private string _statusMessage = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<StockItem> StockItems { get; } = [];

    public StockItem? SelectedStockItem
    {
        get => _selectedStockItem;
        set
        {
            if (_selectedStockItem == value)
                return;

            _selectedStockItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSerialisedSelected));
            OnPropertyChanged(nameof(IsConsumableSelected));
            OnPropertyChanged(nameof(AvailableQuantity));
        }
    }

    public bool IsSerialisedSelected =>
        SelectedStockItem?.IsSerialised == true;

    public bool IsConsumableSelected =>
        SelectedStockItem is { IsSerialised: false };

    public int AvailableQuantity =>
        SelectedStockItem?.Quantity ?? 0;

    public string QuantityInput
    {
        get => _quantityInput;
        set
        {
            if (_quantityInput == value)
                return;

            _quantityInput = value;
            OnPropertyChanged();
        }
    }

    public string SerialInput
    {
        get => _serialInput;
        set
        {
            if (_serialInput == value)
                return;

            _serialInput = value;
            OnPropertyChanged();
        }
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

    public ConsumeViewModel()
    {
        LoadStockItems();
    }

    private void LoadStockItems()
    {
        var repository = new StockRepository();

        var sortedItems = repository
            .GetAllStockItems()
            .OrderByDescending(item => item.IsSerialised)
            .ThenBy(item => item.SupplyType == SupplyType.TechSupplied ? 1 : 0)
            .ThenBy(item => GetCategorySortWeight(item.Category))
            .ThenBy(item => item.Name)
            .ToList();

        StockItems.Clear();

        foreach (var item in sortedItems)
            StockItems.Add(item);
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
