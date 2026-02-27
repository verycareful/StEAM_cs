namespace StEAM_.NET_main.Models;

public record Student(
    string RegisterNumber,
    string Name,
    string Course,
    int Batch,
    string Department,
    string Specialization,
    string Section
);
