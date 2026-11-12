# BI Data Format

## Overview

The application reads HL7 files and exports patient procedure data to JSON.

Each HL7 file produces a separate JSON file in the configured output folder.

## HL7 data

The application reads two segment types:

- `PID` — Patient identifier.
- `PR1` — Procedure details, including code, name, start time, optional end time and quantity.

Example:

```text
PID||P001|000000-00000
PR1|4|VNC|04101^Virsmas anestēzija||20250822091200||1
```

## JSON output

Each procedure is stored as a separate JSON object. The output includes:

- Patient ID
- Procedure sequence
- Procedure code and name
- Code system
- Start and optional end time
- Quantity

Output filenames follow this format:

```text
original-file.hl7.json
```

The JSON files can be used for BI reporting or imported into another system.

Patient identifiers are included in the exported data, so access to the output folder should be restricted.