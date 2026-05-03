using NbnStock.Core.Repositories;
using NbnStock.Core.Models;
using System.Linq;
using System.Windows;
using System.IO;
using System.Text;

namespace NbnStock.Windows
{
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

            // Sort the list logically before binding it to the main DataGrid
            var sortedItems = rawItems
                .OrderByDescending(i => i.IsSerialised)          // 1. Serialised units always at the top
                .ThenBy(i => i.SupplyType.ToString() == "TechSupplied" ? 1 : 0) // 2. Tech Supplied pushed to the bottom
                .ThenBy(i => GetCategorySortWeight(i.Category))  // 3. Sort NBN stuff by Mounts, Cables, Wallplates
                .ThenBy(i => i.Name)                             // 4. Alphabetical within those groups
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
            {
                // Refresh the main grid once the batch is committed
                LoadStockItems();
            }
        }

        private void BtnConsumeStock_Click(object sender, RoutedEventArgs e)
        {
            var bulkConsumeWindow = new BulkConsumeWindow
            {
                Owner = this
            };

            if (bulkConsumeWindow.ShowDialog() == true)
            {
                // Refresh the main grid once the daily batch is committed
                LoadStockItems();
            }
        }
        private void BtnAddCustomItem_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddCustomItemWindow
            {
                Owner = this
            };

            if (addWindow.ShowDialog() == true)
            {
                // Refresh the main grid so the newly created item appears
                LoadStockItems();
            }
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
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Report (*.csv)|*.csv",
                FileName = $"Stock_Report_{DateTime.Now:MMM_yyyy}.csv",
                Title = "Save Monthly Report"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();

                    // Move the serialised repo up here so we can use it for both sections
                    var serialisedRepo = new SerialisedUnitRepository();

                    // --- SECTION 1: MASTER STOCK ON HAND ---
                    sb.AppendLine("--- CURRENT STOCK ON HAND ---");

                    // ADDED: "Serial Numbers" to the column headers
                    sb.AppendLine("Item Code,Name,Category,Supply Type,Quantity On Hand,Serial Numbers");

                    var allStock = _stockRepo.GetAllStockItems().OrderBy(i => i.Name).ToList();

                    // Pre-fetch all OnHand units so we don't have to hit the database 100 times in the loop below
                    var allOnHandUnits = serialisedRepo.GetSerialisedUnitsByStatus(UnitStatus.OnHand);

                    foreach (var item in allStock)
                    {
                        // We replace commas with spaces in the name so it doesn't break the CSV columns
                        string cleanName = item.Name.Replace(",", " ");
                        string serialsString = "N/A";

                        // ADDED: If it's serialised, find the exact serial numbers currently in stock
                        if (item.IsSerialised)
                        {
                            var matchingSerials = allOnHandUnits
                                .Where(u => u.StockItemId == item.Id)
                                .Select(u => u.SerialNumber)
                                .ToList();

                            if (matchingSerials.Count > 0)
                            {
                                // Join them with a separator so they stay inside a single Excel cell
                                serialsString = string.Join(" | ", matchingSerials);
                            }
                            else
                            {
                                serialsString = "None";
                            }
                        }

                        // Append the row, now including the serialsString at the end
                        sb.AppendLine($"{item.ItemCode},{cleanName},{item.Category},{item.SupplyType},{item.Quantity},{serialsString}");
                    }

                    // Add a couple of blank lines to separate the sections
                    sb.AppendLine();
                    sb.AppendLine();

                    // --- SECTION 2: ACTIVE E-WASTE PIPELINE ---
                    sb.AppendLine("--- ACTIVE E-WASTE PIPELINE ---");
                    sb.AppendLine("Serial Number,Item Code,Hardware,Current Status,Date Logged");

                    // We specifically filter OUT 'ApprovedForDisposal' and 'Disposed' 
                    var ewasteUnits = serialisedRepo.GetAllSerialisedUnits()
                        .Where(u => u.Status == UnitStatus.EwastePendingSubmission ||
                                    u.Status == UnitStatus.EwasteAwaitingApproval)
                        .OrderBy(u => u.Status)
                        .ThenBy(u => u.LastUpdatedUtc)
                        .ToList();

                    foreach (var unit in ewasteUnits)
                    {
                        var parentItem = allStock.FirstOrDefault(s => s.Id == unit.StockItemId);
                        string itemName = parentItem != null ? parentItem.Name.Replace(",", " ") : "Unknown";
                        string itemCode = parentItem != null ? parentItem.ItemCode : "N/A";

                        sb.AppendLine($"{unit.SerialNumber},{itemCode},{itemName},{unit.Status},{unit.LastUpdatedUtc:dd/MM/yyyy}");
                    }

                    // Write the whole chunk of text to the file location you chose
                    File.WriteAllText(saveFileDialog.FileName, sb.ToString());

                    MessageBox.Show("Monthly report exported successfully!\n\nYou can now double-click the file to open it in Excel, print it, or email it.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export report: {ex.Message}\n\nMake sure you don't already have the file open in Excel while trying to save over it.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}