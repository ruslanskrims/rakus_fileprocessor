
namespace Rakus.FileProcessor.Configuration;

public sealed class FileProcessingOptions
{
    public const string SectionName = "FileProcessing";

    public string SourceDirectory { get; set; } = "";

    public string EndoHisDirectory { get; set; } = "";

    public string KolHisDirectory { get; set; } = "";

    public string EndoDmDirectory { get; set; } = "";

    public string KolDmDirectory { get; set; } = "";

    public string Hl7SourceDirectory { get; set; } = "";

    public string BiOutputDirectory { get; set; } = "";
}
