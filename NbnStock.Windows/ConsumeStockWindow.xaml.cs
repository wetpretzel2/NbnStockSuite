using NbnStock.Core.Models;
using NbnStock.Core.Repositories;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace NbnStock.Windows
{
    public partial class ConsumeStockWindow : Window
    {
        private readonly StockItem _stockItem;
        private readonly StockRepository _stockRepo;
        private readonly SerialisedUnitRepository _serialisedRepo;

        public ConsumeStockWindow(StockItem item)
        {
            InitializeComponent();
            _stockItem = item;
            _stockRepo = new StockRepository();
            _serialisedRepo = new SerialisedUnitRepository();

            SetupUI();
        }

        private void SetupUI()
        {
            TxtItemName.Text = $"Consume: {_stockItem.Name} ({_stockItem.ItemCode})";
            TxtCurrentStock.Text = $"Currently On Hand: {_stockItem.Quantity}";

            if (_stockItem.IsSerialised)
            {
                PanelConsumable.Visibility = Visibility.Collapsed;
                PanelSerialised.Visibility = Visibility.Visible;
                LoadAvailableSerials();
            }
            else
            {
                PanelSerialised.Visibility = Visibility.Collapsed;
                PanelConsumable.Visibility = Visibility.Visible;
                InputQuantity.Focus();
            }
        }

        private void LoadAvailableSerials()
        {
            // Get all OnHand units, then filter for this specific stock item type
            var availableUnits = _serialisedRepo.GetSerialisedUnitsByStatus(UnitStatus.OnHand)
                                                .Where(u => u.StockItemId == _stockItem.Id)
                                                .ToList();

            ListAvailableSerials.ItemsSource = availableUnits;

            if (availableUnits.Count == 0)
            {
                BtnConfirm.IsEnabled = false;
                MessageBox.Show("There are no available serial numbers on hand for this item.", "No Stock", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void InputQuantity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                BtnConfirm_Click(sender, e);
            }
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
                    var selectedUnits = ListAvailableSerials.SelectedItems.Cast<SerialisedUnit>().ToList();

                    if (selectedUnits.Count == 0)
                    {
                        MessageBox.Show("Please select at least one serial number to consume.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Update each selected serialised unit to 'Installed' status
                    foreach (var unit in selectedUnits)
                    {
                        _serialisedRepo.MarkUnitInstalled(unit.SerialNumber);
                    }

                    // Also deduct from the master quantity pool for quick reference
                    _stockRepo.ConsumeStock(_stockItem.Id, selectedUnits.Count);
                }
                else
                {
                    if (!int.TryParse(InputQuantity.Text, out int qty) || qty <= 0)
                    {
                        MessageBox.Show("Please enter a valid quantity greater than zero.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (qty > _stockItem.Quantity)
                    {
                        MessageBox.Show($"You cannot consume more stock than you have on hand ({_stockItem.Quantity}).", "Insufficient Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _stockRepo.ConsumeStock(_stockItem.Id, qty);
                }

                MessageBox.Show("Stock successfully consumed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error consuming stock: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}