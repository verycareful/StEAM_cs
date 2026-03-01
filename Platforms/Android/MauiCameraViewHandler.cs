using System;
using System.Reflection;
using global::Android.Content;
using global::Android.Hardware.Camera2;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using Microsoft.Maui.Handlers;
using ZXing.Net.Maui.Controls;
using ZXing.Net.Maui;
using Exception = System.Exception;

namespace StEAM_.NET_main.Platforms.Android;

public class MauiCameraViewHandler : CameraBarcodeReaderViewHandler
{
    private SemaphoreSlim _closeSemaphore = new SemaphoreSlim(0, 1);
    private CameraAvailabilityCallback _availabilityCallback;
    private global::Android.Hardware.Camera2.CameraManager _androidCameraManager;
    private string _activeCameraId;

    public MauiCameraViewHandler() : base()
    {
    }

    protected override void ConnectHandler(PreviewView nativeView)
    {
        base.ConnectHandler(nativeView);

        // Get the Android API CameraManager so we can monitor when the framework actually closes the camera
        _androidCameraManager = (global::Android.Hardware.Camera2.CameraManager)global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.CameraService);
    }

    protected override void DisconnectHandler(PreviewView nativeView)
    {
        TrySynchronousTeardown();

        base.DisconnectHandler(nativeView);
    }

    /// <summary>
    /// Proactive teardown that can be cleanly invoked before MAUI navigation Detach happens
    /// </summary>
    public async Task StopCameraAsync()
    {
        // 1. Stop the ZXing analysis loop immediately
        if (VirtualView != null)
        {
            VirtualView.IsDetecting = false;
        }

        await Task.Run(() =>
        {
            TrySynchronousTeardown();
        });
    }

    private void TrySynchronousTeardown()
    {
        try
        {
            // Extract the hidden CameraManager and ProcessCameraProvider using reflection
            var cameraManagerField = typeof(CameraBarcodeReaderViewHandler).GetField("cameraManager", BindingFlags.NonPublic | BindingFlags.Instance);
            var zxingCameraManager = cameraManagerField?.GetValue(this);

            if (zxingCameraManager == null) return;

            var providerField = zxingCameraManager.GetType().GetField("cameraProvider", BindingFlags.NonPublic | BindingFlags.Instance);
            var provider = providerField?.GetValue(zxingCameraManager) as ProcessCameraProvider;

            if (provider == null) return;

            // Try to find the active camera ID assuming back camera by default
            _activeCameraId = "0"; // Default back camera ID on most devices
            try
            {
                var cameraField = zxingCameraManager.GetType().GetField("camera", BindingFlags.NonPublic | BindingFlags.Instance);
                var camera = cameraField?.GetValue(zxingCameraManager) as ICamera;
                if (camera != null)
                {
                    // For CameraX, unbinding all use cases triggers:
                    // 2. captureSession.stopRepeating()
                    // 3. captureSession.close()
                    // 5. cameraDevice.close()
                    // We must wait for CameraCaptureSession/CameraDevice to fully close before returning!
                }
            }
            catch { }

            // Reset the semaphore just in case
            if (_closeSemaphore.CurrentCount > 0)
                _closeSemaphore.Wait(0);

            // Register callback to listen for the specific Camera2 hardware close event
            _availabilityCallback = new CameraAvailabilityCallback(_activeCameraId, _closeSemaphore);
            _androidCameraManager?.RegisterAvailabilityCallback(_availabilityCallback, null);

            // Trigger the CameraX teardown (which stops repeating, closes session, and closes device)
            provider.UnbindAll();

            // 4. Wait for the camera to trigger the CLOSED broadcast callback, blocking the UI thread
            //    so that the SurfaceView is NOT destroyed until the session is fully cleaned up.
            //    Timeout at 2000ms to prevent deadlock as requested.
            bool closedInTime = _closeSemaphore.Wait(2000);

            if (!closedInTime)
            {
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
        finally
        {
            if (_availabilityCallback != null)
            {
                _androidCameraManager?.UnregisterAvailabilityCallback(_availabilityCallback);
                _availabilityCallback = null;
            }
        }
    }

    private class CameraAvailabilityCallback : global::Android.Hardware.Camera2.CameraManager.AvailabilityCallback
    {
        private readonly string _targetCameraId;
        private readonly SemaphoreSlim _semaphore;

        public CameraAvailabilityCallback(string targetCameraId, SemaphoreSlim semaphore)
        {
            _targetCameraId = targetCameraId;
            _semaphore = semaphore;
        }

        public override void OnCameraAvailable(string cameraId)
        {
            base.OnCameraAvailable(cameraId);

            if (cameraId == _targetCameraId)
            {
                // The camera framework broadcasted CAMERA_STATE_CLOSED.
                if (_semaphore.CurrentCount == 0)
                {
                    _semaphore.Release();
                }
            }
        }
    }
}
