using System.Text.Json;
using Rakus.FileProcessor.Configuration;
using Rakus.FileProcessor.Services;
using Xunit;

namespace Rakus.FileProcessor.Tests;

public sealed class Hl7Tests
{
    [Fact]
    public void Parser_MapsCustomPr1AndOptionalEndDate()
    {
        // The example has a PID with patient ID in field index 2.
        string[] lines = [
            "PID||P001|000000-00000",
            "PR1|4|VNC|04101^Virsmas anestēzija||20250822091200||1"
        ];

        var rows = Hl7Parser.Parse(lines);
        Assert.Single(rows);
        Assert.Equal("P001", rows[0].PatientId);
        Assert.Equal("04101", rows[0].ProcedureCode);
        Assert.Null(rows[0].EndedAt);
    }

    [Fact]
    public void Processor_ExportsFiveSyntheticPatientsAndSkipsRepeat()
    {
        using var temp = new TemporaryFolder();
        string input = Path.Combine(temp.Path, "Y");
        string output = Path.Combine(temp.Path, "BI");
        Directory.CreateDirectory(input);

        // Generate five synthetic HL7 documents
        for (int i = 1; i <= 5; i++)
        {
            string path = Path.Combine(input, $"patient-{i}.hl7");
            File.WriteAllLines(path, [
                $"PID||P{i:000}|000000-00000",
                $"PR1|1|VNC|04101^Procedure{i}||20250822091200||1"
            ]);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-1));
        }

        var service = new Hl7Processor(new FileProcessingOptions
        {
            Hl7SourceDirectory = input,
            BiOutputDirectory = output
        });

        Assert.Equal(5, service.Process().Exported);
        Assert.Equal(5, service.Process().Skipped);
        Assert.Equal(5, Directory.GetFiles(output, "*.json").Length);

        // Parse the generated JSON to confirm it is a valid array.
        using var doc = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(output, "patient-1.hl7.json")));
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal("P001", doc.RootElement[0].GetProperty("patientId").GetString());
    }
}