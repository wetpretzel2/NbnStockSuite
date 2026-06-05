using NbnStock.Core.Models;
using NbnStock.Core.Repositories;

namespace NbnStock.Windows;

public partial class BulkConsumeWindow : Window
{
    private readonly SerialisedUnitRepository _serialisedRepo;
    private readonly StockRepository _stockRepo;

    public BulkConsumeWindow()
    {
        InitializeComponent();
        _stockRepo = new StockRepository();
        _serialisedRepo = new SerialisedUnitRepository();
        ConsumeBatch = new ObservableCollection<PendingStockEntry>();

        BatchDataGrid.ItemsSource = ConsumeBatch;
        LoadDropdownItems();
    }

    public ObservableCollection<PendingStockEntry> ConsumeBatch { get; set; }

    private void LoadDropdownItems()
    {
        var rawItems = _stockRepo.GetAllStockItems();

        var sortedItems = rawItems
            .OrderByDescending(i => i.IsSerialised)
            .ThenBy(i => i.SupplyType.ToString() == "TechSupplied" ? 1 : 0)
            .ThenBy(i => i.Category)
            .ThenBy(i => i.Name)
            .ToList();

        ComboItems.ItemsSource = sortedItems;
    }

    private void ComboItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = ComboItems.SelectedItem as StockItem;
        if (selectedItem == null) return;

        TxtAvailable.Text = $"({selectedItem.Quantity} Available)";

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

            // NEW: Strip the leading 'S'
            if (serial.StartsWith("S", System.StringComparison.OrdinalIgnoreCase)) serial = serial.Substring(1);

            if (selectedItem != null && !string.IsNullOrEmpty(serial))
            {
                // 1. Prevent duplicate scans in the queue
                if (ConsumeBatch.Any(p => p.SerialNumber.Equals(serial, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("This serial is already queued for consumption!", "Duplicate Scan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    InputScanner.Clear();
                    return;
                }

                // 2. Validate against database
                var unit = _serialisedRepo.GetSerialisedUnitBySerial(serial);
                if (unit == null)
                {
                    MessageBox.Show("This serial number was not found in the database.", "Not Found",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (unit.StockItemId != selectedItem.Id)
                {
                    MessageBox.Show($"This serial belongs to a different item type. You selected: {selectedItem.Name}.",
                        "Item Mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (unit.Status != UnitStatus.OnHand)
                {
                    MessageBox.Show($"Cannot consume this unit. Its current status is: {unit.Status}", "Invalid Status",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    ConsumeBatch.Add(new PendingStockEntry
                    {
                        StockItemId = selectedItem.Id,
                        ItemCode = selectedItem.ItemCode,
                        Name = selectedItem.Name,
                        IsSerialised = true,
                        Quantity = 1,
                        SerialNumber = unit.SerialNumber
                    });
                    UpdateTotalCount();
                }
            }

            InputScanner.Clear();
            InputScanner.Focus();
        }
    }

    private void InputQuantity_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return || e.Key == Key.Enter) BtnAddConsumable_Click(sender, e);
    }

    private void BtnAddConsumable_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = ComboItems.SelectedItem as StockItem;
        if (selectedItem != null && int.TryParse(InputQuantity.Text, out int qty) && qty > 0)
        {
            // Calculate how many of this item are already in the queue
            int currentlyQueued = ConsumeBatch
                .Where(b => b.StockItemId == selectedItem.Id)
                .Sum(b => b.Quantity);

            if (currentlyQueued + qty > selectedItem.Quantity)
            {
                MessageBox.Show(
                    $"You do not have enough stock. Available: {selectedItem.Quantity}. Already queued: {currentlyQueued}.",
                    "Insufficient Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConsumeBatch.Add(new PendingStockEntry
            {
                StockItemId = selectedItem.Id,
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
        TxtTotalCount.Text = $"Total Entries: {ConsumeBatch.Count}";
        if (ConsumeBatch.Count > 0) BatchDataGrid.ScrollIntoView(ConsumeBatch.Last());
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        ConsumeBatch.Clear();
        UpdateTotalCount();
    }

    private void BtnCommit_Click(object sender, RoutedEventArgs e)
    {
        if (ConsumeBatch.Count == 0) return;

        try
        {
            foreach (var entry in ConsumeBatch)
                if (entry.IsSerialised)
                {
                    _serialisedRepo.MarkUnitInstalled(entry.SerialNumber);
                    _stockRepo.ConsumeStock(entry.StockItemId, entry.Quantity);
                }
                else
                {
                    _stockRepo.ConsumeStock(entry.StockItemId, entry.Quantity);
                }

            MessageBox.Show($"Successfully recorded consumption for {ConsumeBatch.Count} entries.",
                "Consumption Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database Error during commit: {ex.Message}", "Commit Failed", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}