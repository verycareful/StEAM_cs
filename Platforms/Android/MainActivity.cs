using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.OS;

namespace StEAM_.NET_main;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private NfcAdapter? _nfcAdapter;
    private PendingIntent? _nfcPendingIntent;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Plugin.NFC.CrossNFC.Init(this);

        // Set up foreground dispatch (intercepts NFC only while THIS activity is in foreground)
        _nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
        var intent = new Intent(this, GetType()).AddFlags(ActivityFlags.SingleTop);
        _nfcPendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Mutable);
    }

    protected override void OnResume()
    {
        base.OnResume();
        Plugin.NFC.CrossNFC.OnResume();

        // Enable foreground dispatch so NFC tags are handled by us, not by Android's default behavior
        _nfcAdapter?.EnableForegroundDispatch(this, _nfcPendingIntent, null, null);
    }

    protected override void OnPause()
    {
        base.OnPause();

        // Disable foreground dispatch when the activity is not in the foreground
        _nfcAdapter?.DisableForegroundDispatch(this);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Plugin.NFC.CrossNFC.OnNewIntent(intent);
    }
}
