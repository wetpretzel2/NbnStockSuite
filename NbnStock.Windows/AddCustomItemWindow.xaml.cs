using System.Windows;
using System.Windows.Controls;
using NbnStock.Core.Models;
using NbnStock.Core.Repositories;

namespace NbnStock.Windows;

public partial class AddCustomItemWindow : Window
{
    private readonly StockRepository _stockRepo;
    private bool _isManuallyEditingCode = false;

    public AddCustomItemWindow()
    {
        InitializeComponent();
        _stockRepo = new StockRepository();

        // Set focus directly to the Name box so you can start typing
        InputName.Focus();

        // If the user clicks into the Code box, we assume they are taking over manually
        InputCode.GotFocus += (s, e) => _isManuallyEditingCode = true;
    }

    private void InputName_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Stop auto-generating if the user has decided to type their own custom code
        if (_isManuallyEditingCode) return;

        string name = InputName.Text.Trim().ToUpper();
        if (string.IsNullOrWhiteSpace(name))
        {
            InputCode.Text = "";
            return;
        }

        // Split the name into words
        var words = name.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
        string codeSuffix = "";

        if (words.Length == 1)
            // One word: "SADDLES" -> "SAD"
            codeSuffix = words[0].Substring(0, Math.Min(3, words[0].Length));
        else if (words.Length >= 2)
            // Two or more words: "CABLE CLIPS" -> "CABCLI"
            foreach (var word in words.Take(2))
                codeSuffix += word.Substring(0, Math.Min(3, word.Length));

        InputCode.Text = $"TECH-{codeSuffix}";
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(InputName.Text) ||
                string.IsNullOrWhiteSpace(InputCode.Text) ||
                string.IsNullOrWhiteSpace(InputUnit.Text))
            {
                MessageBox.Show("Please fill in all required fields (Name, Code, Unit).", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(InputQty.Text, out int qty);
            int.TryParse(InputMin.Text, out int minStock);

            var newItem = new StockItem
            {
                ItemCode = InputCode.Text.Trim(),
                Name = InputName.Text.Trim(),
                Category = InputCategory.Text.Trim(),
                Unit = InputUnit.Text.Trim(),
                Quantity = qty,
                MinimumStock = minStock,
                IsSerialised = ChkIsSerialised.IsChecked == true,
                SupplyType = SupplyType.TechSupplied,
                Notes = "Added manually via UI",
                LastUpdatedUtc = DateTime.UtcNow
            };

            _stockRepo.AddStockItem(newItem);

            MessageBox.Show("Item successfully added to inventory.", "Success", MessageBoxButton.OK,
                MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save item. Make sure the Item Code is unique.\n\nError: {ex.Message}",
                "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}