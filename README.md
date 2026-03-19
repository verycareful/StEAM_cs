# StEAM -- Student Time and Attendance Manager
[![C#](https://img.shields.io/badge/C%23-12.0-239120?style=flat-square&logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-9.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/maui)
[![Supabase](https://img.shields.io/badge/Supabase-PostgreSQL-3ECF8E?style=flat-square&logo=supabase&logoColor=white)](https://supabase.com/)
[![License: Polyform NC](https://img.shields.io/badge/License-Polyform%20NC%201.0.0-blue.svg)](https://polyformproject.org/licenses/noncommercial/1.0.0/)
[![Status: Archived](https://img.shields.io/badge/Status-Archived-lightgrey?style=flat-square)](.)
A cross-platform .NET MAUI application for tracking student late arrivals on campus. Built with C# and Supabase for authentication, data storage, and real-time operations. Targets Android, iOS, Windows, and macOS from a single codebase.

> **⚠️ Project Status**: This project was **not pursued further due to lack of proper institutional support**. It is now **open-source under the Creative Commons Attribution-NonCommercial 4.0 (CC BY-NC 4.0) License** to benefit the open-source community.
>
> **For Developers**: If you're implementing **NFC + Camera integration in .NET MAUI**, this project contains **proven solutions** to the critical Samsung Android deadlock issue where simultaneous NFC and camera operations would freeze NFC polling for 30+ seconds. See [Release Notes (v1.1.0)](CHANGELOG.md) for details on the fix.
>
> **How to Find This**: Searching for *"NFC and camera not working together"*, *".NET MAUI NFC camera deadlock"*, or similar terms should surface this repository as a solution for anyone facing the same issue.

## Overview

StEAM is used by university floor staff to record students who arrive late and by administrative staff to review, filter, and analyze late-arrival data across departments, courses, batches, and sections. Students are identified by register number entered manually, by tapping their NFC-enabled MIFARE Classic 4K ID card, or by scanning their ID card with the device camera.

**Current Version:** 1.1.2 (2026-03-01) — See [CHANGELOG.md](CHANGELOG.md) for details.

**Key Improvements in v1.1.2:**
- Critical multi-core thread-safety fix for NFC tag reading (`volatile` keyword on `LastNfcTag`)
- Double-entry prevention in NFC Turbo mode using atomic compare-exchange
- Enhanced error handling and user feedback for failed submissions
- Smart typeahead search on FloorStaffPage with live suggestions (register number + name matching)
- Performance optimization: NFC late-count queries now use server-side RPC instead of fetching all rows
- UI improvements: visual distinction of Turbo mode with red styling and active banner
- Observable collection thread-safety improvement in `RecentEntriesService`

See the [CHANGELOG.md](CHANGELOG.md) for the complete list of fixes and improvements across all releases.

## Features

### Authentication
- Email and password login via Supabase Auth
- Role-based access control (floor staff vs. staff)
- Persistent session across cold starts using `SecureStorage` (Android Keystore-backed AES-256) with automatic token refresh on startup
- Custom `IGotrueSessionPersistence` implementation that probes stored tokens before init and forces a `SetSession` exchange if the Supabase client does not self-restore

### Floor Staff — Recording Late Arrivals
- Search students by register number (manual text entry)
- **NFC scanning (MIFARE Classic 4K)**: tap a student ID card to read Sector 0 Blocks 1–2; the register number is ASCII-decoded from the card data and validated with regex `^[A-Z]{2}\d{13}$`
  - Two scan modes: **Regular** (identify then manually submit) and **Turbo** (auto-submit on tap)
- **Camera scanning**: barcode scan (ZXing) or OCR capture (ML Kit) to read the register number from an ID card photo
- **Recent Entries panel**: compact scrollable list of students successfully submitted during the current session, showing name, register number, submission time, and a colour-coded identification method badge (NFC · Barcode · Camera · Manual); cleared on app close or manual tap
- Duplicate entry prevention: checks if the student has already been marked late for the current day

### Staff Dashboard — Viewing and Filtering Records
- Filter late-coming records by date range, department, course, batch, section, and student name
- Two viewing modes:
  - Student view: records grouped by student, showing total late count per student
  - Date view: records grouped by date, showing all late arrivals for each day
- Batch-fetched student details to avoid N+1 query performance issues

### Settings
- Theme selection (light, dark, system default)
- Sign out functionality
- Password change support


## Tech Stack

| Layer          | Technology                                          |
|----------------|-----------------------------------------------------|
| Framework      | .NET 9 MAUI                                         |
| Language       | C# 12                                               |
| UI             | XAML (MAUI controls)                                |
| Architecture   | MVVM (CommunityToolkit.Mvvm 8.4.0)                 |
| Auth           | Supabase Auth (email/password)                      |
| Database       | Supabase PostgreSQL via PostgREST                   |
| SDK            | supabase-csharp 1.1.1                               |
| NFC            | Plugin.MAUI.NFC 0.1.27                              |
| OCR            | Plugin.Maui.OCR 1.1.1 (ML Kit on Android/iOS)      |
| Barcode        | ZXing.Net.Maui.Controls 0.4.0                       |
| UI Toolkit     | CommunityToolkit.Maui 10.0.0                        |
| Config         | DotNetEnv 3.1.1 (environment variable loading)      |
| Fonts          | Inter, Open Sans                                    |


## Architecture

The application follows the MVVM (Model-View-ViewModel) pattern:

```
StEAM-.NET-main/
|
|-- App.xaml / App.xaml.cs              Application entry point and lifecycle
|-- AppShell.xaml / AppShell.xaml.cs     Shell navigation and route registration
|-- MauiProgram.cs                      DI container setup, service and page registration
|-- GlobalXmlns.cs                      Shared XAML namespace declarations
|
|-- Models/
|   |-- Dtos.cs                         Supabase table DTOs (UserDetailsDto, StudentDto,
|   |                                   LateComingDto, DepartmentDto, CourseDto)
|   |-- Student.cs                      Domain record type for student data
|   |-- RecentEntry.cs                  In-session recent submission entry with IdentificationMethod
|   |-- Role.cs                         Role enum and database value mapping
|
|-- ViewModels/
|   |-- LoginViewModel.cs              Login form logic and authentication
|   |-- RoleSelectorViewModel.cs       Post-login role-based navigation
|   |-- FloorStaffViewModel.cs         Manual register number entry and late-arrival recording
|   |-- NfcScanViewModel.cs            NFC tag reading (MIFARE Classic block extraction) and late-arrival recording
|   |-- CameraScanViewModel.cs         Camera-based barcode/OCR scanning for register numbers
|   |-- StaffViewModel.cs              Staff dashboard filters and data retrieval
|   |-- DashboardViewModel.cs          Dashboard summary and statistics
|   |-- SettingsViewModel.cs           Theme management and account operations
|
|-- Pages/
|   |-- LoadingPage.xaml                Splash/loading screen during initialization
|   |-- LoginPage.xaml                  Email and password login form
|   |-- RoleSelectorPage.xaml           Role selection after authentication
|   |-- FloorStaffPage.xaml             Manual student lookup, late-arrival entry, recent entries panel
|   |-- NfcScanPage.xaml                NFC card scanning interface with recent entries panel
|   |-- CameraScanPage.xaml             Camera viewfinder with barcode/OCR overlay
|   |-- StaffPage.xaml                  Filterable staff dashboard with grouped views
|   |-- DashboardPage.xaml              Summary dashboard
|   |-- SettingsPage.xaml               Application settings
|
|-- Services/
|   |-- SupabaseService.cs             Supabase client initialization, auth, and all
|   |                                   database operations (students, late comings, lookups)
|   |-- RecentEntriesService.cs        In-memory singleton tracking submitted entries this session
|   |-- ThemeService.cs                Theme persistence and application
|   |-- NfcService.cs                  NFC adapter management; MIFARE Classic block reading
|   |-- LateComingService.cs           Late-coming business logic layer
|   |-- CustomSessionPersistence.cs    Supabase session save/restore to SecureStorage (Keystore)
|   |-- SnackbarHelper.cs              Toast/snackbar notification utility
|
|-- Converters/
|   |-- ValueConverters.cs             XAML value converters for data binding
|
|-- Resources/
|   |-- AppIcon/                       Application icon assets (SVG + foreground PNG)
|   |-- Splash/                        Splash screen assets
|   |-- Fonts/                         Inter and Open Sans font files
|   |-- Images/                        Image assets
|   |-- Styles/                        XAML style dictionaries and color definitions
|   |-- Raw/                           Raw assets (supabase.env configuration)
|
|-- Platforms/
|   |-- Android/                       Android-specific manifest and configuration
|   |-- iOS/                           iOS-specific Info.plist and configuration
|   |-- MacCatalyst/                   macOS Catalyst configuration
|   |-- Windows/                       Windows-specific configuration
|
|-- Properties/
|   |-- launchSettings.json            Debug launch configuration
```


## Database Schema (Supabase)

| Table          | Primary Key         | Purpose                                                      |
|----------------|---------------------|--------------------------------------------------------------|
| auth.users     | id (UUID)           | Supabase-managed authentication accounts                     |
| user_details   | id (UUID, FK)       | Staff profile: name, staff_id, department, role              |
| students       | register_number     | Student records: name, department, course, specialization, batch, section |
| late_comings   | (register_number, date) | Late arrival entries: date, time, registered_by (staff UUID) |
| departments    | department          | Lookup table for department names                            |
| courses        | course              | Lookup table for course names                                |


## Roles

| Role         | Database Value  | Access                                                          |
|--------------|-----------------|-----------------------------------------------------------------|
| Floor Staff  | floor_staff     | Record student late arrivals via manual entry, NFC, or camera   |
| Staff        | staff           | View and filter late-coming records across all departments      |


## Prerequisites

- .NET 9 SDK
- Visual Studio 2022 (17.8+) with the .NET MAUI workload installed
- For Android: Android SDK API 21+
- For iOS: Xcode 15+ and macOS (required for iOS builds)
- For Windows: Windows 10 (build 17763) or later
- A Supabase project with the tables described above


## Setup

1. Clone the repository:
   ```
   git clone https://github.com/verycareful/StEAM_cs.git
   cd StEAM_cs
   ```

2. Configure Supabase credentials. Create a file at `Resources/Raw/supabase.env` with the following contents:
   ```
   SUPABASE_URL=https://your-project.supabase.co
   SUPABASE_KEY=your-anon-key
   ```

3. Restore NuGet packages:
   ```
   dotnet restore
   ```

4. Build and run:
   ```
   # Android
   dotnet build -t:Run -f net9.0-android

   # Windows
   dotnet build -t:Run -f net9.0-windows10.0.19041.0

   # iOS (requires macOS)
   dotnet build -t:Run -f net9.0-ios

   # macOS Catalyst
   dotnet build -t:Run -f net9.0-maccatalyst
   ```

   Alternatively, open `StEAM-.NET-main.sln` in Visual Studio and select the desired target platform from the toolbar.


## Environment Variables

The application loads Supabase configuration from a `supabase.env` file bundled as a raw asset. This file is read at startup in `MauiProgram.cs` using DotNetEnv. The following variables are required:

| Variable       | Description                          |
|----------------|--------------------------------------|
| SUPABASE_URL   | Your Supabase project URL            |
| SUPABASE_KEY   | Your Supabase anonymous (anon) key   |

Do not commit the `supabase.env` file to version control. It is excluded by the `.gitignore`.


## Troubleshooting & Architecture Notes

### Supabase RPC Setup (v1.1.2+)
To enable the optimized late-count query, create the following RPC function in your Supabase project's SQL editor:

```sql
CREATE OR REPLACE FUNCTION get_late_count(p_register_number character varying)
RETURNS integer
LANGUAGE sql
SECURITY DEFINER
AS $$
  SELECT COUNT(*)::integer
  FROM late_comings
  WHERE register_number = p_register_number;
$$;
```

This function counts late arrivals for a student server-side (no row transfer), replacing the previous client-side approach. If the function does not exist, the client gracefully falls back to returning 0.

### NFC — MIFARE Classic Block Reading
NFC identification reads Sector 0, Blocks 1–2 directly from the student's MIFARE Classic 4K card. The register number is stored on the card as a null-terminated ASCII string. `Plugin.MAUI.NFC`'s `ITagInfo` abstraction does not expose the native Android `Tag` object needed for authenticated MIFARE reads; the workaround is a `static LastNfcTag` property on `MainActivity` populated in `OnNewIntent`, which `NfcService` reads before performing block I/O.

### Camera2 and NFC Hardware Deadlocks
On some Android devices (specifically Samsung), the `CameraService` suppresses NFC polling whenever a `CameraCaptureSession` is active. To prevent the NFC adapter from entering a locked "abandoned buffer" state due to race conditions during page navigation, StEAM uses a custom `MauiCameraViewHandler` (found in `Platforms/Android/`). 

If you modify the ZXing scanner initialization, ensure you do not break the synchronous `StopCameraAsync()` tear-down block invoked in `CameraScanPage.xaml.cs`. This block explicitly waits for the `CAMERA_STATE_CLOSED` broadcast from the Android framework before destroying the `SurfaceView`.

### Cold Start Session Restore
Session data is stored in `SecureStorage` (Android Keystore AES-256) via `CustomSessionPersistence`. On cold start, `SupabaseService.InitializeAsync()` triggers `LoadSession()` through the Gotrue library, but **Gotrue 6.0.3 does not automatically refresh an expired access token or populate `CurrentSession`/`CurrentUser` from the persisted session**. `SupabaseService` works around this by calling `client.Auth.SetSession(accessToken, refreshToken)` explicitly after init when it detects a stored session but a null `CurrentSession`. If the refresh token is expired or revoked, the session is destroyed and the user is redirected to the login screen.

If token-refresh issues occur on cold start on iOS, verify that `CustomSessionPersistence` is not faulting due to OS-level Keychain restrictions (common on un-provisioned simulators).

## License

Copyright © 2026 Sricharan Suresh (github.com/verycareful)

This project is licensed under the **[Polyform Noncommercial License 1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0/)**.
You may use, copy, and modify this software for non-commercial purposes only.
Commercial use of any kind is prohibited without explicit written permission from the author.

See the [LICENSE](LICENSE) file for the full license text, or visit
[https://polyformproject.org/licenses/noncommercial/1.0.0/](https://polyformproject.org/licenses/noncommercial/1.0.0/).

For commercial licensing inquiries, contact [sricharanc03@gmail.com](mailto:sricharanc03@gmail.com).
