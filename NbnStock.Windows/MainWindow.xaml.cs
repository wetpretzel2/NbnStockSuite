using NbnStock.Core.Repositories;
using NbnStock.Core.Models;
using System.Linq;
using System.Windows;

namespace NbnStock.Windows
{
    public partial class MainWindow : Window
    {
        private readonly StockRepository _stockRepo;

        public MainWindow()
        {
            InitializeComponent();
            _stockRepo = new StockRepository();
            LoadStockItems();
        }

        private void LoadStockItems()
        {
            var rawItems = _stockRepo.GetAllStockItems();

            // Sort the list logically before binding it to the main DataGrid
            var sortedItems = rawItems
                .OrderByDescending(i => i.IsSerialised)          // 1. Serialised units always at the top
                .ThenBy(i => i.SupplyType.ToString() == "TechSupplied" ? 1 : 0) // 2. Tech Supplied pushed to the bottom
                .ThenBy(i => GetCategorySortWeight(i.Category))  // 3. Sort NBN stuff by Mounts, Cables, Wallplates
                .ThenBy(i => i.Name)                             // 4. Alphabetical within those groups
                .ToList();

            StockItemsDataGrid.ItemsSource = sortedItems;
        }

        // Helper method to rank categories
        private int GetCategorySortWeight(string category)
        {
            switch (category?.ToLower())
            {
                case "mounts": return 1;
                case "cabling": return 2;
                case "hardware": return 3;
                default: return 4;
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadStockItems();
        }

        private void BtnReceiveStock_Click(object sender, RoutedEventArgs e)
        {
            var bulkWindow = new BulkReceiveWindow
            {
                Owner = this
            };

            if (bulkWindow.ShowDialog() == true)
            {
                // Refresh the main grid once the batch is committed
                LoadStockItems();
            }
        }

        private void BtnConsumeStock_Click(object sender, RoutedEventArgs e)
        {
            var bulkConsumeWindow = new BulkConsumeWindow
            {
                Owner = this
            };

            if (bulkConsumeWindow.ShowDialog() == true)
            {
                // Refresh the main grid once the daily batch is committed
                LoadStockItems();
            }
        }
        private void BtnAddCustomItem_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddCustomItemWindow
            {
                Owner = this
            };

            if (addWindow.ShowDialog() == true)
            {
                // Refresh the main grid so the newly created item appears
                LoadStockItems();
            }
        }
        private void BtnEWasteDashboard_Click(object sender, RoutedEventArgs e)
        {
            var ewasteDashboard = new EWasteDashboardWindow
            {
                Owner = this
            };

            ewasteDashboard.ShowDialog();
        }
    }
}