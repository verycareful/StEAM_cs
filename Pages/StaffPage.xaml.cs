using StEAM_.NET_main.ViewModels;

namespace StEAM_.NET_main.Pages;

public partial class StaffPage : ContentPage
{
    private readonly StaffViewModel _viewModel;

    public StaffPage(StaffViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadLookupsCommand.ExecuteAsync(null);
    }
}
