using NbnStock.Core.Models;
using NbnStock.Core.Repositories;
using System;
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
        private readonly SerialisedUnitRepository _serialisedRepo; // Added this!

        public ObservableCollection<PendingStockEntry> PendingBatch { get; set; }

        public BulkReceiveWindow()
        {
            InitializeComponent();
            _stockRepo = new StockRepository();
            _serialisedRepo = new SerialisedUnitRepository(); // Initialised this!
            PendingBatch = new ObservableCollection<PendingStockEntry>();

            BatchDataGrid.ItemsSource = PendingBatch;
            LoadDropdownItems();
        }

        private void LoadDropdownItems()
        {
            var rawItems = _stockRepo.GetAllStockItems();

            var sortedItems = rawItems
                .OrderByDescending(i => i.IsSerialised)
                .ThenBy(i => i.SupplyType.ToString() == "TechSupplied" ? 1 : 0)
                .ThenBy(i => GetCategorySortWeight(i.Category))
                .ThenBy(i => i.Name)
                .ToList();

            ComboItems.ItemsSource = sortedItems;
        }

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

        private void ComboItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ComboItems.SelectedItem as StockItem;
            if (selectedItem == null) return;

            if (selectedItem.IsSerialised)
            {
                PanelConsumable.Visibility = Visibility.Collapsed;
                PanelSerialised.Visibility = Visibility.Visible;
                InputScanner.Focus();
            }
            else
            {
                PanelSerialised.Visibility = Visibility.Collapsed;
                PanelConsumable.Visibility = Visibility.Visible;
                InputQuantity.Focus();
            }
        }

        private void InputScanner_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                var selectedItem = ComboItems.SelectedItem as StockItem;
                string serial = InputScanner.Text.Trim();

                if (selectedItem != null && !string.IsNullOrEmpty(serial))
                {
                    if (PendingBatch.Any(p => p.SerialNumber == serial))
                    {
                        MessageBox.Show("This serial is already in your current batch!", "Duplicate Scan", MessageBoxButton.OK, MessageBoxImage.Warning);
                        InputScanner.Clear();
                        return;
                    }

                    PendingBatch.Add(new PendingStockEntry
                    {
                        StockItemId = selectedItem.Id, // Saving the DB Id!
                        ItemCode = selectedItem.ItemCode,
                        Name = selectedItem.Name,
                        IsSerialised = true,
                        Quantity = 1,
                        SerialNumber = serial
                    });

                    UpdateTotalCount();
                }
                InputScanner.Clear();
            }
        }

        private void InputQuantity_KeyDown(object sender, KeyEventArgs e)
        {
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
                    StockItemId = selectedItem.Id, // Saving the DB Id!
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

        // THIS IS THE FIX! We are actually talking to SQLite now.
        private void BtnCommit_Click(object sender, RoutedEventArgs e)
        {
            if (PendingBatch.Count == 0) return;

            try
            {
                foreach (var entry in PendingBatch)
                {
                    if (entry.IsSerialised)
                    {
                        // 1. Add the specific serial number to the Serialised table
                        _serialisedRepo.ReceiveSerialisedUnit(entry.StockItemId, entry.SerialNumber);
                        // 2. Add +1 to the master quantity of that item type
                        _stockRepo.ReceiveStock(entry.StockItemId, entry.Quantity);
                    }
                    else
                    {
                        // Bulk item, just add the quantity to the master table
                        _stockRepo.ReceiveStock(entry.StockItemId, entry.Quantity);
                    }
                }

                MessageBox.Show($"Successfully committed {PendingBatch.Count} entries to inventory.", "Batch Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true; // Closes window and tells MainWindow to refresh
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database Error during commit: {ex.Message}\n\nCheck if you scanned a duplicate serial number that is already in the database.", "Commit Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}