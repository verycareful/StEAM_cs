# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## ⚠️ Project Status: No Longer in Active Development

**This project was not pursued further due to lack of proper institutional support.** It is now open-source under the **Creative Commons Attribution-NonCommercial 4.0 (CC BY-NC 4.0) License** to benefit the community.

### Why This Repository is Valuable

If you're building a .NET MAUI application that integrates **NFC scanning** and **camera/barcode scanning** simultaneously, this project demonstrates production-grade solutions to common threading and hardware lifecycle issues:

- **NFC + Camera Race Condition Fix (v1.1.0)**: Samsung Android devices experienced a critical deadlock when `ZXingCameraView` destroyed its `SurfaceView` before the underlying `CameraCaptureSession` closed, which orphaned the session's `BufferQueue` and suppressed NFC polling system-wide for 30+ seconds. This is **solved** in this codebase with synchronous teardown handlers and a custom `MauiCameraViewHandler`.
  
- **Thread-Safety Improvements (v1.1.2)**: Comprehensive fixes for volatile field visibility, atomic operations, and `ObservableCollection` threading on multi-core ARM processors.

Feel free to explore, learn from, and adapt this codebase for your own projects. See the [README](README.md) for full feature documentation.

---

## [1.1.2] - 2026-03-01 - Second Audit Fixes

### Fixed
- **CRITICAL** — Thread-safety issue in `MainActivity.LastNfcTag`: property backed by `volatile` field to ensure multi-core ARM processors read the latest value instead of stale register-cached null.
- **HIGH** — Double-entry race condition in NFC Turbo mode: added `Interlocked.CompareExchange` guard in `NfcScanViewModel.OnNfcTagScanned` to prevent concurrent handling when the same card is tapped rapidly.
- **HIGH** — Unhandled exception in `LateComingService.RecordLateAsync`: `InsertLateComingAsync` now wrapped in try-catch, returning a user-friendly `LateComingResult(false, "Failed to record...")` instead of propagating raw `PostgrestException`.
- **HIGH** — Performance regression in `GetLateComeCountAsync`: now uses Supabase RPC function `get_late_count()` instead of fetching all late-coming rows client-side and counting. Reduces network transfer and scales to large student populations. (**Manual step**: create the Supabase RPC function; SQL provided in audit report section 3.4.)
- **MEDIUM** — ObservableCollection thread-safety in `RecentEntriesService`: `AddEntry` now dispatches collection mutation to `MainThread` via `MainThread.BeginInvokeOnMainThread()` to prevent `NotSupportedException` if called from background threads.
- **MEDIUM** — Removed dead `SelectedTime` property from `FloorStaffViewModel`, `NfcScanViewModel`, and `CameraScanViewModel`. The property was binding to a TimePicker UI but was never passed to `RecordLateAsync`, making the time picker cosmetic. Removed TimePicker from XAML (`NfcScanPage`, `FloorStaffPage`) to eliminate misleading UI.
- **MEDIUM** — Staff dashboard unbounded data loading: `StaffViewModel.SearchAsync()` enforces a 90-day maximum date range; `SupabaseService.GetDataAsync()` adds `.Limit(2000)` as a safety ceiling to cap memory usage on large queries.
- **MEDIUM** — Defensive null check on `MifareClassic.KeyDefault`: if null (rare on some OEM variants), falls back to factory default key bytes `0xFF x 6` instead of passing null to native method.
- **LOW** — Observability gap in `GetDataWithStudentsAsync`: added `Android.Util.Log.E` diagnostic on catch block so silent failures on the staff dashboard can be diagnosed via logcat.
- **LOW** — DI architecture consistency: `LoadingPage` registered as `AddSingleton` instead of `AddTransient` to accurately reflect its lifetime.
- **LOW** — User profile missing state: `SettingsViewModel` added `IsProfileMissing` flag; `SettingsPage` now displays "Profile not configured. Contact your administrator." when the user's `user_details` row is absent.

### Changed
- **UX** — NFC Turbo mode visual distinction: Turbo button background pinned to deep red `#C62828` (no longer theme-overridable); label prefix changed to `[T]` for clarity; added a red warning banner ("TURBO — entries submitted instantly") below the mode toggle when Turbo is active.
- **UX** — Manual search on FloorStaffPage now includes debounced typeahead:
  - New `SearchStudentsAsync(string query)` method in `SupabaseService` queries both register number (prefix match, ILike) and name (substring match, ILike) in parallel, returning up to 8 deduplicated results.
  - `FloorStaffViewModel` wires `OnSearchTextChangedAsync` to the Entry's TextChanged event with 300ms debounce and cancellation token to avoid stale results.
  - New `SelectSuggestion(StudentDto)` relay command auto-populates student details when a suggestion is tapped, bypassing the Search button.
  - `FloorStaffPage.xaml` displays a suggestion overlay below the register number entry (visible only when suggestions exist), showing student name, register number, and department per item.

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

## [1.1.0] - 2026-02-28 - First Audit Fixes

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
