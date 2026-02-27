using StEAM_.NET_main.Models;

namespace StEAM_.NET_main.Services;

/// <summary>
/// Shared service for recording late-coming entries.
/// Eliminates duplicated submit logic across FloorStaff, CameraScan, and NfcScan ViewModels.
/// </summary>
public class LateComingService
{
    private readonly SupabaseService _supabaseService;

    public LateComingService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    /// <summary>
    /// Records a late-coming entry for the given student.
    /// Returns a user-facing status message.
    /// Throws on network/service errors — callers should catch and display appropriately.
    /// </summary>
    public async Task<LateComingResult> RecordLateAsync(Student student)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        bool alreadyEntered;
        try
        {
            alreadyEntered = await _supabaseService.CheckAlreadyEnteredAsync(student.RegisterNumber, today);
        }
        catch (Exception ex)
        {
            return new LateComingResult(false, $"Could not verify status. Please retry. ({ex.Message})");
        }

        if (alreadyEntered)
        {
            return new LateComingResult(false, "Student already marked late today!");
        }

        var time = TimeOnly.FromDateTime(DateTime.Now);
        await _supabaseService.InsertLateComingAsync(student.RegisterNumber, today, time);
        return new LateComingResult(true, "Late coming recorded successfully!");
    }
}

/// <summary>
/// Result of a late-coming recording attempt.
/// </summary>
public record LateComingResult(bool Success, string Message);
