using NbnStock.Core.Repositories;
using NbnStock.Core.Models; // Required for the StockItem cast
using System.Windows;

namespace NbnStock.Windows
{
    public partial class MainWindow : Window
    {
        // Initialize this once for the lifetime of the window
        private readonly StockRepository _stockRepo;

        public MainWindow()
        {
            InitializeComponent();
            _stockRepo = new StockRepository();
            LoadStockItems();
        }

        private void LoadStockItems()
        {
            var items = _stockRepo.GetAllStockItems();
            StockItemsDataGrid.ItemsSource = items;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadStockItems();
        }

        private void BtnReceiveStock_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = StockItemsDataGrid.SelectedItem as StockItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Please select an item from the list first.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Ready to receive stock for: {selectedItem.Name}\nCurrently On Hand: {selectedItem.Quantity}", "Receive Stock");
        }

        private void BtnConsumeStock_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = StockItemsDataGrid.SelectedItem as StockItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Please select an item from the list first.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Ready to consume stock for: {selectedItem.Name}\nCurrently On Hand: {selectedItem.Quantity}", "Consume Stock");
        }
    }
}