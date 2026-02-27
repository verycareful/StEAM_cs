namespace StEAM_.NET_main.Services;

public enum ThemeMode
{
    System,
    Light,
    Dark
}

public enum AccentColor
{
    Blue,
    Orange
}

public class ThemeService
{
    private const string ThemeModeKey = "theme_mode";
    private const string AccentColorKey = "accent_color";

    public ThemeMode CurrentThemeMode { get; private set; } = ThemeMode.System;
    public AccentColor CurrentAccentColor { get; private set; } = AccentColor.Blue;

    public event Action? ThemeChanged;

    public void Initialize()
    {
        // Load persisted preferences, default to System theme
        var savedTheme = Preferences.Default.Get(ThemeModeKey, "System");
        CurrentThemeMode = Enum.TryParse<ThemeMode>(savedTheme, out var mode) ? mode : ThemeMode.System;

        var savedAccent = Preferences.Default.Get(AccentColorKey, "Blue");
        CurrentAccentColor = Enum.TryParse<AccentColor>(savedAccent, out var accent) ? accent : AccentColor.Blue;

        ApplyTheme();
    }

    public void SetThemeMode(ThemeMode mode)
    {
        CurrentThemeMode = mode;
        Preferences.Default.Set(ThemeModeKey, mode.ToString());
        ApplyTheme();
    }

    public void SetAccentColor(AccentColor color)
    {
        CurrentAccentColor = color;
        Preferences.Default.Set(AccentColorKey, color.ToString());
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        // Ensure all UI resource mutations happen on the main thread (Issue #15)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Application.Current == null) return;

            // Set app theme (System / Light / Dark)
            Application.Current.UserAppTheme = CurrentThemeMode switch
            {
                ThemeMode.Light => AppTheme.Light,
                ThemeMode.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified // Follows system
            };

            // Apply accent colors directly to app resources
            var resources = Application.Current.Resources;

            if (CurrentAccentColor == AccentColor.Orange)
            {
                resources["Primary"] = Color.FromArgb("#E65100");
                resources["OnPrimary"] = Color.FromArgb("#FFFFFF");
                resources["PrimaryContainer"] = Color.FromArgb("#FFE0B2");
                resources["OnPrimaryContainer"] = Color.FromArgb("#3E2100");
                resources["Secondary"] = Color.FromArgb("#FF8F00");
                resources["OnSecondary"] = Color.FromArgb("#FFFFFF");
                resources["Tertiary"] = Color.FromArgb("#FF6D00");
                resources["PrimaryDark"] = Color.FromArgb("#E65100");
                resources["SecondaryDark"] = Color.FromArgb("#FF8F00");
            }
            else
            {
                resources["Primary"] = Color.FromArgb("#1565C0");
                resources["OnPrimary"] = Color.FromArgb("#FFFFFF");
                resources["PrimaryContainer"] = Color.FromArgb("#BBDEFB");
                resources["OnPrimaryContainer"] = Color.FromArgb("#002171");
                resources["Secondary"] = Color.FromArgb("#1E88E5");
                resources["OnSecondary"] = Color.FromArgb("#FFFFFF");
                resources["Tertiary"] = Color.FromArgb("#0D47A1");
                resources["PrimaryDark"] = Color.FromArgb("#1565C0");
                resources["SecondaryDark"] = Color.FromArgb("#1E88E5");
            }

            ThemeChanged?.Invoke();
        });
    }

    public AppTheme GetEffectiveTheme()
    {
        if (CurrentThemeMode == ThemeMode.System)
            return Application.Current?.RequestedTheme ?? AppTheme.Light;

        return CurrentThemeMode == ThemeMode.Dark ? AppTheme.Dark : AppTheme.Light;
    }
}
