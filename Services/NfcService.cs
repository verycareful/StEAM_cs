using Plugin.NFC;
#if ANDROID
using Android.Nfc;
using Android.Nfc.Tech;
#endif

namespace StEAM_.NET_main.Services;

public class NfcService
{
    public event Action<string>? TagScanned;
    public event Action<string>? ErrorOccurred;

    private bool _isListening;

    /// <summary>
    /// Checks if NFC hardware is available (built-in or external reader).
    /// </summary>
    public bool IsAvailable
    {
        get
        {
            try
            {
                return CrossNFC.IsSupported && CrossNFC.Current.IsAvailable;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Checks if NFC is currently enabled on the device.
    /// </summary>
    public bool IsEnabled
    {
        get
        {
            try
            {
                return IsAvailable && CrossNFC.Current.IsEnabled;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool IsListening => _isListening;

    /// <summary>
    /// Start listening for NFC tags with retry support.
    /// </summary>
    public void StartListening()
    {
        if (!IsAvailable) return;

        try
        {
            // CrossNFC.OnResume() re-initializes the plugin's internal adapter connection.
            // MainActivity.OnResume only fires when returning from background — it does NOT
            // fire for in-process MAUI Shell navigation (e.g. camera page → back → NFC page).
            // Calling it here every time StartListening is called is safe and necessary.
            CrossNFC.OnResume();

            // Always do a clean stop first to reset any stale state
            ForceStopClean();

            CrossNFC.Current.OnMessageReceived -= OnNfcMessageReceived;
            CrossNFC.Current.OnMessageReceived += OnNfcMessageReceived;
            CrossNFC.Current.OnTagDiscovered -= OnTagDiscovered;
            CrossNFC.Current.OnTagDiscovered += OnTagDiscovered;

            CrossNFC.Current.StartListening();
            _isListening = true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Failed to start NFC: {ex.Message}");
        }
    }

    /// <summary>
    /// Start listening with automatic retry if the first attempt fails
    /// (e.g. after camera hardware release on a slow device).
    /// </summary>
    public async Task StartListeningWithRetryAsync(int maxRetries = 3, int delayMs = 500)
    {
        // Note: CrossNFC.OnResume() is called in MainActivity.OnResume —
        // do NOT call it here (calling it multiple times corrupts internal state)

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                ForceStopClean();

                CrossNFC.Current.OnMessageReceived -= OnNfcMessageReceived;
                CrossNFC.Current.OnMessageReceived += OnNfcMessageReceived;
                CrossNFC.Current.OnTagDiscovered -= OnTagDiscovered;
                CrossNFC.Current.OnTagDiscovered += OnTagDiscovered;

                CrossNFC.Current.StartListening();
                _isListening = true;
                return;
            }
            catch (Exception ex)
            {
                if (attempt < maxRetries)
                {
                    await Task.Delay(delayMs);
                }
                else
                {
                    ErrorOccurred?.Invoke($"Failed to start NFC after {maxRetries + 1} attempts: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Stop listening for NFC tags.
    /// </summary>
    public void StopListening()
    {
        ForceStopClean();
    }

    /// <summary>
    /// Internal clean stop that always resets state regardless of _isListening flag.
    /// </summary>
    private void ForceStopClean()
    {
        try
        {
            if (_isListening)
            {
                CrossNFC.Current.StopListening();
            }
        }
        catch (Exception ex)
        {
        }

        try
        {
            CrossNFC.Current.OnMessageReceived -= OnNfcMessageReceived;
            CrossNFC.Current.OnTagDiscovered -= OnTagDiscovered;
        }
        catch (Exception ex)
        {
        }

        _isListening = false;
    }

    private void OnNfcMessageReceived(ITagInfo tagInfo)
    {
        ProcessTag(tagInfo);
    }

    private void OnTagDiscovered(ITagInfo tagInfo, bool format)
    {
        ProcessTag(tagInfo);
    }

    private void ProcessTag(ITagInfo tagInfo)
    {
        if (tagInfo == null) return;

#if ANDROID
        try
        {
            // Get the native Tag captured by MainActivity.OnNewIntent.
            // ITagInfo does not expose the underlying Android.Nfc.Tag,
            // so MainActivity stores it in a static property for us.
            var nativeTag = MainActivity.LastNfcTag;
            if (nativeTag == null)
            {
                ErrorOccurred?.Invoke("Could not read card hardware.");
                return;
            }

            // Check this is a MifareClassic card — rejects NCMC, NTAG, Ultralight, etc.
            var mifare = MifareClassic.Get(nativeTag);
            if (mifare == null)
            {
                ErrorOccurred?.Invoke("Unrecognized card type.");
                return;
            }

            try
            {
                mifare.Connect();

                // Authenticate Sector 0 with factory default Key A (FF FF FF FF FF FF)
                var defaultKey = MifareClassic.KeyDefault?.ToArray()
                    ?? new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                bool auth = mifare.AuthenticateSectorWithKeyA(0, defaultKey);
                if (!auth)
                {
                    ErrorOccurred?.Invoke("Card authentication failed.");
                    return;
                }

                // Block 1 is the second block of Sector 0 (block index 1)
                var block1 = mifare.ReadBlock(1);
                // Block 2 is the third block of Sector 0 (block index 2)
                var block2 = mifare.ReadBlock(2);

                if (block1 == null || block1.Length < 8 || block2 == null || block2.Length < 7)
                {
                    ErrorOccurred?.Invoke("Could not read card data.");
                    return;
                }

                // Extract register number: first 8 bytes of block1 + first 7 bytes of block2
                var part1 = System.Text.Encoding.ASCII.GetString(block1, 0, 8).Trim();
                var part2 = System.Text.Encoding.ASCII.GetString(block2, 0, 7).Trim();
                var registerNumber = part1 + part2;

                // Validate format: exactly 2 uppercase letters followed by 13 digits
                if (!System.Text.RegularExpressions.Regex.IsMatch(registerNumber, @"^[A-Z]{2}\d{13}$"))
                {
                    ErrorOccurred?.Invoke("Unrecognized card.");
                    return;
                }

                TagScanned?.Invoke(registerNumber);
            }
            finally
            {
                try { mifare.Close(); } catch { }
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Error reading card: {ex.Message}");
        }
#else
        // Non-Android platforms: not supported
        ErrorOccurred?.Invoke("NFC card reading is only supported on Android.");
#endif
    }

    /// <summary>
    /// Prompts the user to enable NFC in device settings (Android).
    /// </summary>
    public async Task PromptEnableNfcAsync()
    {
        if (IsEnabled) return;

#if ANDROID
        try
        {
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionNfcSettings);
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
        }
        catch
        {
            await Application.Current!.Windows[0].Page!
                .DisplayAlert("NFC", "Please enable NFC in your device settings.", "OK");
        }
#else
        await Application.Current!.Windows[0].Page!
            .DisplayAlert("NFC", "NFC is not enabled. Please enable it in your device settings.", "OK");
#endif
    }
}
