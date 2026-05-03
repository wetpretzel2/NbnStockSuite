using NbnStock.Core.Models;
using NbnStock.Core.Repositories;
using System.Windows;

namespace NbnStock.Windows
{
    public partial class EWasteDashboardWindow : Window
    {
        private readonly SerialisedUnitRepository _serialisedRepo;

        public EWasteDashboardWindow()
        {
            InitializeComponent();
            _serialisedRepo = new SerialisedUnitRepository();
            LoadEwastePipeline();
        }

        private void LoadEwastePipeline()
        {
            // Fetch all units, then filter for active E-Waste stages
            var allUnits = _serialisedRepo.GetAllSerialisedUnits();

            var activeEwaste = allUnits.Where(u =>
                u.Status == UnitStatus.EwastePendingSubmission ||
                u.Status == UnitStatus.EwasteAwaitingApproval ||
                u.Status == UnitStatus.ApprovedForDisposal)
                .OrderBy(u => u.Status)
                .ThenByDescending(u => u.LastUpdatedUtc)
                .ToList();

            EwasteDataGrid.ItemsSource = activeEwaste;

            if (activeEwaste.Count == 0)
            {
                BtnProgressStage.IsEnabled = false;
            }
            else
            {
                BtnProgressStage.IsEnabled = true;
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadEwastePipeline();
        }
        private void BtnLogNew_Click(object sender, RoutedEventArgs e)
        {
            // Open the recovery scanner directly from the dashboard
            var scannerWindow = new EWasteRecoveryWindow
            {
                Owner = this
            };

            // If the scanner returns true (meaning they committed new items), instantly refresh the dashboard
            if (scannerWindow.ShowDialog() == true)
            {
                LoadEwastePipeline();
            }
        }

        private void BtnProgressStage_Click(object sender, RoutedEventArgs e)
        {
            var selectedUnits = EwasteDataGrid.SelectedItems.Cast<SerialisedUnit>().ToList();

            if (selectedUnits.Count == 0)
            {
                MessageBox.Show("Please select at least one unit from the list to progress.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                foreach (var unit in selectedUnits)
                {
                    // This relies on the robust state machine you already built in the repository!
                    _serialisedRepo.MoveToNextEwasteStage(unit.SerialNumber);
                }

                MessageBox.Show($"Successfully progressed {selectedUnits.Count} units to their next stage.", "Pipeline Updated", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reload the grid to show the new statuses (or their removal if they hit Disposed)
                LoadEwastePipeline();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update E-Waste pipeline: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}