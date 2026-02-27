using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace StEAM_.NET_main.Services;

/// <summary>
/// Shared snackbar helper that respects the current theme.
/// Uses ThemeService.GetEffectiveTheme() for accurate dark mode detection (Issue #16).
/// </summary>
public static class SnackbarHelper
{
    private static ThemeService? _themeService;

    /// <summary>
    /// Initialize with ThemeService reference for accurate theme detection.
    /// Called once during app startup.
    /// </summary>
    public static void Initialize(ThemeService themeService)
    {
        _themeService = themeService;
    }

    public static async Task ShowAsync(string message, TimeSpan? duration = null)
    {
        // Use ThemeService for accurate dark mode detection including System theme
        var isDark = _themeService?.GetEffectiveTheme() == AppTheme.Dark;

        var options = new SnackbarOptions
        {
            BackgroundColor = isDark ? Color.FromArgb("#E0E0E0") : Color.FromArgb("#323232"),
            TextColor = isDark ? Color.FromArgb("#1A1A1A") : Colors.White,
            CornerRadius = 8,
            Font = Microsoft.Maui.Font.SystemFontOfSize(14)
        };

        var snackbar = Snackbar.Make(
            message,
            duration: duration ?? TimeSpan.FromSeconds(3),
            visualOptions: options);
        await snackbar.Show();
    }
}
