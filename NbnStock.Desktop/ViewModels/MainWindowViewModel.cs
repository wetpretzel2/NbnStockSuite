using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NbnStock.Desktop.ViewModels;


public class MainWindowViewModel : INotifyPropertyChanged
{
    private object? _currentPage;

    public object? CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (_currentPage == value)
                return;

            _currentPage = value;
            OnPropertyChanged();
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    
    
    public MainWindowViewModel()
    {
        CurrentPage = new InventoryViewModel();
    }
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
    public void ShowReceive()
    {
        CurrentPage = new ReceiveViewModel();
    }

    
}