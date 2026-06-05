using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using NbnStock.Core.Models;
using NbnStock.Core.Repositories;

namespace NbnStock.Windows;

public partial class EWasteRecoveryWindow : Window
{
    private readonly SerialisedUnitRepository _serialisedRepo;
    private readonly StockRepository _stockRepo;

    public EWasteRecoveryWindow()
    {
        InitializeComponent();
        _stockRepo = new StockRepository();
        _serialisedRepo = new SerialisedUnitRepository();
        RecoveryBatch = new ObservableCollection<SerialisedUnit>();

        BatchDataGrid.ItemsSource = RecoveryBatch;
        LoadDropdownItems();

        InputScanner.Focus();
    }

    public ObservableCollection<SerialisedUnit> RecoveryBatch { get; set; }

    private void LoadDropdownItems()
    {
        // Only load serialised items (like ODU/IDU) for the legacy fallback dropdown
        var serialisedItems = _stockRepo.GetAllStockItems()
            .Where(i => i.IsSerialised)
            .OrderBy(i => i.Name)
            .ToList();
        ComboLegacyType.ItemsSource = serialisedItems;
    }

    private void InputScanner_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return || e.Key == Key.Enter)
        {
            // V1 Scanner Fix: Automatically strips the manufacturer 'S' prefix
            var serial = InputScanner.Text.TrimStart('s', 'S').Trim();

            if (string.IsNullOrEmpty(serial)) return;

            // Prevent scanning the same unit twice in one session
            if (RecoveryBatch.Any(u => u.SerialNumber.Equals(serial, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This serial is already in your current recovery batch.", "Duplicate Scan",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                InputScanner.Clear();
                return;
            }

            var existingUnit = _serialisedRepo.GetSerialisedUnitBySerial(serial);

            if (existingUnit != null)
            {
                // Validation: Make sure it's not already logged as e-waste
                if (existingUnit.Status == UnitStatus.EwastePendingSubmission ||
                    existingUnit.Status == UnitStatus.EwasteAwaitingApproval ||
                    existingUnit.Status == UnitStatus.ApprovedForDisposal ||
                    existingUnit.Status == UnitStatus.Disposed)
                {
                    MessageBox.Show(
                        $"This unit is already logged in the E-Waste system. Current Status: {existingUnit.Status}",
                        "Already Logged", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    // Found it! Temporarily repurpose the Notes field for the UI grid
                    existingUnit.Notes = "Known Unit";
                    RecoveryBatch.Add(existingUnit);
                }
            }
            else
            {
                // It's a legacy unit not in the DB! We need the dropdown to know what it is.
                var selectedType = ComboLegacyType.SelectedItem as StockItem;
                if (selectedType == null)
                {
                    MessageBox.Show(
                        "This serial was not found in your database. Please select what type of hardware it is from the dropdown below before scanning.",
                        "Legacy Unit Detected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create a temporary object to represent the new legacy unit (Id 0 flags it as new)
                var legacyUnit = new SerialisedUnit
                {
                    Id = 0,
                    StockItemId = selectedType.Id,
                    SerialNumber = serial,
                    Notes = "Legacy Unit"
                };
                RecoveryBatch.Add(legacyUnit);
            }

            TxtTotalCount.Text = $"Total Units: {RecoveryBatch.Count}";
            InputScanner.Clear();
            InputScanner.Focus();

            if (RecoveryBatch.Count > 0) BatchDataGrid.ScrollIntoView(RecoveryBatch.Last());
        }
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        RecoveryBatch.Clear();
        TxtTotalCount.Text = "Total Units: 0";
        InputScanner.Focus();
    }

    private void BtnCommit_Click(object sender, RoutedEventArgs e)
    {
        if (RecoveryBatch.Count == 0) return;

        try
        {
            foreach (var unit in RecoveryBatch)
                if (unit.Id == 0)
                    // Legacy unit: Add it fresh to the DB as E-Waste
                    _serialisedRepo.AddUnitToEwaste(unit.StockItemId, unit.SerialNumber);
                else
                    // Known unit: Update its status to E-Waste Pending
                    _serialisedRepo.UpdateSerialisedUnitStatus(unit.Id, UnitStatus.EwastePendingSubmission);

            MessageBox.Show($"Successfully queued {RecoveryBatch.Count} units into the E-Waste pipeline.",
                "Recovery Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error committing to E-Waste: {ex.Message}", "Database Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}