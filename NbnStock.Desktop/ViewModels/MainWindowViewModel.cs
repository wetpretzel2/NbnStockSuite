using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NbnStock.Desktop.ViewModels;


public class MainWindowViewModel : INotifyPropertyChanged
{
    private object? _currentPage;
    public RelayCommand ReceiveCommand { get; }
    public RelayCommand InventoryCommand { get; }
    public RelayCommand RefreshCommand { get; }

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

        InventoryCommand = new RelayCommand(ShowInventory);
        ReceiveCommand = new RelayCommand(ShowReceive);
        RefreshCommand = new RelayCommand(RefreshCurrentPage);
    }
    
    
    public void RefreshCurrentPage()
    {
        if (CurrentPage is InventoryViewModel inventoryViewModel)
        {
            inventoryViewModel.LoadStockItems();
        }
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

    public void ShowInventory()
    {
        CurrentPage = new InventoryViewModel();
    }
}