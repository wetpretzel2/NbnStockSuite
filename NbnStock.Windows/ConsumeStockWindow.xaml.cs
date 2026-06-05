using NbnStock.Core.Models;
using NbnStock.Core.Repositories;

namespace NbnStock.Windows;

public partial class ConsumeStockWindow : Window
{
    private readonly SerialisedUnitRepository _serialisedRepo;
    private readonly StockItem _stockItem;
    private readonly StockRepository _stockRepo;

    public ConsumeStockWindow(StockItem item)
    {
        InitializeComponent();
        _stockItem = item;
        _stockRepo = new StockRepository();
        _serialisedRepo = new SerialisedUnitRepository();
        ScannedQueue = new ObservableCollection<SerialisedUnit>();

        ListScannedSerials.ItemsSource = ScannedQueue;

        SetupUI();
    }

    // This holds the queue of items you have scanned
    public ObservableCollection<SerialisedUnit> ScannedQueue { get; set; }

    private void SetupUI()
    {
        TxtItemName.Text = $"Consume: {_stockItem.Name} ({_stockItem.ItemCode})";
        TxtCurrentStock.Text = $"Currently On Hand: {_stockItem.Quantity}";

        if (_stockItem.IsSerialised)
        {
            PanelConsumable.Visibility = Visibility.Collapsed;
            PanelSerialised.Visibility = Visibility.Visible;
            InputScanner.Focus(); // Auto-focus the scanner ready for the first beep
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
            string serial = InputScanner.Text.Trim();
            if (string.IsNullOrEmpty(serial)) return;

            // NEW: Strip the leading 'S'
            if (serial.StartsWith("S", System.StringComparison.OrdinalIgnoreCase)) serial = serial.Substring(1);

            // 1. Prevent double-scanning the same unit in this session
            if (ScannedQueue.Any(u => u.SerialNumber.Equals(serial, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("You have already scanned this serial number into the current queue.", "Duplicate Scan",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                InputScanner.Clear();
                return;
            }

            // 2. Look up the serial in the database
            var unit = _serialisedRepo.GetSerialisedUnitBySerial(serial);

            // 3. Validation checks
            if (unit == null)
            {
                MessageBox.Show("This serial number was not found in the database.", "Not Found", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else if (unit.StockItemId != _stockItem.Id)
            {
                MessageBox.Show(
                    $"This serial belongs to a different item type. You are currently consuming: {_stockItem.Name}.",
                    "Item Mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (unit.Status != UnitStatus.OnHand)
            {
                MessageBox.Show($"Cannot consume this unit. Its current status is: {unit.Status}", "Invalid Status",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                // 4. Validation passed! Add to our consumption queue
                ScannedQueue.Add(unit);
                TxtScanCount.Text = $"Scanned units ready to consume: {ScannedQueue.Count}";
            }

            // Clear the box instantly for the next rapid scan
            InputScanner.Clear();
            InputScanner.Focus();
        }
    }

    private void InputQuantity_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return || e.Key == Key.Enter) BtnConfirm_Click(sender, e);
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_stockItem.IsSerialised)
            {
                if (ScannedQueue.Count == 0)
                {
                    MessageBox.Show("Please scan at least one serial number to consume.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update each scanned unit to 'Installed' status
                foreach (var unit in ScannedQueue) _serialisedRepo.MarkUnitInstalled(unit.SerialNumber);

                // Deduct the total amount from the master quantity pool
                _stockRepo.ConsumeStock(_stockItem.Id, ScannedQueue.Count);
            }
            else
            {
                if (!int.TryParse(InputQuantity.Text, out int qty) || qty <= 0)
                {
                    MessageBox.Show("Please enter a valid quantity greater than zero.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (qty > _stockItem.Quantity)
                {
                    MessageBox.Show($"You cannot consume more stock than you have on hand ({_stockItem.Quantity}).",
                        "Insufficient Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _stockRepo.ConsumeStock(_stockItem.Id, qty);
            }

            MessageBox.Show("Stock successfully consumed.", "Success", MessageBoxButton.OK,
                MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error consuming stock: {ex.Message}", "Database Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}