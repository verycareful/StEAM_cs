using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StEAM_.NET_main.Services;

namespace StEAM_.NET_main.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ThemeService _themeService;
    private readonly SupabaseService _supabaseService;

    public SettingsViewModel(ThemeService themeService, SupabaseService supabaseService)
    {
        _themeService = themeService;
        _supabaseService = supabaseService;
        _selectedThemeIndex = (int)_themeService.CurrentThemeMode;
        _isOrangeAccent = _themeService.CurrentAccentColor == AccentColor.Orange;
    }

    // --- Profile ---
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _userEmail = "";
    [ObservableProperty] private string _userRole = "";
    [ObservableProperty] private bool _isProfileMissing;

    // --- Theme ---
    [ObservableProperty] private int _selectedThemeIndex;
    [ObservableProperty] private bool _isOrangeAccent;

    public string[] ThemeOptions => ["System", "Light", "Dark"];

    partial void OnSelectedThemeIndexChanged(int value)
    {
        _themeService.SetThemeMode((ThemeMode)value);
    }

    partial void OnIsOrangeAccentChanged(bool value)
    {
        _themeService.SetAccentColor(value ? AccentColor.Orange : AccentColor.Blue);
    }

    [RelayCommand]
    private async Task LoadProfileAsync()
    {
        try
        {
            var details = await _supabaseService.GetUserDetailsAsync();
            if (details != null)
            {
                UserName = details.Name;
                UserRole = details.Role;
                IsProfileMissing = false;
            }
            else
            {
                IsProfileMissing = true;
            }
            // Get email from auth
            UserEmail = _supabaseService.IsInitialized
                ? _supabaseService.Client.Auth.CurrentUser?.Email ?? ""
                : "";
        }
        catch
        {
            IsProfileMissing = true;
        }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        await _supabaseService.SignOutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    [RelayCommand]
    private async Task ReportIssueAsync()
    {
        try
        {
            var message = new EmailMessage
            {
                Subject = "StEAM App — Issue Report",
                Body = "Please describe the issue:\n\n",
                To = ["ss1833@srmist.edu.in"]
            };
            await Email.Default.ComposeAsync(message);
        }
        catch
        {
            // Fallback: open mailto in browser
            await Launcher.Default.OpenAsync("mailto:ss1833@srmist.edu.in?subject=StEAM%20App%20-%20Issue%20Report");
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
