
namespace Rakus.FileProcessor.Configuration;

public sealed class FileProcessingOptions
{
    public const string SectionName = "FileProcessing";

    public string SourceDirectory { get; set; } = string.Empty;

    public string EndoHisDirectory { get; set; } = string.Empty;

    public string KolHisDirectory { get; set; } = string.Empty;

    public string EndoDmDirectory { get; set; } = string.Empty;

    public string KolDmDirectory { get; set; } = string.Empty;

    public string Hl7SourceDirectory { get; set; } = string.Empty;

    public string BiOutputDirectory { get; set; } = string.Empty;
}
