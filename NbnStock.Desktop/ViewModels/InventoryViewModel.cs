using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NbnStock.Core.Data;
using NbnStock.Core.Models;
using NbnStock.Core.Repositories;

namespace NbnStock.Desktop.ViewModels;

public class InventoryViewModel : INotifyPropertyChanged
{
    private string _statusMessage = "Ready";
    private StockItem? _selectedStockItem;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<StockItem> StockItems { get; } = [];

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
            var repository = new StockRepository();
            var stockItems = repository.GetAllStockItems();

            StockItems.Clear();

            foreach (var stockItem in stockItems)
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

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}