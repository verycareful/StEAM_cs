namespace StEAM_.NET_main.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly ViewModels.DashboardViewModel _viewModel;

    public DashboardPage(ViewModels.DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTodayDataCommand.ExecuteAsync(null);
    }
}
