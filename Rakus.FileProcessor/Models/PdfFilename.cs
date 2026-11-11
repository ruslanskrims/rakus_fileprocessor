namespace Rakus.FileProcessor.Models;

public sealed record PdfFileInfo(
    string originalFileName,
    string patientPersonalCode,
    string sourceUniqueId,
    string examinationId,
    string examinationName)
{
    // Read-only property that checks whether the examination name contains "Kolp" or "CRth".
    public bool IsExamNameSpecial =>
        examinationName.Contains("Kolp", StringComparison.OrdinalIgnoreCase) ||
        examinationName.Contains("CRth", StringComparison.OrdinalIgnoreCase);

    public string NameForKolDm =>
        $"{patientPersonalCode}_{sourceUniqueId}_{examinationId}_{examinationName}" +
        Path.GetExtension(originalFileName);
}