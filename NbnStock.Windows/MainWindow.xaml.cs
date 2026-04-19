using NbnStock.Core.Repositories;
using System.Windows;

namespace NbnStock.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadStockItems();
        }

        private void LoadStockItems()
        {
            var stockRepo = new StockRepository();
            var items = stockRepo.GetAllStockItems();

            StockItemsDataGrid.ItemsSource = items;
        }
    }
}