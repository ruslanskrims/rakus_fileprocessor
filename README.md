# RAKUS File Processor

.NET console application for processing PDF and HL7 files.

The application has two main functions:
- **PDF processing:** Copies PDF files to the correct folders based on their filenames.
- **HL7 processing:** Reads patient and procedure information from HL7 files and exports it to JSON for BI reporting.

## Project structure

- `Rakus.FileProcessor` — Console application and file processing.
- `Rakus.FileProcessor.Core` — Models, PDF routing and HL7 parsing.
- `Rakus.FileProcessor.Tests` — Unit tests.

## Configuration

Folder paths can be changed in `appsettings.json`.

## How to run

Process PDF files:

```bash
dotnet run --project Rakus.FileProcessor -- --pdf
```

Process HL7 files:

```bash
dotnet run --project Rakus.FileProcessor -- --hl7
```

## Notes

- Source files are not deleted or modified.
- Existing files with identical content are skipped.
- Processing errors are logged without patient identifiers.
- The application can be run periodically using Windows Task Scheduler.