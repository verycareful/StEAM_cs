using StEAM_.NET_main.Services;

namespace StEAM_.NET_main.Pages;

public partial class LoadingPage : ContentPage
{
    private readonly SupabaseService _supabaseService;

    public LoadingPage(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _supabaseService.WaitForInitializationAsync();

            if (_supabaseService.HasValidSession())
            {
                // Session valid — skip login, go straight to role selector
                await Shell.Current.GoToAsync("//LoginPage/RoleSelectorPage");
            }
            else
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
        catch
        {
            // Init failed or timed out — show login page
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
