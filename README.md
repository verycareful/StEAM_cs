# StEAM -- Student Time and Attendance Manager

A cross-platform .NET MAUI application for tracking student late arrivals on campus. Built with C# and Supabase for authentication, data storage, and real-time operations. Targets Android, iOS, Windows, and macOS from a single codebase.


## Overview

StEAM is used by university floor staff to record students who arrive late and by administrative staff to review, filter, and analyze late-arrival data across departments, courses, batches, and sections. Students are identified by register number entered manually, by tapping their NFC-enabled ID card, or by scanning their ID card with the device camera.


## Features

### Authentication
- Email and password login via Supabase Auth
- Role-based access control (floor staff vs. staff)
- Persistent session management with automatic token refresh
- Custom session persistence using secure local storage

### Floor Staff -- Recording Late Arrivals
- Search students by register number (manual text entry)
- NFC scanning: tap a student ID card to identify the student via their card UID
- Camera scanning: use on-device OCR (text recognition) to read the register number from an ID card
- Card ID assignment: link an NFC card UID to a student record for future tap-to-identify
- Duplicate entry prevention: checks if the student has already been marked late for the current day

### Staff Dashboard -- Viewing and Filtering Records
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
|   |-- Role.cs                         Role enum and database value mapping
|
|-- ViewModels/
|   |-- LoginViewModel.cs              Login form logic and authentication
|   |-- RoleSelectorViewModel.cs       Post-login role-based navigation
|   |-- FloorStaffViewModel.cs         Manual register number entry and late-arrival recording
|   |-- NfcScanViewModel.cs            NFC tag reading, card ID lookup, and card assignment
|   |-- CameraScanViewModel.cs         Camera-based OCR scanning for register numbers
|   |-- StaffViewModel.cs              Staff dashboard filters and data retrieval
|   |-- DashboardViewModel.cs          Dashboard summary and statistics
|   |-- SettingsViewModel.cs           Theme management and account operations
|
|-- Pages/
|   |-- LoadingPage.xaml                Splash/loading screen during initialization
|   |-- LoginPage.xaml                  Email and password login form
|   |-- RoleSelectorPage.xaml           Role selection after authentication
|   |-- FloorStaffPage.xaml             Manual student lookup and late-arrival entry
|   |-- NfcScanPage.xaml                NFC card scanning interface
|   |-- CameraScanPage.xaml             Camera viewfinder with OCR overlay
|   |-- StaffPage.xaml                  Filterable staff dashboard with grouped views
|   |-- DashboardPage.xaml              Summary dashboard
|   |-- SettingsPage.xaml               Application settings
|
|-- Services/
|   |-- SupabaseService.cs             Supabase client initialization, auth, and all
|   |                                   database operations (students, late comings, lookups)
|   |-- ThemeService.cs                Theme persistence and application
|   |-- NfcService.cs                  NFC adapter management and tag reading
|   |-- LateComingService.cs           Late-coming business logic layer
|   |-- CustomSessionPersistence.cs    Supabase session save/restore to local storage
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
| students       | register_number     | Student records: name, department, course, specialization, batch, section, card_id |
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


## Known Issues

- **Session persistence**: Session restore after a cold start may occasionally fail, requiring the user to sign in again. The access token may be expired on cold start, and although the library should auto-refresh via the refresh token, edge cases exist where the refresh silently fails.
- **NFC unavailable after camera use**: After opening the camera scanner (CameraScanPage), the NFC adapter may stop responding to tag reads until the application is fully restarted. This is a platform-level resource conflict between the camera subsystem and the NFC adapter on certain Android devices.


## License

This project is developed for internal use at SRM Institute of Science and Technology.
