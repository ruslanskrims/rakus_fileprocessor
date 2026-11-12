
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rakus.FileProcessor.Configuration;

namespace Rakus.FileProcessor.Services;

// Reads HL7 files and exports their procedures as BI JSON files.
// The actual HL7 parsing logic stays in the Core library.
public sealed class Hl7Processor
{
    private readonly FileProcessingOptions _options;
    private readonly ILogger<Hl7Processor> _logger;

    // Recently modified files might still be receiving data.
    private static readonly TimeSpan MinimumAge =
        TimeSpan.FromSeconds(10);

    public Hl7Processor(
        FileProcessingOptions options,
        ILogger<Hl7Processor>? logger = null)
    {
        _options = options;
        _logger = logger ?? NullLogger<Hl7Processor>.Instance;
    }

    // Process all HL7 files from the configured directory.
    // Return the number of exported, skipped, and failed files.
    public (int Exported, int Skipped, int Failed) Process()
    {
        string inputDirectory =
            FullPath(_options.Hl7SourceDirectory);

        string outputDirectory =
            FullPath(_options.BiOutputDirectory);

        if (!Directory.Exists(inputDirectory))
            throw new DirectoryNotFoundException(
                "Configured HL7 input directory was not found.");

        Directory.CreateDirectory(outputDirectory);

        int exported = 0, skipped = 0, failed = 0;

        foreach (string sourceFile in
            Directory.EnumerateFiles(inputDirectory))
        {
            // Only process files with the .hl7 extension.
            if (!string.Equals(
                Path.GetExtension(sourceFile),
                ".hl7",
                StringComparison.OrdinalIgnoreCase))
                continue;

            // File operations must be inside try.
            // Files can disappear while the directory is being scanned.
            try
            {
                DateTime lastWriteTime =
                    File.GetLastWriteTimeUtc(sourceFile);

                // Skip files that were modified too recently.
                if (DateTime.UtcNow - lastWriteTime < MinimumAge)
                {
                    skipped++;
                    continue;
                }

                // Open the source with restricted sharing.
                using var stream = new FileStream(
                    sourceFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                // Save metadata before reading.
                long originalLength = stream.Length;

                DateTime originalLastWrite =
                    File.GetLastWriteTimeUtc(sourceFile);

                using var reader = new StreamReader(stream);

                // Read the HL7 message as text.
                string content = reader.ReadToEnd();

                // Detect changes that occurred while reading.
                if (stream.Length != originalLength ||
                    File.GetLastWriteTimeUtc(sourceFile) != originalLastWrite)
                {
                    throw new IOException(
                        "Source HL7 file changed during reading.");
                }

                // Normalize all common line-ending formats.
                //
                // Windows: \r\n
                // HL7 v2:  \r
                // Unix:    \n
                //
                // Convert them to a single format.
                string normalized = content
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n');

                // Split the HL7 message into segments.
                string[] lines = normalized.Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries);

                // Use the parser from our Core library.
                var procedures = Hl7Parser.Parse(lines);

                // Generate a stable output filename.
                // Reprocessing the same source will use the same target.
                string targetFile = Path.Combine(
                    outputDirectory,
                    Path.GetFileName(sourceFile) + ".json");

                // Export JSON using the existing exporter.
                // Identical output is skipped rather than duplicated.
                if (BiJsonExporter.Export(procedures, targetFile))
                    exported++;
                else
                    skipped++;
            }
            catch (Exception ex) when (
                ex is IOException
                or UnauthorizedAccessException
                or FormatException
                or System.Text.Json.JsonException
                or ArgumentException)
            {
                failed++;

                // Log the error category without patient information.
                _logger.LogWarning(
                    "HL7 file processing failed ({ErrorType}); retry later.",
                    ex.GetType().Name);
            }
        }

        return (exported, skipped, failed);
    }

    // Resolve configured paths relative to the executable.
    private static string FullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException(
                "A required directory path is missing.");

        return Path.GetFullPath(
            Path.IsPathRooted(path)
                ? path
                : Path.Combine(AppContext.BaseDirectory, path));
    }
}
