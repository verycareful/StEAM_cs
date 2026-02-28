using StEAM_.NET_main.Services;
using StEAM_.NET_main.ViewModels;

namespace StEAM_.NET_main.Pages;

public partial class NfcScanPage : ContentPage
{
    private readonly NfcScanViewModel _viewModel;

    public NfcScanPage(NfcScanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe to popup messages (fresh subscription each time)
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        await _viewModel.StartListeningAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe to prevent handler leak (Bug 3 fix)
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel.Cleanup();
    }

    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NfcScanViewModel.PopupMessage) &&
            !string.IsNullOrEmpty(_viewModel.PopupMessage))
        {
            await SnackbarHelper.ShowAsync(_viewModel.PopupMessage);
            _viewModel.PopupMessage = null;
        }
    }
}
