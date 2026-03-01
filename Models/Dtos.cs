using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StEAM_.NET_main.Models;

[Table("user_details")]
public class UserDetailsDto : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = "";

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("staff_id")]
    public string StaffId { get; set; } = "";

    [Column("department")]
    public string Department { get; set; } = "";

    [Column("role")]
    public string Role { get; set; } = "";

    [Column("created_at")]
    public string? CreatedAt { get; set; }

    [Column("updated_at")]
    public string? UpdatedAt { get; set; }
}

[Table("students")]
public class StudentDto : BaseModel
{
    [PrimaryKey("register_number", false)]
    [Column("register_number")]
    public string RegisterNumber { get; set; } = "";

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("course")]
    public string Course { get; set; } = "";

    [Column("batch")]
    public int Batch { get; set; }

    [Column("department")]
    public string Department { get; set; } = "";

    [Column("specialization")]
    public string Specialization { get; set; } = "";

    [Column("section")]
    public string Section { get; set; } = "";

    [Column("created_at")]
    public string? CreatedAt { get; set; }
}

[Table("late_comings")]
public class LateComingDto : BaseModel
{
    // Composite PK is (register_number, date) — no 'id' column exists
    [PrimaryKey("register_number", false)]
    [Column("register_number")]
    public string RegisterNumber { get; set; } = "";

    [Column("date")]
    public string Date { get; set; } = "";

    [Column("time")]
    public string Time { get; set; } = "";

    [Column("registered_by")]
    public string RegisteredBy { get; set; } = "";

    [Column("created_at")]
    public string? CreatedAt { get; set; }
}

[Table("departments")]
public class DepartmentDto : BaseModel
{
    [PrimaryKey("department", false)]
    [Column("department")]
    public string Department { get; set; } = "";
}

[Table("courses")]
public class CourseDto : BaseModel
{
    [PrimaryKey("course", false)]
    [Column("course")]
    public string Course { get; set; } = "";
}
