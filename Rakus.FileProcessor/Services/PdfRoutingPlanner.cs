using Rakus.FileProcessor.Models;

namespace Rakus.FileProcessor.Services;

// Decide the two target folders for each valid PDF filename.
public static class PdfRoutingPlanner
{
    public static IReadOnlyList<PdfDestination> Plan(PdfFileInfo info)
    {
        // The assignment says to check whether the NAME contains Kolp or CRth.
        bool isSpecial = info.originalFileName.Contains("Kolp", StringComparison.OrdinalIgnoreCase)
            || info.originalFileName.Contains("CRth", StringComparison.OrdinalIgnoreCase);

        if (isSpecial)
        {
            // KOLHIS receives the original name; KOLDM requires an extra underscore.
            string renamed = $"{info.patientPersonalCode}_{info.sourceUniqueId}_{info.examinationId}_" +
                info.examinationName + Path.GetExtension(info.originalFileName);

            return [
                new PdfDestination(DestinationFolder.KolHis, info.originalFileName),
                new PdfDestination(DestinationFolder.KolDm, renamed)
            ];
        }

        // All other examinations are copied to ENDOHIS and ENDODM unchanged.
        return [
            new PdfDestination(DestinationFolder.EndoHis, info.originalFileName),
            new PdfDestination(DestinationFolder.EndoDm, info.originalFileName)
        ];
    }
}