using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NbnStock.Core.Models;
using NbnStock.Core.Repositories;

namespace NbnStock.Desktop.ViewModels;

public class ReceiveViewModel : INotifyPropertyChanged
{
    private StockItem? _selectedStockItem;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<StockItem> StockItems { get; } = [];

    public ObservableCollection<PendingStockEntry> PendingBatch { get; } = [];
    public RelayCommand AddConsumableCommand { get; }
    public RelayCommand AddSerialCommand { get; }

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
        }
    }
    private string _quantityInput = "";
    private string _serialInput = "";
    private string _statusMessage = "";
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

    public bool IsSerialisedSelected =>
        SelectedStockItem?.IsSerialised == true;

    public bool IsConsumableSelected =>
        SelectedStockItem is { IsSerialised: false };

    public ReceiveViewModel()
    {
        LoadStockItems();
        AddConsumableCommand = new RelayCommand(AddConsumable);
        AddSerialCommand = new RelayCommand(AddSerial);
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
        {
            StockItems.Add(item);
        }
    }
    private void AddConsumable()
    {
        if (SelectedStockItem == null)
            return;

        if (!int.TryParse(QuantityInput, out var quantity) || quantity <= 0)
        {
            StatusMessage = "Enter a quantity greater than zero.";
            return;
        }

        PendingBatch.Add(new PendingStockEntry
        {
            StockItemId = SelectedStockItem.Id,
            ItemCode = SelectedStockItem.ItemCode,
            Name = SelectedStockItem.Name,
            IsSerialised = false,
            Quantity = quantity,
            SerialNumber = "N/A"
        });

        StatusMessage = $"Added {quantity} × {SelectedStockItem.Name}";
        QuantityInput = "";
    }

    private void AddSerial()
    {
        if (SelectedStockItem == null)
            return;

        var serial = SerialInput.Trim();

        if (serial.StartsWith("S", System.StringComparison.OrdinalIgnoreCase))
            serial = serial[1..];

        if (string.IsNullOrWhiteSpace(serial))
        {
            StatusMessage = "Enter a serial number.";
            return;
        }

        if (PendingBatch.Any(entry =>
                entry.IsSerialised &&
                string.Equals(
                    entry.SerialNumber,
                    serial,
                    System.StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = $"Serial {serial} is already in the current batch.";
            SerialInput = "";
            return;
        }

        PendingBatch.Add(new PendingStockEntry
        {
            StockItemId = SelectedStockItem.Id,
            ItemCode = SelectedStockItem.ItemCode,
            Name = SelectedStockItem.Name,
            IsSerialised = true,
            Quantity = 1,
            SerialNumber = serial
        });
        StatusMessage = $"Added {SelectedStockItem.Name} — {serial}";
        SerialInput = "";
        
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
}