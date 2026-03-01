using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StEAM_.NET_main.Models;
using StEAM_.NET_main.Services;
using System.Collections.ObjectModel;

namespace StEAM_.NET_main.ViewModels;

public partial class FloorStaffViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService;
    private readonly LateComingService _lateComingService;
    private readonly RecentEntriesService _recentEntriesService;

    public FloorStaffViewModel(SupabaseService supabaseService, LateComingService lateComingService, RecentEntriesService recentEntriesService)
    {
        _supabaseService = supabaseService;
        _lateComingService = lateComingService;
        _recentEntriesService = recentEntriesService;
    }

    // --- Observable Properties ---

    [ObservableProperty] private string _registerNumber = "";
    [ObservableProperty] private Student? _student;
    [ObservableProperty] private long _lateCount;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _popupMessage;
    [ObservableProperty] private bool _hasStudent;

    // Recent entries (shared singleton)
    public System.Collections.ObjectModel.ObservableCollection<Models.RecentEntry> RecentEntries => _recentEntriesService.Entries;
    public bool HasRecentEntries => RecentEntries.Count > 0;

    // Typeahead suggestions
    public ObservableCollection<StudentDto> Suggestions { get; } = [];
    [ObservableProperty] private bool _showSuggestions;
    private CancellationTokenSource? _searchCts;

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
            if (result.Success)
            {
                LateCount++;
                _recentEntriesService.AddEntry(Student, Models.IdentificationMethod.Manual);
                OnPropertyChanged(nameof(HasRecentEntries));
            }
            PopupMessage = result.Message;
        }
        catch (Exception ex) { PopupMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    // --- Typeahead ---

    /// <summary>
    /// Called from code-behind on Entry.TextChanged. Debounces and queries for suggestions.
    /// </summary>
    public async Task OnSearchTextChangedAsync(string text)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Suggestions.Clear();
        ShowSuggestions = false;

        if (text.Length < 2) return;

        try
        {
            await Task.Delay(300, token); // 300ms debounce
            var results = await _supabaseService.SearchStudentsAsync(text);
            if (token.IsCancellationRequested) return;

            foreach (var s in results) Suggestions.Add(s);
            ShowSuggestions = Suggestions.Count > 0;
        }
        catch (TaskCanceledException) { }
    }

    [RelayCommand]
    private void SelectSuggestion(StudentDto dto)
    {
        RegisterNumber = dto.RegisterNumber;
        Student = new Student(dto.RegisterNumber, dto.Name, dto.Course,
            dto.Batch, dto.Department, dto.Specialization, dto.Section);
        Suggestions.Clear();
        ShowSuggestions = false;
        // Also load late count
        _ = LoadLateCountAsync(dto.RegisterNumber);
    }

    private async Task LoadLateCountAsync(string registerNumber)
    {
        try { LateCount = await _supabaseService.GetLateComeCountAsync(registerNumber); }
        catch { /* count is non-critical */ }
    }

    // --- Navigation ---

    [RelayCommand]
    private void ClearRecentEntries()
    {
        _recentEntriesService.Clear();
        OnPropertyChanged(nameof(HasRecentEntries));
    }

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
