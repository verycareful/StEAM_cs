using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StEAM_.NET_main.Models;
using StEAM_.NET_main.Services;
using System.Collections.ObjectModel;

namespace StEAM_.NET_main.ViewModels;

public partial class StaffViewModel : ObservableObject
{
    private readonly SupabaseService _supabaseService;

    public StaffViewModel(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    // --- Filter Properties ---

    [ObservableProperty]
    private DateTime _startDate = DateTime.Now.AddDays(-5);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Now;

    [ObservableProperty]
    private string _filterRegisterNumber = "";

    [ObservableProperty]
    private string _filterName = "";

    [ObservableProperty]
    private string _selectedDepartment = "";

    [ObservableProperty]
    private string _selectedCourse = "";

    [ObservableProperty]
    private string _filterBatch = "";

    [ObservableProperty]
    private string _filterSection = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    // --- Lookups ---
    public ObservableCollection<string> Departments { get; } = [];
    public ObservableCollection<string> Courses { get; } = [];

    // --- Data ---
    private List<(Student Student, DateOnly Date, TimeOnly Time)> _rawData = [];

    // Student-grouped data
    public ObservableCollection<StudentGroupItem> StudentGroupedData { get; } = [];

    // Date-grouped data
    public ObservableCollection<DateGroupItem> DateGroupedData { get; } = [];

    [ObservableProperty]
    private bool _hasData;

    [RelayCommand]
    private async Task LoadLookupsAsync()
    {
        var depts = await _supabaseService.GetDepartmentsAsync();
        var courses = await _supabaseService.GetCoursesAsync();

        Departments.Clear();
        Departments.Add(""); // Empty = All
        foreach (var d in depts) Departments.Add(d);

        Courses.Clear();
        Courses.Add(""); // Empty = All
        foreach (var c in courses) Courses.Add(c);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            int? batch = null;
            if (!string.IsNullOrWhiteSpace(FilterBatch) && int.TryParse(FilterBatch, out var b))
                batch = b;

            _rawData = await _supabaseService.GetDataWithStudentsAsync(
                DateOnly.FromDateTime(StartDate),
                DateOnly.FromDateTime(EndDate),
                FilterRegisterNumber,
                SelectedDepartment,
                SelectedCourse,
                batch,
                FilterSection,
                FilterName
            );

            BuildGroupedData();
            HasData = _rawData.Count > 0;
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

    private void BuildGroupedData()
    {
        // Student grouped
        StudentGroupedData.Clear();
        var byStudent = _rawData.GroupBy(d => d.Student.RegisterNumber);
        foreach (var group in byStudent)
        {
            var first = group.First();
            StudentGroupedData.Add(new StudentGroupItem
            {
                Student = first.Student,
                TimesLate = group.Count(),
                Entries = group.Select(e => new LateEntry(e.Date, e.Time)).ToList()
            });
        }

        // Date grouped
        DateGroupedData.Clear();
        var byDate = _rawData.GroupBy(d => d.Date).OrderBy(g => g.Key);
        foreach (var group in byDate)
        {
            DateGroupedData.Add(new DateGroupItem
            {
                Date = group.Key,
                StudentsLate = group.Count(),
                Entries = group.Select(e => new StudentLateEntry(e.Student, e.Time)).ToList()
            });
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await Shell.Current.GoToAsync("SettingsPage");
    }
}

// View models for grouped data
public class StudentGroupItem
{
    public Student Student { get; set; } = null!;
    public int TimesLate { get; set; }
    public List<LateEntry> Entries { get; set; } = [];
    public bool IsExpanded { get; set; }
}

public record LateEntry(DateOnly Date, TimeOnly Time);

public class DateGroupItem
{
    public DateOnly Date { get; set; }
    public int StudentsLate { get; set; }
    public List<StudentLateEntry> Entries { get; set; } = [];
    public bool IsExpanded { get; set; }
}

public record StudentLateEntry(Student Student, TimeOnly Time);
