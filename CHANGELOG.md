# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.1] - 2026-03-01

### Added
- **Recent Entries panel** on `FloorStaffPage` and `NfcScanPage`: shows a compact, scrollable list of students whose late-coming records were successfully submitted to the database during the current session.
  - Each entry card displays the student name, register number, submission time, and a colour-coded identification method badge (NFC · Barcode · Camera · Manual).
  - Newest entries appear at the top; the list is cleared when the app is closed or the user taps the **Clear** button.
  - Backed by a new in-memory `RecentEntriesService` singleton registered in `MauiProgram.cs`.
  - New `RecentEntry` model in `Models/` with `IdentificationMethod` enum (NFC, Barcode, Camera, Manual).
- **MIFARE Classic 4K register number extraction** via NFC:
  - `NfcService` now reads Sector 0, Blocks 1–2 of a MIFARE Classic 4K card, decodes the bytes as ASCII, and extracts the register number using regex `^[A-Z]{2}\d{13}$`.
  - `MainActivity` captures the native `MifareClassic` tag object via a `LastNfcTag` static property set in `OnNewIntent`, making the raw tag available to `NfcService` independently of Plugin.MAUI.NFC's `ITagInfo`.
- **Persistent login across cold starts** (session persistence overhaul):
  - `CustomSessionPersistence` now uses `SecureStorage` (Android Keystore-backed AES-256 encryption) instead of `Preferences` for storing the serialised Supabase session JSON.
  - `SupabaseService` probes `SecureStorage` before `InitializeAsync()` to detect a stored session (`HasStoredSession` flag).
  - If `CurrentSession` is null after `InitializeAsync()` despite a stored session existing, `client.Auth.SetSession(accessToken, refreshToken)` is called explicitly to force a token exchange, populating `CurrentUser` before the loading screen resolves.
  - `LoadingPage` simplified: removed polling loop — routing decision is made once after `SignalInitComplete` fires.

### Changed
- **NFC identification is now register-number based**: cards encode the student's register number directly in MIFARE Classic blocks. The previous card-UID-to-student lookup via a `card_id` database column has been removed.
  - `NfcMode.Register` enum value removed from `NfcScanViewModel`.
  - Card registration UI (assign card to student) removed from `NfcScanPage`.
  - `card_id` field removed from `StudentDto` and all related `SupabaseService` methods (`GetStudentByCardIdAsync`, `UpdateStudentCardIdAsync`) removed.
- **Recent entries are recorded only on successful database write**: entries are added to the `RecentEntriesService` exclusively inside the `SubmitAsync` paths (FloorStaffViewModel, NfcScanViewModel, CameraScanViewModel) after `RecordLateAsync` returns `Success = true`. Student identification alone (tap, scan, search) does not create an entry.
- **Camera ViewModel** (`CameraScanViewModel`): identification method (Barcode vs Camera/OCR) is tracked per-lookup and forwarded to `RecentEntriesService` at submit time.
- All `System.Diagnostics.Debug.WriteLine` diagnostic calls removed from production code. Android logcat diagnostics retained via `Android.Util.Log` in `SupabaseService` and `CustomSessionPersistence`.

### Fixed
- Cold-start session restore failure: Supabase C# client (`Gotrue 6.0.3`) calls the `IGotrueSessionPersistence.LoadSession()` handler but does not refresh an expired access token or populate `CurrentSession`/`CurrentUser` from the result. The explicit `SetSession` call after `InitializeAsync()` resolves this, ensuring users with valid refresh tokens are not asked to log in again.

## [1.1.0] - 2026-02-28

### Added
- Created an explicit `MauiCameraViewHandler` for Android to interact securely with Camera2 and `ProcessCameraProvider`.
- Added synchronous teardown hooks bound to `CameraScanPage.OnNavigatingFrom()` to safely strip hardware bindings proactively during navigation transitions.

### Changed
- Removed artificial fixed time delays (`Task.Delay(3000)`) in `NfcScanPage` since the hardware conflict is now gracefully resolved.
- Transitioned `NfcScanViewModel` to start NFC polling instantly.

### Fixed
- **Critical**: Resolved a severe race condition on Samsung Android devices where the `ZXingCameraView` destroyed its `SurfaceView` before the underlying `CameraCaptureSession` closed. This previously orphaned the session's `BufferQueue` and caused the Android `CameraService` to suppress NFC polling system-wide for upwards of 30 seconds.

### Other Changes
- Some small UI Changes
- Code optimisation based on the First Audit results
- Minor bug fixes

## [1.0.0] - Initial Release
### Added
- Full .NET 9 MAUI cross-platform application.
- Supabase integration for Authentication and PostgreSQL CRUD operations.
- NFC-based ID Card scanning and automated database binding.
- ZXing barcode scanning component for capturing physical code prints.
- On-device ML Kit OCR module to parse text from identification cards.
- Complete Staff and Floor Staff dashboard with query-filtering parameters.
- Built-in theme switching capability mapping to Material Design paradigms.
