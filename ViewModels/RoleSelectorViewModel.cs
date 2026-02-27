using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StEAM_.NET_main.Models;
using StEAM_.NET_main.Services;

namespace StEAM_.NET_main.ViewModels;

public partial class RoleSelectorViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService;

    public RoleSelectorViewModel(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    [ObservableProperty]
    private string _welcomeMessage = "";

    [ObservableProperty]
    private string _roleDisplay = "";

    [ObservableProperty]
    private string _buttonText = "";

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasDetails;

    private Role? _userRole;

    [RelayCommand]
    private async Task LoadDetailsAsync()
    {
        // Always reload (no _hasLoaded flag) so fresh data is shown after re-login
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var details = await _supabaseService.GetUserDetailsAsync();
            if (details != null)
            {
                WelcomeMessage = $"Welcome, {details.Name}";
                RoleDisplay = $"Role: {details.Role}";

                _userRole = RoleExtensions.FromDbValue(details.Role);
                ButtonText = _userRole != null ? $"Continue as {_userRole}" : "";
                HasDetails = _userRole != null;

                if (_userRole == null)
                    ErrorMessage = $"Unknown role: {details.Role}";
            }
            else
            {
                ErrorMessage = "Could not fetch user details.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (_userRole == null) return;

        var route = _userRole switch
        {
            Role.FloorStaff => "FloorStaffPage",
            Role.Staff => "StaffPage",
            _ => null
        };

        if (route != null)
            await Shell.Current.GoToAsync(route);
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        await _supabaseService.SignOutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    [RelayCommand]
    private async Task OpenDashboardAsync()
    {
        await Shell.Current.GoToAsync("DashboardPage");
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await Shell.Current.GoToAsync("SettingsPage");
    }
}
