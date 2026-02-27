using CommunityToolkit.Maui;
using Plugin.Maui.OCR;
using ZXing.Net.Maui.Controls;
using StEAM_.NET_main.Pages;
using StEAM_.NET_main.Services;
using StEAM_.NET_main.ViewModels;

namespace StEAM_.NET_main;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseOcr()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter-Variable.ttf", "Inter");
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services
        builder.Services.AddSingleton<SupabaseService>();
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton<NfcService>();
        builder.Services.AddSingleton<LateComingService>();

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RoleSelectorViewModel>();
        builder.Services.AddTransient<FloorStaffViewModel>();
        builder.Services.AddTransient<NfcScanViewModel>();
        builder.Services.AddTransient<CameraScanViewModel>();
        builder.Services.AddTransient<StaffViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();

        // Register Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RoleSelectorPage>();
        builder.Services.AddTransient<FloorStaffPage>();
        builder.Services.AddTransient<NfcScanPage>();
        builder.Services.AddTransient<CameraScanPage>();
        builder.Services.AddTransient<StaffPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<LoadingPage>();

        var app = builder.Build();

        // Initialize background services
        Task.Run(async () =>
        {
            var supabase = app.Services.GetRequiredService<SupabaseService>();
            try
            {
                // Load env from assets (renamed from .env — Android excludes dot-files)
                using var stream = await FileSystem.OpenAppPackageFileAsync("supabase.env");
                using var reader = new StreamReader(stream);
                var contents = await reader.ReadToEndAsync();
                DotNetEnv.Env.LoadContents(contents);

                await supabase.InitializeAsync();
                // Library handles session restore via CustomSessionPersistence internally
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Init error: {ex.Message}");
                supabase.SignalInitComplete(ex);
                return;
            }
            finally
            {
                // Always signal completion so login doesn't hang forever
                supabase.SignalInitComplete();
            }
        });

        return app;
    }
}
