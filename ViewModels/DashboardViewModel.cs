using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StEAM_.NET_main.Services;

namespace StEAM_.NET_main.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService;

    public DashboardViewModel(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private int _totalLateToday;
    [ObservableProperty] private string _dateDisplay = "";

    [ObservableProperty]
    private List<DashboardEntry> _lateStudents = [];

    [RelayCommand]
    private async Task LoadTodayDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        HasData = false;
        DateDisplay = DateTime.Today.ToString("dddd, MMMM d, yyyy");

        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var data = await _supabaseService.GetDataWithStudentsAsync(today, today);

            // Sort by the actual TimeOnly value (not string) before formatting
            var entries = data
                .OrderByDescending(d => d.Time)
                .Select(d => new DashboardEntry
                {
                    StudentName = d.Student.Name,
                    RegisterNumber = d.Student.RegisterNumber,
                    Department = d.Student.Department,
                    Time = d.Time.ToString("hh:mm tt")
                })
                .ToList();

            LateStudents = entries;
            TotalLateToday = entries.Count;
            HasData = entries.Count > 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}

public class DashboardEntry
{
    public string StudentName { get; set; } = "";
    public string RegisterNumber { get; set; } = "";
    public string Department { get; set; } = "";
    public string Time { get; set; } = "";
}
