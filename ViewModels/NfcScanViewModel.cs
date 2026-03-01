using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StEAM_.NET_main.Models;
using StEAM_.NET_main.Services;

namespace StEAM_.NET_main.ViewModels;

public enum NfcMode
{
    Regular,
    Turbo
}

public partial class NfcScanViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService;
    private readonly NfcService _nfcService;
    private readonly LateComingService _lateComingService;
    private readonly RecentEntriesService _recentEntriesService;

    public NfcScanViewModel(SupabaseService supabaseService, NfcService nfcService, LateComingService lateComingService, RecentEntriesService recentEntriesService)
    {
        _supabaseService = supabaseService;
        _nfcService = nfcService;
        _lateComingService = lateComingService;
        _recentEntriesService = recentEntriesService;

        // Don't subscribe in constructor — StartListening/StopListening manages subscriptions
        IsNfcAvailable = _nfcService.IsAvailable;
        IsNfcEnabled = _nfcService.IsEnabled;
    }

    // --- Observable Properties ---

    [ObservableProperty] private NfcMode _currentMode = NfcMode.Regular;
    [ObservableProperty] private string? _popupMessage;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isNfcAvailable;
    [ObservableProperty] private bool _isNfcEnabled;
    [ObservableProperty] private bool _isNfcListening;
    [ObservableProperty] private string _nfcStatusText = "";
    [ObservableProperty] private bool _isRegularMode = true;
    [ObservableProperty] private bool _isTurboMode;

    // Student found via NFC
    [ObservableProperty] private Student? _student;
    [ObservableProperty] private bool _hasStudent;
    [ObservableProperty] private long _lateCount;
    [ObservableProperty] private TimeSpan _selectedTime = DateTime.Now.TimeOfDay;

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
    private void ClearRecentEntries()
    {
        _recentEntriesService.Clear();
        OnPropertyChanged(nameof(HasRecentEntries));
    }

    [RelayCommand]
    private void SetMode(string mode)
    {
        if (!Enum.TryParse<NfcMode>(mode, out var nfcMode)) return;

        CurrentMode = nfcMode;
        IsRegularMode = nfcMode == NfcMode.Regular;
        IsTurboMode = nfcMode == NfcMode.Turbo;
        NfcStatusText = nfcMode switch
        {
            NfcMode.Turbo => "Turbo — Tap card for instant entry",
            _ => "NFC listening... Tap a card"
        };
    }

    [RelayCommand]
    private async Task PromptNfcAsync()
    {
        await _nfcService.PromptEnableNfcAsync();
        IsNfcEnabled = _nfcService.IsEnabled;
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
                _recentEntriesService.AddEntry(Student, Models.IdentificationMethod.NFC);
                OnPropertyChanged(nameof(HasRecentEntries));
            }
            PopupMessage = result.Message;
        }
        catch (Exception ex) { PopupMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    // --- NFC Tag Handlers ---

    private async void OnNfcTagScanned(string registerNumber)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            IsLoading = true;
            try
            {
                switch (CurrentMode)
                {
                    case NfcMode.Turbo: await HandleTurboScan(registerNumber); break;
                    default: await HandleRegularScan(registerNumber); break;
                }
            }
            catch (Exception ex) { PopupMessage = $"Error: {ex.Message}"; }
            finally { IsLoading = false; }
        });
    }

    private async Task HandleRegularScan(string registerNumber)
    {
        var dto = await _supabaseService.GetStudentAsync(registerNumber);
        if (dto == null)
        {
            PopupMessage = "Student not found for this card.";
            return;
        }

        Student = new Student(dto.RegisterNumber, dto.Name, dto.Course, dto.Batch,
            dto.Department, dto.Specialization, dto.Section);
        LateCount = await _supabaseService.GetLateComeCountAsync(dto.RegisterNumber);
        PopupMessage = "Student identified via NFC!";
    }

    private async Task HandleTurboScan(string registerNumber)
    {
        var dto = await _supabaseService.GetStudentAsync(registerNumber);
        if (dto == null)
        {
            PopupMessage = "Student not found for this card.";
            return;
        }

        var student = new Student(dto.RegisterNumber, dto.Name, dto.Course, dto.Batch,
            dto.Department, dto.Specialization, dto.Section);

        var result = await _lateComingService.RecordLateAsync(student);
        if (result.Success)
            _recentEntriesService.AddEntry(student, Models.IdentificationMethod.NFC);
        OnPropertyChanged(nameof(HasRecentEntries));
        PopupMessage = result.Success
            ? $"Late recorded for {dto.Name}!"
            : result.Message;
    }

    private void OnNfcError(string error)
    {
        MainThread.BeginInvokeOnMainThread(() => PopupMessage = error);
    }

    // --- Lifecycle ---

    public async Task StartListeningAsync()
    {
        // Refresh the selected time to current time each time the page appears
        SelectedTime = DateTime.Now.TimeOfDay;

        // Manage NfcService event subscriptions
        _nfcService.TagScanned -= OnNfcTagScanned;
        _nfcService.TagScanned += OnNfcTagScanned;
        _nfcService.ErrorOccurred -= OnNfcError;
        _nfcService.ErrorOccurred += OnNfcError;

        // Refresh NFC hardware state
        IsNfcAvailable = _nfcService.IsAvailable;
        IsNfcEnabled = _nfcService.IsEnabled;

        if (!IsNfcEnabled)
        {
            _ = PromptNfcAsync();
            return;
        }

        // Start listening directly since the camera is synchronously torn down
        _nfcService.StartListening();
        IsNfcListening = _nfcService.IsListening;
        NfcStatusText = CurrentMode switch
        {
            NfcMode.Turbo => "Turbo — Tap card for instant entry",
            _ => "NFC listening... Tap a card"
        };
    }

    public void StopListening()
    {
        _nfcService.StopListening();
        // Unsubscribe so no stale events fire while page is not visible
        _nfcService.TagScanned -= OnNfcTagScanned;
        _nfcService.ErrorOccurred -= OnNfcError;
        IsNfcListening = false;
    }

    public void Cleanup()
    {
        StopListening();
    }
}
