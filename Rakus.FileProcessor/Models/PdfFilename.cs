namespace Rakus.FileProcessor.Models;

public sealed record PdfFileInfo(
    string OriginalFileName,
    string PatientPersonalCode,
    string SourceUniqueId,
    string ExaminationId,
    string ExaminationName)
{
    // Read-only property that checks whether the examination name contains "Kolp" or "CRth".
    public bool IsExamNameSpecial =>
        ExaminationName.Contains("Kolp", StringComparison.OrdinalIgnoreCase) ||
        ExaminationName.Contains("CRth", StringComparison.OrdinalIgnoreCase);

    public string NameForKolDm =>
        $"{PatientPersonalCode}_{SourceUniqueId}_{ExaminationId}_{ExaminationName}" +
        Path.GetExtension(OriginalFileName);
}