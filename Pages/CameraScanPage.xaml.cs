using StEAM_.NET_main.Services;
using StEAM_.NET_main.ViewModels;
using ZXing.Net.Maui;

namespace StEAM_.NET_main.Pages;

public partial class CameraScanPage : ContentPage
{
    private readonly CameraScanViewModel _viewModel;
    private int _isProcessing; // 0 = false, 1 = true — thread-safe via Interlocked

    public CameraScanPage(CameraScanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = false
        };
        BarcodeReader.BarcodesDetected += OnBarcodesDetected;
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        // Thread-safe guard: Interlocked ensures only one thread processes at a time
        if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0) return;

        var first = e.Results?.FirstOrDefault();
        if (first == null || string.IsNullOrWhiteSpace(first.Value))
        {
            Interlocked.Exchange(ref _isProcessing, 0);
            return;
        }

        try
        {
            await _viewModel.OnBarcodeDetected(first.Value);
        }
        finally
        {
            await Task.Delay(3000);
            Interlocked.Exchange(ref _isProcessing, 0);
        }
    }

    private async void OnOcrCaptureClicked(object? sender, EventArgs e)
    {
        try
        {
            // Check camera permission
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await SnackbarHelper.ShowAsync("Camera permission is required.");
                    return;
                }
            }

            _viewModel.IsLoading = true;
            _viewModel.PopupMessage = "Opening camera...";

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo == null)
            {
                _viewModel.PopupMessage = "Capture cancelled.";
                _viewModel.IsLoading = false;
                return;
            }

            _viewModel.PopupMessage = "Processing image...";

            byte[] imageBytes;
            using (var stream = await photo.OpenReadAsync())
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            if (imageBytes.Length == 0)
            {
                _viewModel.PopupMessage = "Image was empty. Try again.";
                _viewModel.IsLoading = false;
                return;
            }

            await _viewModel.ProcessOcrFromBytes(imageBytes);
        }
        catch (Exception ex)
        {
            _viewModel.PopupMessage = $"Error: {ex.Message}";
            _viewModel.IsLoading = false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Re-subscribe to barcode events (safe against double-subscribe)
        BarcodeReader.BarcodesDetected -= OnBarcodesDetected;
        BarcodeReader.BarcodesDetected += OnBarcodesDetected;

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status == PermissionStatus.Granted && _viewModel.IsBarcodeMode)
        {
            BarcodeReader.IsDetecting = true;
        }
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
        
        // Stop detection and unsubscribe early before UI transition
        BarcodeReader.IsDetecting = false;
        BarcodeReader.BarcodesDetected -= OnBarcodesDetected;

        try
        {
#if ANDROID
            if (BarcodeReader.Handler is Platforms.Android.MauiCameraViewHandler customHandler)
            {
                // Force synchronous teardown before allowing navigation to complete
                Task.Run(async () => await customHandler.StopCameraAsync()).Wait();
            }
#endif
        }
        catch (Exception ex)
        {
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Fully release the camera hardware so it doesn't interfere with NFC
        try
        {
            BarcodeReader.Handler?.DisconnectHandler();
        }
        catch (Exception ex)
        {
        }
    }
}
