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

    /// <summary>
    /// The most recently discovered native NFC Tag, captured from the intent
    /// before Plugin.NFC processes it. Used by NfcService to perform
    /// MIFARE Classic sector reads.
    /// </summary>
    public static Android.Nfc.Tag? LastNfcTag { get; private set; }

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

        // Capture the native Tag from the NFC intent before Plugin.NFC processes it.
        // ITagInfo does not expose the underlying Android.Nfc.Tag, so we store it here
        // for NfcService.ProcessTag to use when reading MIFARE Classic sectors.
        if (intent != null &&
            (intent.Action == NfcAdapter.ActionTagDiscovered ||
             intent.Action == NfcAdapter.ActionNdefDiscovered ||
             intent.Action == NfcAdapter.ActionTechDiscovered))
        {
#pragma warning disable CA1422 // Validate platform compatibility (deprecated but functional on all API levels)
            LastNfcTag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Android.Nfc.Tag;
#pragma warning restore CA1422
        }

        Plugin.NFC.CrossNFC.OnNewIntent(intent);
    }
}
