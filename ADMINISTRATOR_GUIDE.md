# Administrator Guide

## Configuration

All folder paths are configured in `appsettings.json`.

The application needs read access to source folders and write access to destination folders.

## Running the application

Process PDF files:

```bash
dotnet run --project Rakus.FileProcessor -- --pdf
```

Process HL7 files:

```bash
dotnet run --project Rakus.FileProcessor -- --hl7
```

For automatic processing, the published application can be scheduled using Windows Task Scheduler. Separate tasks can be created for PDF and HL7 processing.

Make sure that the scheduled tasks do not overlap.

## Error handling

- Invalid PDF filenames are skipped.
- Files modified in the last 10 seconds are skipped and can be processed during the next run.
- Existing files with different content are not overwritten.
- Processing errors are logged without patient information.
- Source files are kept for future processing.

If files are not processed, check the folder paths, permissions and application logs.