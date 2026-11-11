using Rakus.FileProcessor.Models;

namespace Rakus.FileProcessor.Services;

// Decide the two target folders for each valid PDF filename.
public static class PdfRoutingPlanner
{
    public static IReadOnlyList<PdfDestination> Plan(PdfFileInfo info)
    {
        // The assignment says to check whether the NAME contains Kolp or CRth.
        bool isSpecial = info.OriginalFileName.Contains("Kolp", StringComparison.OrdinalIgnoreCase)
            || info.OriginalFileName.Contains("CRth", StringComparison.OrdinalIgnoreCase);

        if (isSpecial)
        {
            // KOLHIS receives the original name; KOLDM requires an extra underscore.
            string renamed = $"{info.PatientPersonalCode}_{info.SourceUniqueId}_{info.ExaminationId}_" +
                info.ExaminationName + Path.GetExtension(info.OriginalFileName);

            return [
                new PdfDestination(DestinationFolder.KolHis, info.OriginalFileName),
                new PdfDestination(DestinationFolder.KolDm, renamed)
            ];
        }

        // All other examinations are copied to ENDOHIS and ENDODM unchanged.
        return [
            new PdfDestination(DestinationFolder.EndoHis, info.OriginalFileName),
            new PdfDestination(DestinationFolder.EndoDm, info.OriginalFileName)
        ];
    }
}