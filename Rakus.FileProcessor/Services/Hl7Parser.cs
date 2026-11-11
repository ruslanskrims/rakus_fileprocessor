using System.Globalization;
using Rakus.FileProcessor.Models;

namespace Rakus.FileProcessor.Services;

// Parse the task's simplified/custom PID and PR1 layout.
// This is NOT a general-purpose HL7 implementation.
public static class Hl7Parser
{
    public static IReadOnlyList<Hl7Procedure> Parse(IEnumerable<string> lines)
    {
        string? patientId = null;
        var procedures = new List<Hl7Procedure>();

        foreach (string line in lines)
        {
            if (line.StartsWith("PID|", StringComparison.Ordinal))
            {
                // PID||patientID|... => patient ID is index 2 after Split('|').
                string[] fields = line.Split('|');
                if (fields.Length < 3 || string.IsNullOrWhiteSpace(fields[2]))
                    throw new FormatException("PID is missing a patient ID.");
                if (patientId is not null)
                    throw new FormatException("This example supports one patient per HL7 file.");

                patientId = fields[2].Trim();
            }
            else if (line.StartsWith("PR1|", StringComparison.Ordinal))
            {
                if (patientId is null)
                    throw new FormatException("PR1 appeared before PID.");

                string[] fields = line.Split('|');
                if (fields.Length < 8)
                    throw new FormatException("PR1 requires eight pipe-delimited fields.");

                string[] procedurePart = fields[3].Split('^', 2);
                if (procedurePart.Length != 2 ||
                    string.IsNullOrWhiteSpace(procedurePart[0]) ||
                    string.IsNullOrWhiteSpace(procedurePart[1]) ||
                    string.IsNullOrWhiteSpace(fields[2]))
                    throw new FormatException("PR1 is missing required procedure data.");

                int sequence = int.Parse(fields[1], CultureInfo.InvariantCulture);
                int quantity = int.Parse(fields[7], CultureInfo.InvariantCulture);
                if (sequence <= 0 || quantity <= 0)
                    throw new FormatException("PR1 sequence and quantity must be positive.");

                DateTime start = ReadDate(fields[5]);
                DateTime? end = string.IsNullOrWhiteSpace(fields[6])
                    ? null
                    : ReadDate(fields[6]);

                procedures.Add(new Hl7Procedure(
                    patientId, sequence, fields[2].Trim(),
                    procedurePart[0].Trim(), procedurePart[1].Trim(),
                    start, end, quantity));
            }
        }

        if (patientId is null) throw new FormatException("HL7 file has no PID segment.");
        return procedures;
    }

    private static DateTime ReadDate(string raw) => DateTime.ParseExact(
        raw.Trim(), "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
        DateTimeStyles.None);
}