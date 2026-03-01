using StEAM_.NET_main.Services;
using StEAM_.NET_main.ViewModels;

namespace StEAM_.NET_main.Pages;

public partial class FloorStaffPage : ContentPage
{
    private readonly FloorStaffViewModel _viewModel;

    public FloorStaffPage(FloorStaffViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        SearchEntry.TextChanged += OnSearchEntryTextChanged;
    }

    private async void OnSearchEntryTextChanged(object? sender, TextChangedEventArgs e)
    {
        await _viewModel.OnSearchTextChangedAsync(e.NewTextValue ?? "");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FloorStaffViewModel.PopupMessage) &&
            !string.IsNullOrEmpty(_viewModel.PopupMessage))
        {
            await SnackbarHelper.ShowAsync(_viewModel.PopupMessage);
            _viewModel.PopupMessage = null;
        }
    }
}
