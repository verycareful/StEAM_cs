using Plugin.NFC;

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
                System.Diagnostics.Debug.WriteLine($"[NfcService] Started listening on attempt {attempt + 1}");
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NfcService] Start attempt {attempt + 1} failed: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"Error stopping NFC listening: {ex.Message}");
        }

        try
        {
            CrossNFC.Current.OnMessageReceived -= OnNfcMessageReceived;
            CrossNFC.Current.OnTagDiscovered -= OnTagDiscovered;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error unsubscribing NFC events: {ex.Message}");
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

        try
        {
            // Get the UID as hex string (e.g., "49:A2:39:63")
            var identifier = tagInfo.Identifier;
            if (identifier != null && identifier.Length > 0)
            {
                var uid = string.Join(":", identifier.Select(b => b.ToString("X2")));
                TagScanned?.Invoke(uid);
            }
            else
            {
                ErrorOccurred?.Invoke("Could not read tag ID");
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Error reading tag: {ex.Message}");
        }
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
