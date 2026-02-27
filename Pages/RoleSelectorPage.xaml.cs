using StEAM_.NET_main.ViewModels;

namespace StEAM_.NET_main.Pages;

public partial class RoleSelectorPage : ContentPage
{
    private readonly RoleSelectorViewModel _viewModel;

    public RoleSelectorPage(RoleSelectorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Always reload details (VM is now Transient, so each page gets fresh data)
        await _viewModel.LoadDetailsCommand.ExecuteAsync(null);
    }
}
