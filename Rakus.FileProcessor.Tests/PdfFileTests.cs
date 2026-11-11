using Rakus.FileProcessor.Configuration;
using Rakus.FileProcessor.Services;
using Xunit;

namespace Rakus.FileProcessor.Tests;

public sealed class PdfFileTests
{
    [Theory]
    [InlineData("111111-12345_010000234ABERG_212765CRth.PDF", true)]
    [InlineData("222222-12345_010000234GFDSA_654321Kolp.PDF", true)]
    [InlineData("333333-12345_010000234RT234_123456GAS.PDF", true)]
    [InlineData("111111-12345_nav_123456Kolp.PDF", true)]
    [InlineData("bad-file.pdf", false)]
    public void TryParse_ValidatesFileNames(string name, bool expected)
    {
        // Test only the parser without creating any actual PDF files.
        Assert.Equal(expected, PdfFilenameParser.TryParse(name, out _));
    }

    [Fact]
    public void Planner_RenamesOnlyKoldmFile()
    {
        Assert.True(PdfFilenameParser.TryParse(
            "111111-12345_010000234ABERG_212765CRth.PDF", out var info));

        var targets = PdfRoutingPlanner.Plan(info);
        Assert.Equal(2, targets.Count);
        Assert.Contains(targets, t => t.Folder == Rakus.FileProcessor.Models.DestinationFolder.KolDm
            && t.FileName.EndsWith("212765_CRth.PDF"));
        Assert.Contains(targets, t => t.Folder == Rakus.FileProcessor.Models.DestinationFolder.KolHis
            && t.FileName.EndsWith("212765CRth.PDF"));
    }

    [Fact]
    public void Processor_HandlesRepeatWithoutOverwriting()
    {
        // Use isolated directories; do not touch any hospital files.
        using var temp = new TemporaryFolder();
        var options = NewOptions(temp.Path);
        Directory.CreateDirectory(options.SourceDirectory);

        string name = "333333-12345_010000234RT234_123456GAS.PDF";
        string input = Path.Combine(options.SourceDirectory, name);
        File.WriteAllText(input, "example PDF bytes");
        File.SetLastWriteTimeUtc(input, DateTime.UtcNow.AddMinutes(-1));

        var processor = new PdfProcessor(options);
        var first = processor.Process();
        var second = processor.Process();

        Assert.Equal(2, first.Copied);
        Assert.Equal(0, second.Copied);
        Assert.Equal(2, second.Skipped);

        // A file with the same name but different contents is an error, not a replace.
        File.WriteAllText(Path.Combine(options.EndoDmDirectory, name), "different");
        Assert.Equal(1, processor.Process().Failed);
    }

    private static FileProcessingOptions NewOptions(string root) => new()
    {
        SourceDirectory = Path.Combine(root, "uzHIS"),
        EndoHisDirectory = Path.Combine(root, "ENDOHIS"),
        KolHisDirectory = Path.Combine(root, "KOLHIS"),
        EndoDmDirectory = Path.Combine(root, "ENDODM"),
        KolDmDirectory = Path.Combine(root, "KOLDM")
    };
}

// Delete test files automatically, even when an assertion fails.
internal sealed class TemporaryFolder : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "rakus-tests-" + Guid.NewGuid().ToString("N"));

    public TemporaryFolder() => Directory.CreateDirectory(Path);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}