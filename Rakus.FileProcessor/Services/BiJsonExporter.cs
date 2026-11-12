using System.Text;
using System.Text.Json;
using Rakus.FileProcessor.Models;

namespace Rakus.FileProcessor.Services;

// One JSON array per HL7 file: each element is one procedure/BI row.
public static class BiJsonExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    // Return false if identical JSON already exists; never replace different data.
    public static bool Export(IReadOnlyList<Hl7Procedure> rows, string destinationPath)
    {
        string json = JsonSerializer.Serialize(rows, JsonOptions);
        string folder = Path.GetDirectoryName(destinationPath)
            ?? throw new ArgumentException("An output directory is required.", nameof(destinationPath));
        Directory.CreateDirectory(folder);

        if (File.Exists(destinationPath))
        {
            if (File.ReadAllText(destinationPath) == json) return false;
            throw new IOException("BI output already exists with different content.");
        }

        // Write to a temporary file first, so BI readers only see complete JSON.
        string tempPath = Path.Combine(folder, $".export-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            try { File.Move(tempPath, destinationPath); }
            catch (IOException) when (File.Exists(destinationPath))
            {
                // Another scheduled run might have created the same output.
                if (File.ReadAllText(destinationPath) == json) return false;
                throw;
            }
            return true;
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}