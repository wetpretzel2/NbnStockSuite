using System;
using Avalonia.Controls;
using NbnStock.Core.Data;
using NbnStock.Core.Repositories;
using NbnStock.Desktop.ViewModels;

namespace NbnStock.Desktop;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        LoadStockItems();
    }

    private void LoadStockItems()
    {
        try
        {
            

            var repository = new StockRepository();
            var stockItems = repository.GetAllStockItems();

            _viewModel.StockItems.Clear();

            foreach (var stockItem in stockItems)
            {
                _viewModel.StockItems.Add(stockItem);
            }
            _viewModel.StatusMessage =
                $"Loaded {stockItems.Count} stock items from {DatabaseInitialiser.DatabasePath}";
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage =
                $"Failed to load stock items: {ex.Message}";
        }
    }
}
