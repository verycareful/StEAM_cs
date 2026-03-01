using System.Collections.ObjectModel;
using StEAM_.NET_main.Models;

namespace StEAM_.NET_main.Services;

/// <summary>
/// In-memory singleton that tracks recently identified students across all pages.
/// Entries persist until the app is closed or manually cleared.
/// </summary>
public class RecentEntriesService
{
    public ObservableCollection<RecentEntry> Entries { get; } = new();

    /// <summary>
    /// Adds a new entry at the top of the list (newest first).
    /// Dispatches to main thread to ensure ObservableCollection safety.
    /// </summary>
    public void AddEntry(Student student, IdentificationMethod method)
    {
        var entry = new RecentEntry
        {
            Student = student,
            Method = method,
            Timestamp = DateTime.Now
        };
        MainThread.BeginInvokeOnMainThread(() => Entries.Insert(0, entry));
    }

    /// <summary>
    /// Clears all recent entries.
    /// </summary>
    public void Clear() => Entries.Clear();
}
