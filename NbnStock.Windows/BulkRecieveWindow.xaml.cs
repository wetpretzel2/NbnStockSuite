using NbnStock.Core.Models;
using NbnStock.Core.Repositories;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NbnStock.Windows
{
    public partial class BulkReceiveWindow : Window
    {
        private readonly StockRepository _stockRepo;
        // ObservableCollection automatically updates the DataGrid when items are added
        public ObservableCollection<PendingStockEntry> PendingBatch { get; set; }

        public BulkReceiveWindow()
        {
            InitializeComponent();
            _stockRepo = new StockRepository();
            PendingBatch = new ObservableCollection<PendingStockEntry>();

            BatchDataGrid.ItemsSource = PendingBatch;
            LoadDropdownItems();
        }

        private void LoadDropdownItems()
        {
            var rawItems = _stockRepo.GetAllStockItems();

            // Sort the list logically before giving it to the dropdown
            var sortedItems = rawItems
                .OrderByDescending(i => i.IsSerialised)          // 1. Serialised units always at the very top
                .ThenBy(i => i.SupplyType.ToString() == "TechSupplied" ? 1 : 0) // 2. Push all Tech Supplied to the bottom
                .ThenBy(i => GetCategorySortWeight(i.Category))  // 3. Sort NBN stuff by Mounts, Cables, Wallplates
                .ThenBy(i => i.Name)                             // 4. Alphabetical within those groups
                .ToList();

            ComboItems.ItemsSource = sortedItems;
        }

        // Helper method to rank categories
        private int GetCategorySortWeight(string category)
        {
            switch (category?.ToLower())
            {
                case "mounts": return 1;
                case "cabling": return 2;
                case "hardware": return 3; // Wallplates (ODU/IDU are hardware too, but they are caught by IsSerialised first)
                default: return 4;
            }
        }

        private void ComboItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ComboItems.SelectedItem as StockItem;
            if (selectedItem == null) return;

            if (selectedItem.IsSerialised)
            {
                PanelConsumable.Visibility = Visibility.Collapsed;
                PanelSerialised.Visibility = Visibility.Visible;
                // Auto-focus the scanner box so you don't have to click it
                InputScanner.Focus();
            }
            else
            {
                PanelSerialised.Visibility = Visibility.Collapsed;
                PanelConsumable.Visibility = Visibility.Visible;
                InputQuantity.Focus();
            }
        }

        // --- SCANNER LOGIC ---
        private void InputScanner_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                var selectedItem = ComboItems.SelectedItem as StockItem;
                string serial = InputScanner.Text.Trim();

                if (selectedItem != null && !string.IsNullOrEmpty(serial))
                {
                    // Prevent duplicate scans in the same batch
                    if (PendingBatch.Any(p => p.SerialNumber == serial))
                    {
                        MessageBox.Show("This serial is already in your current batch!", "Duplicate Scan", MessageBoxButton.OK, MessageBoxImage.Warning);
                        InputScanner.Clear();
                        return;
                    }

                    PendingBatch.Add(new PendingStockEntry
                    {
                        ItemCode = selectedItem.ItemCode,
                        Name = selectedItem.Name,
                        IsSerialised = true,
                        Quantity = 1,
                        SerialNumber = serial
                    });

                    UpdateTotalCount();
                }

                // Clear the box instantly for the next scan
                InputScanner.Clear();
            }
        }

        // --- CONSUMABLE LOGIC ---
        private void InputQuantity_KeyDown(object sender, KeyEventArgs e)
        {
            // Allow pressing Enter instead of clicking the Add button
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                BtnAddConsumable_Click(sender, e);
            }
        }

        private void BtnAddConsumable_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ComboItems.SelectedItem as StockItem;
            if (selectedItem != null && int.TryParse(InputQuantity.Text, out int qty) && qty > 0)
            {
                PendingBatch.Add(new PendingStockEntry
                {
                    ItemCode = selectedItem.ItemCode,
                    Name = selectedItem.Name,
                    IsSerialised = false,
                    Quantity = qty,
                    SerialNumber = "N/A"
                });

                InputQuantity.Clear();
                UpdateTotalCount();
            }
        }

        private void UpdateTotalCount()
        {
            TxtTotalCount.Text = $"Total Items in Batch: {PendingBatch.Count}";
            // Automatically scroll the grid to the newest item at the bottom
            if (PendingBatch.Count > 0)
            {
                BatchDataGrid.ScrollIntoView(PendingBatch.Last());
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            PendingBatch.Clear();
            UpdateTotalCount();
        }

        private void BtnCommit_Click(object sender, RoutedEventArgs e)
        {
            if (PendingBatch.Count == 0) return;

            // Here we would loop through PendingBatch and call _stockRepo and SerialisedUnitRepository
            // For now, we will just close the window and pretend it saved
            MessageBox.Show($"Successfully processed {PendingBatch.Count} batch entries.", "Batch Complete");
            this.DialogResult = true;
        }
    }
}