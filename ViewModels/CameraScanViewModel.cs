using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.OCR;
using StEAM_.NET_main.Models;
using StEAM_.NET_main.Services;

namespace StEAM_.NET_main.ViewModels;

public enum CameraMode
{
    Barcode,
    OCR
}

public partial class CameraScanViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService;
    private readonly IOcrService _ocrService;
    private readonly LateComingService _lateComingService;
    private readonly RecentEntriesService _recentEntriesService;

    public CameraScanViewModel(SupabaseService supabaseService, IOcrService ocrService, LateComingService lateComingService, RecentEntriesService recentEntriesService)
    {
        _supabaseService = supabaseService;
        _ocrService = ocrService;
        _lateComingService = lateComingService;
        _recentEntriesService = recentEntriesService;
    }

    // --- Observable Properties ---

    [ObservableProperty] private CameraMode _currentMode = CameraMode.Barcode;
    [ObservableProperty] private string? _popupMessage;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isBarcodeMode = true;
    [ObservableProperty] private bool _isOcrMode;

    // Student found
    [ObservableProperty] private string _registerNumber = "";
    [ObservableProperty] private Student? _student;
    [ObservableProperty] private bool _hasStudent;
    [ObservableProperty] private long _lateCount;

    // Track which method identified this student
    private Models.IdentificationMethod _studentIdentificationMethod = Models.IdentificationMethod.Barcode;

    // Recent entries (shared singleton)
    public System.Collections.ObjectModel.ObservableCollection<Models.RecentEntry> RecentEntries => _recentEntriesService.Entries;
    public bool HasRecentEntries => RecentEntries.Count > 0;

    // --- Computed Properties ---

    public string StudentName => Student?.Name ?? "";
    public string StudentDepartment => Student != null ? $"{Student.Department} — {Student.Specialization}" : "";
    public string StudentCourse => Student != null ? $"Batch {Student.Batch} of {Student.Course}" : "";
    public string StudentSection => Student != null ? $"Section {Student.Section}" : "";
    public string LateCountText => $"Late {LateCount} time(s)";

    partial void OnStudentChanged(Student? value)
    {
        HasStudent = value != null;
        OnPropertyChanged(nameof(StudentName));
        OnPropertyChanged(nameof(StudentDepartment));
        OnPropertyChanged(nameof(StudentCourse));
        OnPropertyChanged(nameof(StudentSection));
    }

    partial void OnLateCountChanged(long value) => OnPropertyChanged(nameof(LateCountText));

    // --- Commands ---

    [RelayCommand]
    private void SetMode(string mode)
    {
        if (!Enum.TryParse<CameraMode>(mode, out var camMode)) return;
        CurrentMode = camMode;
        IsBarcodeMode = camMode == CameraMode.Barcode;
        IsOcrMode = camMode == CameraMode.OCR;
    }

    /// <summary>
    /// Called when the barcode scanner detects a value.
    /// </summary>
    public async Task OnBarcodeDetected(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            RegisterNumber = value.Trim().ToUpper();
            PopupMessage = $"Barcode: {RegisterNumber}";
            await SearchAndLoadStudentAsync(RegisterNumber, Models.IdentificationMethod.Barcode);
        });
    }

    /// <summary>
    /// Process OCR from image bytes captured by the in-app camera.
    /// Called from the code-behind after Camera.MAUI TakePhotoAsync.
    /// </summary>
    public async Task ProcessOcrFromBytes(byte[] imageBytes)
    {
        try
        {
            IsLoading = true;
            PopupMessage = "Reading text from ID card...";

            var ocrResult = await _ocrService.RecognizeTextAsync(imageBytes);

            if (ocrResult == null || !ocrResult.Success || string.IsNullOrWhiteSpace(ocrResult.AllText))
            {
                PopupMessage = "Could not read text from image. Try again.";
                return;
            }

            // Try to extract register number pattern (e.g. RA2211003010001)
            var regNumMatch = Regex.Match(ocrResult.AllText,
                @"\b(RA\d{10,})\b", RegexOptions.IgnoreCase);

            if (!regNumMatch.Success)
            {
                // More general: 2+ letters followed by 8+ digits
                regNumMatch = Regex.Match(ocrResult.AllText,
                    @"\b([A-Z]{2,}\d{8,})\b", RegexOptions.IgnoreCase);
            }

            if (regNumMatch.Success)
            {
                RegisterNumber = regNumMatch.Value.ToUpper();
                PopupMessage = $"Detected: {RegisterNumber}";
                await SearchAndLoadStudentAsync(RegisterNumber, Models.IdentificationMethod.Camera);
            }
            else
            {
                PopupMessage = "No register number found in text.";
            }
        }
        catch (Exception ex)
        {
            PopupMessage = $"OCR Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (Student == null) { PopupMessage = "No student selected."; return; }

        IsLoading = true;
        try
        {
            var result = await _lateComingService.RecordLateAsync(Student);
            if (result.Success)
            {
                LateCount++;
                _recentEntriesService.AddEntry(Student, _studentIdentificationMethod);
                OnPropertyChanged(nameof(HasRecentEntries));
            }
            PopupMessage = result.Message;
        }
        catch (Exception ex) { PopupMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    // --- Helpers ---

    private async Task SearchAndLoadStudentAsync(string regNum, Models.IdentificationMethod method = Models.IdentificationMethod.Barcode)
    {
        _studentIdentificationMethod = method;
        IsLoading = true;
        try
        {
            var dto = await _supabaseService.GetStudentAsync(regNum);
            if (dto != null)
            {
                Student = new Student(dto.RegisterNumber, dto.Name, dto.Course, dto.Batch,
                    dto.Department, dto.Specialization, dto.Section);
                LateCount = await _supabaseService.GetLateComeCountAsync(dto.RegisterNumber);
            }
            else
            {
                Student = null;
                PopupMessage = "Student not found!";
            }
        }
        catch (Exception ex) { PopupMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }
}
