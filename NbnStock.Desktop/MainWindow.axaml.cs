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
            DatabaseInitialiser.Initialise();

            var repository = new StockRepository();
            var stockItems = repository.GetAllStockItems();

            StockItemsList.ItemsSource = stockItems;
            StatusText.Text = $"Loaded {stockItems.Count} stock items from {DatabaseInitialiser.DatabasePath}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to load stock items: {ex.Message}";
        }
    }
}
