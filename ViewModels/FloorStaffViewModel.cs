using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StEAM_.NET_main.Models;
using StEAM_.NET_main.Services;

namespace StEAM_.NET_main.ViewModels;

public partial class FloorStaffViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService;
    private readonly LateComingService _lateComingService;

    public FloorStaffViewModel(SupabaseService supabaseService, LateComingService lateComingService)
    {
        _supabaseService = supabaseService;
        _lateComingService = lateComingService;
    }

    // --- Observable Properties ---

    [ObservableProperty] private string _registerNumber = "";
    [ObservableProperty] private Student? _student;
    [ObservableProperty] private long _lateCount;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _popupMessage;
    [ObservableProperty] private bool _hasStudent;
    [ObservableProperty] private TimeSpan _selectedTime = DateTime.Now.TimeOfDay;

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
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(RegisterNumber))
        { PopupMessage = "Please enter a register number."; return; }

        IsLoading = true;
        try
        {
            var dto = await _supabaseService.GetStudentAsync(RegisterNumber.Trim());
            if (dto != null)
            {
                Student = new Student(dto.RegisterNumber, dto.Name, dto.Course, dto.Batch,
                    dto.Department, dto.Specialization, dto.Section);
                LateCount = await _supabaseService.GetLateComeCountAsync(dto.RegisterNumber);
            }
            else { Student = null; PopupMessage = "Student not found!"; }
        }
        catch (Exception ex) { PopupMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (Student == null) { PopupMessage = "No student selected."; return; }

        IsLoading = true;
        try
        {
            var result = await _lateComingService.RecordLateAsync(Student);
            if (result.Success) LateCount++;
            PopupMessage = result.Message;
        }
        catch (Exception ex) { PopupMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    // --- Navigation ---

    [RelayCommand]
    private async Task OpenCameraScanAsync()
    {
        await Shell.Current.GoToAsync("CameraScanPage");
    }

    [RelayCommand]
    private async Task OpenNfcScanAsync()
    {
        await Shell.Current.GoToAsync("NfcScanPage");
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await Shell.Current.GoToAsync("SettingsPage");
    }
}
