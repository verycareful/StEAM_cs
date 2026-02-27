namespace StEAM_.NET_main.Models;

public enum Role
{
    Staff,
    FloorStaff
}

public static class RoleExtensions
{
    public static string ToDbValue(this Role role) => role switch
    {
        Role.Staff => "staff",
        Role.FloorStaff => "floor_staff",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    public static Role? FromDbValue(string value) => value switch
    {
        "staff" => Role.Staff,
        "floor_staff" => Role.FloorStaff,
        _ => null
    };
}
