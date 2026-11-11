namespace Rakus.FileProcessor.Models;

public enum DestinationFolder { EndoHis, KolHis, EndoDm, KolDm }

public sealed record PdfDestination(DestinationFolder Folder, string FileName);