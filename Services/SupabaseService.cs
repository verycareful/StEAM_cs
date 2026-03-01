using Supabase;
using StEAM_.NET_main.Models;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;
#if ANDROID
using Android.Util;
#endif

namespace StEAM_.NET_main.Services;

public class SupabaseService
{
    private volatile Supabase.Client? _client;
    private readonly TaskCompletionSource _initTcs = new();
    private readonly object _lock = new();
    private Exception? _initException;

    public Supabase.Client Client => _client
        ?? throw new InvalidOperationException("Supabase not initialized. Call InitializeAsync first.");

    public bool IsInitialized => _client != null;

    /// <summary>Await this to wait until Supabase is ready (with 15s timeout).</summary>
    public async Task WaitForInitializationAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var tcs = _initTcs.Task;
        var completed = await Task.WhenAny(tcs, Task.Delay(Timeout.Infinite, cts.Token));
        if (completed != tcs)
            throw new TimeoutException("Supabase initialization timed out after 15 seconds.");

        if (_initException != null)
            throw new InvalidOperationException("Supabase failed to initialize.", _initException);
    }

    /// <summary>Signal that initialization attempt is complete (success or failure).</summary>
    public void SignalInitComplete(Exception? ex = null)
    {
        _initException = ex;
        _initTcs.TrySetResult();
    }

    public async Task InitializeAsync()
    {
        var url = Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? throw new InvalidOperationException("SUPABASE_URL environment variable is not set.");
        var key = Environment.GetEnvironmentVariable("SUPABASE_KEY")
            ?? throw new InvalidOperationException("SUPABASE_KEY environment variable is not set.");

        var sessionHandler = new CustomSessionPersistence();
        var options = new SupabaseOptions 
        { 
            AutoRefreshToken = true,
            SessionHandler = sessionHandler
        };
        var client = new Supabase.Client(url, key, options);
        
        // Probe for a stored session BEFORE full initialization
        var probeSession = sessionHandler.LoadSession();
        HasStoredSession = probeSession != null && !string.IsNullOrEmpty(probeSession.RefreshToken);
        
        await client.InitializeAsync();

        // If the library didn't restore CurrentSession from storage (common with expired access tokens),
        // manually inject the session and force a refresh so CurrentUser is populated before we signal completion.
        if (HasStoredSession && client.Auth.CurrentSession == null && probeSession?.RefreshToken != null)
        {
#if ANDROID
            Log.Info("StEAM.Session", "CurrentSession null after init, calling SetSession to force token refresh...");
#endif
            try
            {
                // SetSession detects expired access tokens and calls RefreshSession internally
                await client.Auth.SetSession(probeSession.AccessToken ?? "", probeSession.RefreshToken);
#if ANDROID
                Log.Info("StEAM.Session", $"SetSession succeeded, CurrentUser: {client.Auth.CurrentUser?.Email}");
#endif
            }
            catch (Exception ex)
            {
                // Refresh token expired or revoked — clear it so the user is prompted to log in
#if ANDROID
                Log.Error("StEAM.Session", $"SetSession failed: {ex.GetType().Name}: {ex.Message}");
#endif
                sessionHandler.DestroySession();
                HasStoredSession = false;
            }
        }

        // Thread-safe assignment
        lock (_lock) { _client = client; }

        // Auto-save session on any auth state change (sign-in, token refresh, etc.)
        client.Auth.AddStateChangedListener((_, state) =>
        {
        });
    }

    /// <summary>True if a session with a refresh token was loaded from SecureStorage on this cold start.</summary>
    public bool HasStoredSession { get; private set; }

    public bool HasValidSession()
    {
        var session = _client?.Auth.CurrentSession;
        if (session == null) return false;
        return !string.IsNullOrEmpty(session.RefreshToken);
    }

    /// <summary>
    /// Clears all saved session data from the single persistence store.
    /// </summary>
    public void ClearSession()
    {
        var persistence = new CustomSessionPersistence();
        persistence.DestroySession();
    }

    // --- Auth ---

    public async Task SignInAsync(string email, string password)
    {
        await Client.Auth.SignIn(email, password);
        // Session is auto-saved by CustomSessionPersistence via the library
    }

    public async Task SignOutAsync()
    {
        // Clear persisted session first so even if network call fails, local session is gone
        ClearSession();

        try
        {
            await Client.Auth.SignOut();
        }
        catch (Exception ex)
        {
        }
    }

    public string? GetCurrentUserId()
    {
        return Client.Auth.CurrentUser?.Id;
    }

    // --- User Details ---

    public async Task<UserDetailsDto?> GetUserDetailsAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return null;

        try
        {
            var response = await Client.From<UserDetailsDto>()
                .Filter("id", Operator.Equals, userId)
                .Single();
            return response;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    // --- Students ---

    public async Task<StudentDto?> GetStudentAsync(string registerNumber)
    {
        try
        {
            var response = await Client.From<StudentDto>()
                .Filter("register_number", Operator.Equals, registerNumber)
                .Single();
            return response;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    /// <summary>
    /// Searches students by register number prefix or name substring for typeahead.
    /// Returns up to <paramref name="limit"/> distinct results.
    /// </summary>
    public async Task<List<StudentDto>> SearchStudentsAsync(string query, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return [];
        try
        {
            var byRegNum = Client.From<StudentDto>()
                .Filter("register_number", Operator.ILike, $"{query}%")
                .Limit(limit);
            var byName = Client.From<StudentDto>()
                .Filter("name", Operator.ILike, $"%{query}%")
                .Limit(limit);

            var r1 = await byRegNum.Get();
            var r2 = await byName.Get();

            return r1.Models
                .Concat(r2.Models)
                .DistinctBy(s => s.RegisterNumber)
                .Take(limit)
                .ToList();
        }
        catch { return []; }
    }

    /// <summary>
    /// Fetches multiple students by register numbers in a single query (avoids N+1).
    /// </summary>
    public async Task<List<StudentDto>> GetStudentsByRegNumbersAsync(IEnumerable<string> registerNumbers)
    {
        try
        {
            var regNos = registerNumbers.ToList();
            if (regNos.Count == 0) return [];

            // Use In filter to batch-fetch all students in one query
            var response = await Client.From<StudentDto>()
                .Filter("register_number", Operator.In, regNos)
                .Get();
            return response.Models;
        }
        catch (Exception ex)
        {
            return [];
        }
    }

    // --- Late Comings ---

    public async Task<long> GetLateComeCountAsync(string registerNumber)
    {
        try
        {
            var response = await Client.Rpc("get_late_count",
                new Dictionary<string, object> { { "p_register_number", registerNumber } });
            return long.TryParse(response.Content?.Trim('"'), out var count) ? count : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Checks if a student has already been marked late today.
    /// Throws on error so the caller can distinguish network failure from "already entered".
    /// </summary>
    public async Task<bool> CheckAlreadyEnteredAsync(string registerNumber, DateOnly date)
    {
        try
        {
            var response = await Client.From<LateComingDto>()
                .Filter("register_number", Operator.Equals, registerNumber)
                .Filter("date", Operator.Equals, date.ToString("yyyy-MM-dd"))
                .Get();
            return response.Models.Count > 0;
        }
        catch (Exception ex)
        {
            throw; // Let caller decide how to handle — do not swallow as "already entered"
        }
    }

    public async Task InsertLateComingAsync(string registerNumber, DateOnly date, TimeOnly time)
    {
        var userId = GetCurrentUserId()
            ?? throw new InvalidOperationException("User not authenticated");

        var dto = new LateComingDto
        {
            RegisterNumber = registerNumber,
            Date = date.ToString("yyyy-MM-dd"),
            Time = time.ToString("HH:mm:ss"),
            RegisteredBy = userId
        };

        await Client.From<LateComingDto>().Insert(dto);
    }

    // --- Lookups ---

    public async Task<List<string>> GetDepartmentsAsync()
    {
        try
        {
            var response = await Client.From<DepartmentDto>().Get();
            return response.Models.Select(d => d.Department).ToList();
        }
        catch (Exception ex)
        {
            return [];
        }
    }

    public async Task<List<string>> GetCoursesAsync()
    {
        try
        {
            var response = await Client.From<CourseDto>().Get();
            return response.Models.Select(c => c.Course).ToList();
        }
        catch (Exception ex)
        {
            return [];
        }
    }

    // --- Filtered Data (Staff Dashboard) ---

    public async Task<List<LateComingDto>> GetDataAsync(
        DateOnly startDate,
        DateOnly endDate,
        string registerNumber = "",
        string department = "",
        string course = "",
        int? batch = null,
        string section = "",
        string name = "")
    {
        try
        {
            var query = Client.From<LateComingDto>()
                .Filter("date", Operator.GreaterThanOrEqual, startDate.ToString("yyyy-MM-dd"))
                .Filter("date", Operator.LessThanOrEqual, endDate.ToString("yyyy-MM-dd"));

            if (!string.IsNullOrEmpty(registerNumber))
                query = query.Filter("register_number", Operator.ILike, $"%{registerNumber}%");

            // Note: department, course, batch, section, name filters are applied
            // client-side in GetDataWithStudentsAsync because supabase-csharp
            // doesn't support cross-table filtering without joins/views.

            var response = await query
                .Order("register_number", Ordering.Ascending)
                .Order("date", Ordering.Ascending)
                .Limit(2000)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            return [];
        }
    }

    /// <summary>
    /// Gets filtered late-coming data with student details for the staff dashboard.
    /// Uses batch-fetch for students to avoid N+1 query problem.
    /// </summary>
    public async Task<List<(Student Student, DateOnly Date, TimeOnly Time)>> GetDataWithStudentsAsync(
        DateOnly startDate,
        DateOnly endDate,
        string registerNumber = "",
        string department = "",
        string course = "",
        int? batch = null,
        string section = "",
        string name = "")
    {
        try
        {
            var lateComings = await GetDataAsync(startDate, endDate, registerNumber, department, course, batch, section, name);
            var result = new List<(Student, DateOnly, TimeOnly)>();

            // Batch-fetch all students in one query instead of N+1
            var regNos = lateComings.Select(l => l.RegisterNumber).Distinct().ToList();
            var studentDtos = await GetStudentsByRegNumbersAsync(regNos);
            var studentMap = studentDtos.ToDictionary(s => s.RegisterNumber);

            // Apply client-side filters and build result
            var filteredStudents = new Dictionary<string, StudentDto>();
            foreach (var kvp in studentMap)
            {
                var student = kvp.Value;

                if (!string.IsNullOrEmpty(department) &&
                    !student.Department.Equals(department, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(course) &&
                    !student.Course.Equals(course, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (batch.HasValue && student.Batch != batch.Value)
                    continue;
                if (!string.IsNullOrEmpty(section) &&
                    !student.Section.Equals(section, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(name) &&
                    !student.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                    continue;

                filteredStudents[kvp.Key] = student;
            }

            foreach (var lc in lateComings)
            {
                if (filteredStudents.TryGetValue(lc.RegisterNumber, out var studentDto))
                {
                    var student = new Student(
                        studentDto.RegisterNumber,
                        studentDto.Name,
                        studentDto.Course,
                        studentDto.Batch,
                        studentDto.Department,
                        studentDto.Specialization,
                        studentDto.Section
                    );

                    result.Add((
                        student,
                        DateOnly.Parse(lc.Date),
                        TimeOnly.Parse(lc.Time)
                    ));
                }
            }

            return result;
        }
        catch (Exception ex)
        {
#if ANDROID
            Log.Error("SupabaseService", $"GetDataWithStudentsAsync failed: {ex.Message}");
#endif
            return [];
        }
    }
}
