using System.Security.Cryptography;
using Rakus.FileProcessor.Configuration;
using Rakus.FileProcessor.Models;

namespace Rakus.FileProcessor.Services;

public sealed class PdfProcessor
{
    private readonly FileProcessingOptions _options;
    private static readonly TimeSpan MinimumAge = TimeSpan.FromSeconds(10);

    public PdfProcessor(FileProcessingOptions options) => _options = options;

    // Return how many output copies were created, skipped, or failed.
    public (int Copied, int Skipped, int Failed) Process()
    {
        string sourceFolder = FullPath(_options.SourceDirectory);
        if (!Directory.Exists(sourceFolder))
            throw new DirectoryNotFoundException("Configured PDF input directory was not found.");

        int copied = 0, skipped = 0, failed = 0;

        foreach (string sourceFile in Directory.EnumerateFiles(sourceFolder))
        {
            if (!string.Equals(Path.GetExtension(sourceFile), ".pdf",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            if (!PdfFilenameParser.TryParse(Path.GetFileName(sourceFile), out var info))
            {
                skipped++;
                continue;
            }

            if (DateTime.UtcNow - File.GetLastWriteTimeUtc(sourceFile) < MinimumAge)
            {
                skipped++;
                continue;
            }

            try
            {
                foreach (var destination in PdfRoutingPlanner.Plan(info))
                {
                    string folder = FullPath(GetDestinationPath(destination.Folder));
                    Directory.CreateDirectory(folder);
                    string targetFile = Path.Combine(folder, destination.FileName);

                    if (CopySafely(sourceFile, targetFile)) copied++;
                    else skipped++;
                }
            }
            catch (IOException)
            {
                failed++;
                Console.Error.WriteLine("A PDF could not be delivered; it will be retried next run.");
            }
            catch (UnauthorizedAccessException)
            {
                failed++;
                Console.Error.WriteLine("No permission to process one PDF.");
            }
        }

        return (copied, skipped, failed);
    }

    // Resolve relative paths against the directory containing the application.
    private static string FullPath(string path)
    {
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

        string directory = Path.GetDirectoryName(destinationPath)!;
        string tempPath = Path.Combine(directory, $".copy-{Guid.NewGuid():N}.tmp");

        try
        {
            using (var temp = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                source.CopyTo(temp);
                temp.Flush(flushToDisk: true);
            }

            try { File.Move(tempPath, destinationPath); }
            catch (IOException) when (File.Exists(destinationPath))
            {
                if (SameContent(source, destinationPath)) return false;
                throw;
            }

            return true;
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    // Compare hashes of the files
    private static bool SameContent(FileStream source, string destinationPath)
    {
        using var destination = File.OpenRead(destinationPath);
        if (source.Length != destination.Length) return false;

        source.Position = 0;
        byte[] sourceHash = SHA256.HashData(source);
        byte[] destinationHash = SHA256.HashData(destination);
        return sourceHash.AsSpan().SequenceEqual(destinationHash);
    }
}