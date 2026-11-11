namespace Rakus.FileProcessor.Models;

// PatientId comes from PID; procedure properties come from custom RAKUS PR1
public sealed record Hl7Procedure(
    string PatientId,
    int Sequence,
    string SourceCode,
    string ProcedureCode,
    string ProcedureName,
    DateTime StartedAt,
    DateTime? EndedAt,
    int Quantity);