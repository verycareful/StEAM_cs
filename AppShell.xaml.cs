using StEAM_.NET_main.Pages;
using StEAM_.NET_main.Services;

namespace StEAM_.NET_main;

public partial class AppShell : Shell
{
    private const string LoginRoute = "LoginPage";

    private readonly SupabaseService _supabaseService;

    public AppShell(SupabaseService supabaseService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        // Resolve LoadingPage from DI as the initial content
        var loadingPage = serviceProvider.GetRequiredService<LoadingPage>();
        LoadingContent.ContentTemplate = null;
        LoadingContent.Content = loadingPage;

        // Resolve LoginPage from DI
        var loginPage = serviceProvider.GetRequiredService<LoginPage>();
        LoginContent.ContentTemplate = null;
        LoginContent.Content = loginPage;

        // Register routes for navigation
        Routing.RegisterRoute("RoleSelectorPage", typeof(RoleSelectorPage));
        Routing.RegisterRoute("FloorStaffPage", typeof(FloorStaffPage));
        Routing.RegisterRoute("CameraScanPage", typeof(CameraScanPage));
        Routing.RegisterRoute("NfcScanPage", typeof(NfcScanPage));
        Routing.RegisterRoute("StaffPage", typeof(StaffPage));
        Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));
        Routing.RegisterRoute("DashboardPage", typeof(DashboardPage));
    }
}
