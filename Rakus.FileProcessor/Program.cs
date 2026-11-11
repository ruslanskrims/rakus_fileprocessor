using Microsoft.Extensions.Configuration;
using Rakus.FileProcessor.Configuration;
using Rakus.FileProcessor.Services;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var options = config.GetRequiredSection(FileProcessingOptions.SectionName)
    .Get<FileProcessingOptions>()
    ?? throw new InvalidOperationException("FileProcessing settings are missing.");

var result = new PdfProcessor(options).Process();
Console.WriteLine($"PDF: {result.Copied} copied, {result.Skipped} skipped, {result.Failed} failed.");

// A non-zero exit code tells a scheduler or monitoring system about a failure.
if (result.Failed > 0) Environment.ExitCode = 1;