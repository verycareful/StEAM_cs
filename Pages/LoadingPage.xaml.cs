using StEAM_.NET_main.Services;

#if ANDROID
using Android.Util;
#endif

namespace StEAM_.NET_main.Pages;

public partial class LoadingPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private const string LogTag = "StEAM.Loading";

    public LoadingPage(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

#if ANDROID
        Log.Info(LogTag, "LoadingPage.OnAppearing: Starting");
#endif

        try
        {
            await _supabaseService.WaitForInitializationAsync();
#if ANDROID
            Log.Info(LogTag, "Supabase initialized");
#endif

            var hasValidSession = _supabaseService.HasValidSession();
#if ANDROID
            Log.Info(LogTag, $"HasValidSession: {hasValidSession}");
#endif

            if (hasValidSession)
            {
#if ANDROID
                Log.Info(LogTag, "Valid session, going to RoleSelectorPage");
#endif
                await Shell.Current.GoToAsync("//LoginPage/RoleSelectorPage");
            }
            else
            {
#if ANDROID
                Log.Info(LogTag, "No valid session, going to LoginPage");
#endif
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
        catch (Exception ex)
        {
#if ANDROID
            Log.Error(LogTag, $"Exception: {ex.GetType().Name}: {ex.Message}");
#endif
        }
    }
}
