using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rakus.FileProcessor.Configuration;
using Rakus.FileProcessor.Models;

namespace Rakus.FileProcessor.Services;

// Enumerate incoming PDFs and copy each to the destinations selected by the planner.
public sealed class PdfProcessor
{
    private readonly FileProcessingOptions _options;
    private readonly ILogger<PdfProcessor> _logger;

    // Files modified very recently may still be in the process of being written.
    // This age check is a simple precaution.
    private static readonly TimeSpan MinimumAge = TimeSpan.FromSeconds(10);

    // The optional logger keeps tests easy to set up.
    public PdfProcessor(FileProcessingOptions options, ILogger<PdfProcessor>? logger = null)
    {
        _options = options;
        _logger = logger ?? NullLogger<PdfProcessor>.Instance;
    }

    // Return how many output copies were created, skipped, or failed.
    public (int Copied, int Skipped, int Failed) Process()
    {
        string sourceFolder = FullPath(_options.SourceDirectory);
        if (!Directory.Exists(sourceFolder))
            throw new DirectoryNotFoundException("Configured PDF input directory was not found.");

        int copied = 0, skipped = 0, failed = 0;

        //Imoprovement:Prevent a disappearing or inaccessible file from interrupting the entire processing run
        foreach (string sourceFile in Directory.EnumerateFiles(sourceFolder))
        {
            if (!string.Equals(
                Path.GetExtension(sourceFile),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                // Validate the filename before processing.
                // Invalid filenames are skipped without throwing exceptions.
                if (!PdfFilenameParser.TryParse(
                    Path.GetFileName(sourceFile), out var info))
                {
                    skipped++;
                    continue;
                }

                // Reduce the risk of processing a file that is still being written.
                // The age check is a precaution, not a guarantee of completeness.
                if (DateTime.UtcNow -
                    File.GetLastWriteTimeUtc(sourceFile) < MinimumAge)
                {
                    skipped++;
                    continue;
                }

                foreach (var destination in PdfRoutingPlanner.Plan(info))
                {
                    string folder = FullPath(
                        GetDestinationPath(destination.Folder));

                    Directory.CreateDirectory(folder);

                    string targetFile = Path.Combine(
                        folder, destination.FileName);

                    if (CopySafely(sourceFile, targetFile))
                        copied++;
                    else
                        skipped++;
                }
            }
            catch (IOException)
            {
                failed++;
                _logger.LogWarning(
                    "PDF processing failed; retry on the next run.");
            }
            catch (UnauthorizedAccessException)
            {
                failed++;
                // Report access problems without including sensitive filenames
                _logger.LogWarning(
                    "Insufficient permissions to process a PDF.");
            }
        }

        return (copied, skipped, failed);
    }

    // Resolve relative paths against the directory containing the application.
    private static string FullPath(string path)
    {
        // Reject missing directory settings instead of silently using the EXE directory.
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("A required directory path is missing.");

        return Path.GetFullPath(Path.IsPathRooted(path)
            ? path : Path.Combine(AppContext.BaseDirectory, path));
    }

    // Connect the enum from the routing planner to the external configuration.
    private string GetDestinationPath(DestinationFolder folder) => folder switch
    {
        DestinationFolder.EndoHis => _options.EndoHisDirectory,
        DestinationFolder.KolHis => _options.KolHisDirectory,
        DestinationFolder.EndoDm => _options.EndoDmDirectory,
        DestinationFolder.KolDm => _options.KolDmDirectory,
        _ => throw new ArgumentOutOfRangeException(nameof(folder))
    };

    // Return false if the destination already contains exactly the same data.
    // Never silently overwrite a different PDF with the same filename.
    private static bool CopySafely(string sourcePath, string destinationPath)
    {
        // FileShare.Read excludes simultaneous writers on file systems honoring .NET sharing.
        using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        if (File.Exists(destinationPath))
        {
            if (SameContent(source, destinationPath)) return false;
            throw new IOException("Destination already exists with different content.");
        }

        // Write to a random temporary name in the TARGET directory.
        // Consumers will not see a partly written destination *.PDF.
        string directory = Path.GetDirectoryName(destinationPath)!;
        string tempPath = Path.Combine(directory, $".copy-{Guid.NewGuid():N}.tmp");

        try
        {
            using (var temp = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                source.CopyTo(temp);
                temp.Flush(flushToDisk: true);
            }

            // Publish the PDF only after the temporary copy is complete.
            // Renaming within the same filesystem is normally atomic,
            // preventing consumers from seeing a partially copied final file.
            // Never overwrite an existing destination.
            try { File.Move(tempPath, destinationPath); }
            catch (IOException) when (File.Exists(destinationPath))
            {
                // Another instance may have delivered the PDF at the same time.
                if (SameContent(source, destinationPath)) return false;
                throw;
            }

            return true;
        }
        // Improvement: Remove temporary files after successful or failed processing.
        // Cleanup failures must not hide the original exception.
        finally
        {
            
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (IOException)
            {
                // Best-effort cleanup; an administrator may need
                // to remove a leftover temporary file.
            }
            catch (UnauthorizedAccessException)
            {
                // Cleanup may fail if filesystem permissions change.
            }
        }
    }

    // Compare file sizes and SHA-256 hashes to detect duplicate content
    // Improvement:Identical files are skipped; conflicting files are never overwritten
    private static bool SameContent(
        FileStream source,
        string destinationPath)
    {
        using var destination = File.OpenRead(destinationPath);

        if (source.Length != destination.Length)
            return false;

        source.Position = 0;

        byte[] sourceHash = SHA256.HashData(source);
        byte[] destinationHash = SHA256.HashData(destination);

        return sourceHash.AsSpan().SequenceEqual(destinationHash);
    }

}