using StEAM_.NET_main.Services;

namespace StEAM_.NET_main;

public partial class App : Application
{
    private readonly SupabaseService _supabaseService;
    private readonly ThemeService _themeService;
    private readonly IServiceProvider _serviceProvider;

    public App(SupabaseService supabaseService, ThemeService themeService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _supabaseService = supabaseService;
        _themeService = themeService;
        _serviceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Initialize theme AFTER app is fully constructed
        _themeService.Initialize();

        // Initialize SnackbarHelper with ThemeService for accurate dark mode detection
        SnackbarHelper.Initialize(_themeService);

        return new Window(new AppShell(_supabaseService, _serviceProvider));
    }
}