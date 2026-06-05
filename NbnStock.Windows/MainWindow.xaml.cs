using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using NbnStock.Core.Models;
using NbnStock.Core.Repositories;
using NbnStock.Core.Services;
using NbnStock.Windows.Services;

namespace NbnStock.Windows;

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

        var serialisedRepo = new SerialisedUnitRepository();
        var onHandUnits = serialisedRepo.GetSerialisedUnitsByStatus(UnitStatus.OnHand);

        foreach (var item in rawItems.Where(i => i.IsSerialised))
        {
            item.Quantity = onHandUnits.Count(u => u.StockItemId == item.Id);
        }

        var sortedItems = rawItems
            .OrderByDescending(i => i.IsSerialised)
            .ThenBy(i => i.SupplyType.ToString() == "TechSupplied" ? 1 : 0)
            .ThenBy(i => GetCategorySortWeight(i.Category))
            .ThenBy(i => i.Name)
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
            // Refresh the main grid once the batch is committed
            LoadStockItems();
    }

    private void BtnConsumeStock_Click(object sender, RoutedEventArgs e)
    {
        var bulkConsumeWindow = new BulkConsumeWindow
        {
            Owner = this
        };

        if (bulkConsumeWindow.ShowDialog() == true)
            // Refresh the main grid once the daily batch is committed
            LoadStockItems();
    }

    private void BtnAddCustomItem_Click(object sender, RoutedEventArgs e)
    {
        var addWindow = new AddCustomItemWindow
        {
            Owner = this
        };

        if (addWindow.ShowDialog() == true)
            // Refresh the main grid so the newly created item appears
            LoadStockItems();
    }

    private void BtnEWasteDashboard_Click(object sender, RoutedEventArgs e)
    {
        var ewasteDashboard = new EWasteDashboardWindow
        {
            Owner = this
        };

        ewasteDashboard.ShowDialog();
    }

    private void BtnExportReport_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "CSV Report (*.csv)|*.csv",
            FileName = $"Stock_Report_{DateTime.Now:MMM_yyyy}.csv",
            Title = "Save Monthly Report"
        };

        if (saveFileDialog.ShowDialog() != true)
            return;

        try
        {
            var sb = new StringBuilder();

            var serialisedRepo = new SerialisedUnitRepository();
            var allStock = _stockRepo.GetAllStockItems().OrderBy(i => i.Name).ToList();
            var allOnHandUnits = serialisedRepo.GetSerialisedUnitsByStatus(UnitStatus.OnHand);

            sb.AppendLine("--- CURRENT STOCK ON HAND ---");
            sb.AppendLine("Item Code,Name,Category,Supply Type,Quantity On Hand,Serial Numbers");

            foreach (var item in allStock)
            {
                var serialsString = "N/A";
                var quantityOnHand = item.Quantity;

                if (item.IsSerialised)
                {
                    var matchingSerials = allOnHandUnits
                        .Where(u => u.StockItemId == item.Id)
                        .Select(u => u.SerialNumber)
                        .OrderBy(s => s)
                        .ToList();

                    quantityOnHand = matchingSerials.Count;
                    serialsString = matchingSerials.Count > 0
                        ? string.Join(" | ", matchingSerials)
                        : "None";
                }

                sb.AppendLine(
                    $"{Csv(item.ItemCode)},{Csv(item.Name)},{Csv(item.Category)},{Csv(item.SupplyType.ToString())},{quantityOnHand},{Csv(serialsString)}");
            }

            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine("--- ACTIVE E-WASTE PIPELINE ---");
            sb.AppendLine("Serial Number,Item Code,Hardware,Current Status,Date Logged");

            var ewasteUnits = serialisedRepo.GetAllSerialisedUnits()
                .Where(u => u.Status == UnitStatus.EwastePendingSubmission ||
                            u.Status == UnitStatus.EwasteAwaitingApproval)
                .OrderBy(u => u.Status)
                .ThenBy(u => u.LastUpdatedUtc)
                .ToList();

            foreach (var unit in ewasteUnits)
            {
                var parentItem = allStock.FirstOrDefault(s => s.Id == unit.StockItemId);

                sb.AppendLine(
                    $"{Csv(unit.SerialNumber)},{Csv(parentItem?.ItemCode ?? "N/A")},{Csv(parentItem?.Name ?? "Unknown")},{Csv(unit.Status.ToString())},{unit.LastUpdatedUtc:dd/MM/yyyy}");
            }

            File.WriteAllText(saveFileDialog.FileName, sb.ToString());

            MessageBox.Show(
                "Monthly report exported successfully!",
                "Export Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to export report: {ex.Message}",
                "Export Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static string Csv(string value)
    {
        value ??= "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }

    private async void BtnSyncJobCards_Click(object sender, RoutedEventArgs e)
    {
        var vault = new WindowsCredentialVault();
        var config = ConfigManager.LoadEmailConfig(vault);

        // 1. Validate if we have a basic setup at all
        if (config == null || string.IsNullOrEmpty(config.Username))
        {
            MessageBox.Show("You need to configure your email settings before syncing job cards.", "Settings Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            new EmailSettingsWindow { Owner = this }.ShowDialog();
            return;
        }

        var isM365 = config.ProviderType == EmailProvider.Microsoft365;

        // 2. If it's legacy IMAP, ensure they have an App Password saved
        if (!isM365 && string.IsNullOrEmpty(config.Password))
        {
            MessageBox.Show("You need to enter an App Password for your email before syncing.", "Password Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            new EmailSettingsWindow { Owner = this }.ShowDialog();
            return;
        }

        // 3. If it's Microsoft 365, grab the fresh OAuth token silently from your cache
        if (isM365)
        {
            var authWindow = new EmailSettingsWindow();
            var activeToken = await authWindow.GetTokenAsync();

            if (string.IsNullOrEmpty(activeToken))
            {
                MessageBox.Show(
                    "Your Microsoft session has expired or is invalid. Please open Settings and Sign In again.",
                    "Authentication Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                new EmailSettingsWindow { Owner = this }.ShowDialog();
                return;
            }

            // Inject the fresh token directly into the config object in memory.
            // This ensures JobCardProcessor and EmailHookService use the active token!
            config.AccessToken = activeToken;
        }

        BtnSyncJobCards.IsEnabled = false;
        BtnSyncJobCards.Content = "Syncing...";

        try
        {
            // The processor now receives the config containing the fresh AccessToken
            var processor = new JobCardProcessor(config);
            var result = await processor.RunSyncAsync();

            MessageBox.Show($"Sync Complete.\nProcessed: {result.Processed} jobs.\nErrors: {result.Errors}.",
                "Job Sync", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadStockItems(); // Refresh grids
        }
        catch (Exception ex)
        {
            // Generalized the error message so it applies to both legacy and modern auth
            MessageBox.Show(
                $"Failed to sync job cards: {ex.Message}\n\nCheck your authentication and network settings.",
                "Sync Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnSyncJobCards.IsEnabled = true;
            BtnSyncJobCards.Content = "Sync Job Cards";
        }
    }

    private void BtnEmailSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new EmailSettingsWindow
        {
            Owner = this
        };

        settingsWindow.ShowDialog();
    }
}