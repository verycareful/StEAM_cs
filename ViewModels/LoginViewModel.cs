using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StEAM_.NET_main.Services;

namespace StEAM_.NET_main.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService;

    public LoginViewModel(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter both email and password.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Wait for background Supabase initialization to complete
            await _supabaseService.WaitForInitializationAsync();
            await _supabaseService.SignInAsync(Email, Password);

            // Navigate to role selector — session is auto-persisted by CustomSessionPersistence
            await Shell.Current.GoToAsync("//LoginPage/RoleSelectorPage");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
