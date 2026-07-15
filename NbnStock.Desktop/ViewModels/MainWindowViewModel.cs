using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace NbnStock.Desktop.ViewModels;
using System.Collections.ObjectModel;
using NbnStock.Core.Models;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<StockItem> StockItems { get; } = [];
    private string _statusMessage = "Ready";
    
    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
    
    
    
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value)
                return;

            _statusMessage = value;
            OnPropertyChanged();
        }
    }
}