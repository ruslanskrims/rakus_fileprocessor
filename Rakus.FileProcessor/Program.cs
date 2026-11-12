using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rakus.FileProcessor.Configuration;
using Rakus.FileProcessor.Services;

// Accept --pdf or --hl7; running without flags processes both inputs once.
if (args.Any(arg => arg != "--pdf" && arg != "--hl7"))
{
    Console.Error.WriteLine("Usage: Rakus.FileProcessor [--pdf] [--hl7]");
    return 2;
}

bool runPdf = args.Length == 0 || args.Contains("--pdf");
bool runHl7 = args.Length == 0 || args.Contains("--hl7");

// Use built-in Microsoft logging instead of a custom logging framework.
using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddSimpleConsole(options => options.SingleLine = true));
var log = loggerFactory.CreateLogger("RAKUS");

try
{
    // Load external configuration next to the published EXE.
    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
    var options = config.GetRequiredSection(FileProcessingOptions.SectionName)
        .Get<FileProcessingOptions>()
        ?? throw new InvalidOperationException("FileProcessing settings are missing.");

    int failed = 0;
    if (runPdf)
    {
        var result = new PdfProcessor(options,
            loggerFactory.CreateLogger<PdfProcessor>()).Process();
        log.LogInformation("PDF: {Copied} copied, {Skipped} skipped, {Failed} failed.",
            result.Copied, result.Skipped, result.Failed);
        failed += result.Failed;
    }
    if (runHl7)
    {
        var result = new Hl7Processor(options,
            loggerFactory.CreateLogger<Hl7Processor>()).Process();
        log.LogInformation("HL7: {Exported} exported, {Skipped} skipped, {Failed} failed.",
            result.Exported, result.Skipped, result.Failed);
        failed += result.Failed;
    }

    // Exit 1 if any file failed, so Windows Task Scheduler can detect a problem.
    return failed == 0 ? 0 : 1;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
    or InvalidOperationException or System.Text.Json.JsonException or ArgumentException)
{
    log.LogError("Application configuration or filesystem setup failed ({ErrorType}).",
        ex.GetType().Name);
    return 1;
}