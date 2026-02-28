# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-02-28
### Added
- Created an explicit `MauiCameraViewHandler` for Android to interact securely with Camera2 and `ProcessCameraProvider`.
- Added synchronous teardown hooks bound to `CameraScanPage.OnNavigatingFrom()` to safely strip hardware bindings proactively during navigation transitions.

### Changed
- Removed artificial fixed time delays (`Task.Delay(3000)`) in `NfcScanPage` since the hardware conflict is now gracefully resolved.
- Transitioned `NfcScanViewModel` to start NFC polling instantly.

### Fixed
- **Critical**: Resolved a severe race condition on Samsung Android devices where the `ZXingCameraView` destroyed its `SurfaceView` before the underlying `CameraCaptureSession` closed. This previously orphaned the session's `BufferQueue` and caused the Android `CameraService` to suppress NFC polling system-wide for upwards of 30 seconds.

## [1.0.0] - Initial Release
### Added
- Full .NET 9 MAUI cross-platform application.
- Supabase integration for Authentication and PostgreSQL CRUD operations.
- NFC-based ID Card scanning and automated database binding.
- ZXing barcode scanning component for capturing physical code prints.
- On-device ML Kit OCR module to parse text from identification cards.
- Complete Staff and Floor Staff dashboard with query-filtering parameters.
- Built-in theme switching capability mapping to Material Design paradigms.
