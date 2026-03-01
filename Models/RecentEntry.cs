namespace StEAM_.NET_main.Models;

public enum IdentificationMethod
{
    NFC,
    Barcode,
    Camera,
    Manual
}

public class RecentEntry
{
    public required Student Student { get; init; }
    public required IdentificationMethod Method { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;

    // Display helpers for XAML binding
    public string StudentName => Student.Name;
    public string RegisterNumber => Student.RegisterNumber;
    public string Department => $"{Student.Department} — {Student.Specialization}";
    public string TimeText => Timestamp.ToString("hh:mm tt");

    public string MethodLabel => Method switch
    {
        IdentificationMethod.NFC => "NFC",
        IdentificationMethod.Barcode => "Barcode",
        IdentificationMethod.Camera => "Camera",
        IdentificationMethod.Manual => "Manual",
        _ => "Unknown"
    };

    public Color MethodColor => Method switch
    {
        IdentificationMethod.NFC => Color.FromArgb("#1565C0"),
        IdentificationMethod.Barcode => Color.FromArgb("#2E7D32"),
        IdentificationMethod.Camera => Color.FromArgb("#E65100"),
        IdentificationMethod.Manual => Color.FromArgb("#6A1B9A"),
        _ => Colors.Grey
    };
}
