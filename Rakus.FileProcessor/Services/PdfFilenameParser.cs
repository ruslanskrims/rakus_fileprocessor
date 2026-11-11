using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Rakus.FileProcessor.Models;

namespace Rakus.FileProcessor.Services;

public static partial class PdfFilenameParser
{
    // Capture each meaningful part of the expected RAKUS filename.
    // Example: 111111-12345_010000234ABERG_212765CRth.PDF
    // 'nav' is allowed when a source unique ID is unavailable.
    [GeneratedRegex(
        @"^(?<person>[0-9]{6}-[0-9]{5})_(?<unique>010000234[A-Za-z0-9]*|nav)_(?<examId>[0-9]{6})(?<examName>[^/\\\r\n]+)\.pdf$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FilenamePattern();

    public static bool TryParse(string? fileName,
        [NotNullWhen(true)] out PdfFileInfo? info)
    {
        info = null;

        // A filename must not include directories or be empty.
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.Contains('/') || fileName.Contains('\\'))
            return false;

        Match match = FilenamePattern().Match(fileName);
        if (!match.Success)
            return false;

        info = new PdfFileInfo(
            fileName,
            match.Groups["person"].Value,
            match.Groups["unique"].Value,
            match.Groups["examId"].Value,
            match.Groups["examName"].Value);

        return true;
    }
}