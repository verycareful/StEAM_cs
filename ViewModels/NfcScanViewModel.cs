using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StEAM_.NET_main.Models;
using StEAM_.NET_main.Services;

namespace StEAM_.NET_main.ViewModels;

public enum NfcMode
{
    Regular,
    Turbo,
    Register
}

public partial class NfcScanViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService;
    private readonly NfcService _nfcService;
    private readonly LateComingService _lateComingService;

    public NfcScanViewModel(SupabaseService supabaseService, NfcService nfcService, LateComingService lateComingService)
    {
        _supabaseService = supabaseService;
        _nfcService = nfcService;
        _lateComingService = lateComingService;

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
    [ObservableProperty] private bool _isRegisterMode;

    // Student found via NFC
    [ObservableProperty] private Student? _student;
    [ObservableProperty] private bool _hasStudent;
    [ObservableProperty] private long _lateCount;
    [ObservableProperty] private TimeSpan _selectedTime = DateTime.Now.TimeOfDay;

    // Card Registration
    [ObservableProperty] private bool _isCardRegistrationMode;
    [ObservableProperty] private string _pendingCardId = "";
    [ObservableProperty] private string _cardSearchRegNumber = "";
    [ObservableProperty] private Student? _cardSearchStudent;
    [ObservableProperty] private bool _hasCardSearchStudent;

    // --- Computed Properties ---

    public string StudentName => Student?.Name ?? "";
    public string StudentDepartment => Student != null ? $"{Student.Department} — {Student.Specialization}" : "";
    public string StudentCourse => Student != null ? $"Batch {Student.Batch} of {Student.Course}" : "";
    public string StudentSection => Student != null ? $"Section {Student.Section}" : "";
    public string LateCountText => $"Late {LateCount} time(s)";
    public string CardSearchStudentName => CardSearchStudent?.Name ?? "";
    public string CardSearchStudentDept => CardSearchStudent != null
        ? $"{CardSearchStudent.Department} — {CardSearchStudent.Course}" : "";

    partial void OnStudentChanged(Student? value)
    {
        HasStudent = value != null;
        OnPropertyChanged(nameof(StudentName));
        OnPropertyChanged(nameof(StudentDepartment));
        OnPropertyChanged(nameof(StudentCourse));
        OnPropertyChanged(nameof(StudentSection));
    }

    partial void OnLateCountChanged(long value) => OnPropertyChanged(nameof(LateCountText));

    partial void OnCardSearchStudentChanged(Student? value)
    {
        HasCardSearchStudent = value != null;
        OnPropertyChanged(nameof(CardSearchStudentName));
        OnPropertyChanged(nameof(CardSearchStudentDept));
    }

    // --- Commands ---

    [RelayCommand]
    private void SetMode(string mode)
    {
        if (!Enum.TryParse<NfcMode>(mode, out var nfcMode)) return;

        // Clear registration state
        if (CurrentMode == NfcMode.Register && nfcMode != NfcMode.Register)
        {
            IsCardRegistrationMode = false;
            PendingCardId = "";
            CardSearchRegNumber = "";
            CardSearchStudent = null;
        }

        CurrentMode = nfcMode;
        IsRegularMode = nfcMode == NfcMode.Regular;
        IsTurboMode = nfcMode == NfcMode.Turbo;
        IsRegisterMode = nfcMode == NfcMode.Register;
        NfcStatusText = nfcMode switch
        {
            NfcMode.Regular => "NFC listening... Tap a card",
            NfcMode.Turbo => "Turbo — Tap card for instant entry",
            NfcMode.Register => "Register — Tap card to assign",
            _ => ""
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
            if (result.Success) LateCount++;
            PopupMessage = result.Message;
        }
        catch (Exception ex) { PopupMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SearchCardStudentAsync()
    {
        if (string.IsNullOrWhiteSpace(CardSearchRegNumber))
        { PopupMessage = "Please enter a register number."; return; }

        IsLoading = true;
        try
        {
            var dto = await _supabaseService.GetStudentAsync(CardSearchRegNumber.Trim());
            if (dto != null)
            {
                if (!string.IsNullOrEmpty(dto.CardId))
                {
                    PopupMessage = $"{dto.Name} already has a card assigned!";
                    CardSearchStudent = null;
                    return;
                }
                CardSearchStudent = new Student(dto.RegisterNumber, dto.Name, dto.Course, dto.Batch,
                    dto.Department, dto.Specialization, dto.Section);
            }
            else { CardSearchStudent = null; PopupMessage = "Student not found!"; }
        }
        catch (Exception ex) { PopupMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task AssignCardAsync()
    {
        if (CardSearchStudent == null || string.IsNullOrEmpty(PendingCardId))
        { PopupMessage = "No student or card selected."; return; }

        IsLoading = true;
        try
        {
            await _supabaseService.AssignCardIdAsync(CardSearchStudent.RegisterNumber, PendingCardId);
            PopupMessage = $"Card assigned to {CardSearchStudent.Name}!";
            IsCardRegistrationMode = false;
            PendingCardId = "";
            CardSearchRegNumber = "";
            CardSearchStudent = null;
        }
        catch (Exception ex) { PopupMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    // --- NFC Tag Handlers ---

    private async void OnNfcTagScanned(string uid)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            IsLoading = true;
            try
            {
                switch (CurrentMode)
                {
                    case NfcMode.Turbo: await HandleTurboScan(uid); break;
                    case NfcMode.Register: await HandleRegisterScan(uid); break;
                    default: await HandleRegularScan(uid); break;
                }
            }
            catch (Exception ex) { PopupMessage = $"Error: {ex.Message}"; }
            finally { IsLoading = false; }
        });
    }

    private async Task HandleRegularScan(string uid)
    {
        var dto = await _supabaseService.GetStudentByCardIdAsync(uid);
        if (dto == null)
        {
            await HandleRegisterScan(uid);
            // Use SetMode instead of setting properties directly (Issue #13)
            MainThread.BeginInvokeOnMainThread(() => SetMode("Register"));
            return;
        }

        Student = new Student(dto.RegisterNumber, dto.Name, dto.Course, dto.Batch,
            dto.Department, dto.Specialization, dto.Section);
        LateCount = await _supabaseService.GetLateComeCountAsync(dto.RegisterNumber);
        PopupMessage = "Student identified via NFC!";
    }

    private async Task HandleTurboScan(string uid)
    {
        var dto = await _supabaseService.GetStudentByCardIdAsync(uid);
        if (dto == null)
        {
            await HandleRegisterScan(uid);
            // Use SetMode instead of setting properties directly (Issue #13)
            MainThread.BeginInvokeOnMainThread(() => SetMode("Register"));
            return;
        }

        var student = new Student(dto.RegisterNumber, dto.Name, dto.Course, dto.Batch,
            dto.Department, dto.Specialization, dto.Section);

        var result = await _lateComingService.RecordLateAsync(student);
        PopupMessage = result.Success
            ? $"Late recorded for {dto.Name}!"
            : result.Message;
    }

    private async Task HandleRegisterScan(string uid)
    {
        var dto = await _supabaseService.GetStudentByCardIdAsync(uid);
        if (dto != null)
        {
            PopupMessage = $"Card already assigned to {dto.Name} ({dto.RegisterNumber})";
            return;
        }

        PendingCardId = uid;
        CardSearchRegNumber = "";
        CardSearchStudent = null;
        IsCardRegistrationMode = true;
        PopupMessage = "Unregistered card scanned. Search a student to assign.";
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
            NfcMode.Register => "Register — Tap card to assign",
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
