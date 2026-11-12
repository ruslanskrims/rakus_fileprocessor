using Rakus.FileProcessor.Configuration;

namespace Rakus.FileProcessor.Services;

// Scan the incoming HL7 folder once and create one JSON result for each source
public sealed class Hl7Processor
{
    private readonly FileProcessingOptions _options;
    private static readonly TimeSpan MinimumAge = TimeSpan.FromSeconds(10);

    public Hl7Processor(FileProcessingOptions options) => _options = options;

    public (int Exported, int Skipped, int Failed) Process()
    {
        string inputDirectory = FullPath(_options.Hl7SourceDirectory);
        string outputDirectory = FullPath(_options.BiOutputDirectory);
        if (!Directory.Exists(inputDirectory))
            throw new DirectoryNotFoundException("Configured HL7 input directory was not found.");
        Directory.CreateDirectory(outputDirectory);

        int exported = 0, skipped = 0, failed = 0;
        foreach (string sourceFile in Directory.EnumerateFiles(inputDirectory, "*"))
        {
            if (!string.Equals(Path.GetExtension(sourceFile), ".hl7",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            if (DateTime.UtcNow - File.GetLastWriteTimeUtc(sourceFile) < MinimumAge)
            {
                skipped++;
                continue;
            }

            try
            {
                using var stream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(stream);
                string content = reader.ReadToEnd();

                // Parse the patient and procedures into data-only records.
                var lines = content.Replace("\r", "").Split('\n');
                var rows = Hl7Parser.Parse(lines);

                string target = Path.Combine(outputDirectory,
                    Path.GetFileName(sourceFile) + ".json");
                if (BiJsonExporter.Export(rows, target)) exported++;
                else skipped++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                or FormatException or System.Text.Json.JsonException)
            {
                failed++;
                Console.Error.WriteLine("An HL7 file could not be exported; it will be retried.");
            }
        }

        return (exported, skipped, failed);
    }

    private static string FullPath(string path)
    {
        // Reject missing directory settings instead of silently using the EXE directory.
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("A required directory path is missing.");

        return Path.GetFullPath(Path.IsPathRooted(path)
            ? path : Path.Combine(AppContext.BaseDirectory, path));
    }
}