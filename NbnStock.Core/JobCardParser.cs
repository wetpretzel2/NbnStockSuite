namespace NbnStock.Core.Services;

public class JobCardParser
{
    public ParsedJobData ParseJobCard(string pdfText)
    {
        var jobData = new ParsedJobData();

        // 1. Determine Job Type
        var jobType = ExtractValue(pdfText, "Job Type") ?? "Unknown";
        jobData.JobType = jobType;

        // 2. Extract Installed Serials (Safely stripping the 'S' prefix if present)
        jobData.InstalledOdu = CleanSerial(ExtractValue(pdfText, "ODU Serial Number Barcode Installed"));
        jobData.InstalledIdu = CleanSerial(ExtractValue(pdfText, "IDU Serial Number Barcode Installed"));

        // 3. Extract E-Waste / Removed Serials
        jobData.RemovedOdu = CleanSerial(ExtractValue(pdfText, "Old ODU Serial"));
        jobData.RemovedIdu = CleanSerial(ExtractValue(pdfText, "Old IDU Serial"));

        // 4. Consumables Logic: Wall Plates
        if (jobType != null && jobType.IndexOf("WNTD Install", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            jobData.WallPlatesConsumed = 1;
            jobData.WallPlateType = "Cat 5e Wallplate"; // System default

            // Look at the cable type to override the wall plate to Cat 6 if necessary
            var cableTypeUsed = ExtractValue(pdfText, "Cable Type Used");
            if (cableTypeUsed != null && cableTypeUsed.IndexOf("CAT6", StringComparison.OrdinalIgnoreCase) >= 0)
                jobData.WallPlateType = "Cat 6 Wallplate";
        }
        // Service Call / Swap jobs may consume a wallplate if clearly noted in comments
        if (jobData.WallPlatesConsumed == 0 && IsServiceOrSwapJob(jobType))
        {
            var additionalInfo = ExtractValue(pdfText, "Additional Information") ?? "";
            var complaintResolved = ExtractValue(pdfText, "Complaint Resolved") ?? "";
            var comments = $"{additionalInfo} {complaintResolved}";

            if (CommentsIndicateWallPlateUsed(comments))
            {
                jobData.WallPlatesConsumed = 1;
                jobData.WallPlateType = "Cat 5e Wallplate";
            }
        }

        // 5. Consumables Logic: Mounts
        var isExistingMount = false;

        if (IsServiceOrSwapJob(jobType))
        {
            var mountStatus = ExtractValue(pdfText, "Mount Type Status");
            if (ContainsIgnoreCase(mountStatus, "Existing"))
                isExistingMount = true;
        }

        if (!isExistingMount)
        {
            var mountInstalled = ExtractValue(pdfText, "Mount Type Installed");
            var mappedMountType = MapMountType(mountInstalled);

            if (!string.IsNullOrWhiteSpace(mappedMountType))
            {
                jobData.MountsConsumed = 1;
                jobData.MountType = mappedMountType;
            }
        }

        return jobData;
    }

    private static bool IsServiceOrSwapJob(string jobType)
    {
        return ContainsIgnoreCase(jobType, "Service Call")
               || ContainsIgnoreCase(jobType, "SwapToLatest")
               || ContainsIgnoreCase(jobType, "Swap To Latest")
               || ContainsIgnoreCase(jobType, "SwapODU")
               || ContainsIgnoreCase(jobType, "Swap ODU");
    }

    private static string MapMountType(string mountInstalled)
    {
        if (string.IsNullOrWhiteSpace(mountInstalled))
            return null;

        if (ContainsIgnoreCase(mountInstalled, "Flexi Tin"))
            return "1m Tin Mount";

        if (ContainsIgnoreCase(mountInstalled, "Fascia"))
            return "Vertical/Fascia Mount";

        if (ContainsIgnoreCase(mountInstalled, "Gutter"))
            return "Gutter Mount";

        if (ContainsIgnoreCase(mountInstalled, "2m Tin"))
            return "2m Tin Mount";

        if (ContainsIgnoreCase(mountInstalled, "3m Tin"))
            return "3m Tin Mount";

        return null;
    }

    private static bool ContainsIgnoreCase(string value, string searchText)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    ///     Emulates the global V1 scanner fix, ensuring serials from the PDF
    ///     match the clean format in the SQLite database.
    /// </summary>
    private string CleanSerial(string rawSerial)
    {
        if (string.IsNullOrWhiteSpace(rawSerial)) return null;

        // Slices off any extra table headers the Regex accidentally swallowed
        var serialOnly = rawSerial.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];

        return serialOnly.TrimStart('s', 'S').Trim();
    }

    /// <summary>
    ///     Pulls values from the pseudo-CSV format extracted by PdfPig.
    ///     Safely handles the newline and whitespace formatting inside the quotes.
    /// </summary>
    private string ExtractValue(string text, string key)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(key))
            return null;

        var cleanText = NormalizePdfText(text);

        var keyIndex = cleanText.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (keyIndex < 0)
            return null;

        var valueStart = keyIndex + key.Length;

        while (valueStart < cleanText.Length &&
               (char.IsWhiteSpace(cleanText[valueStart]) || cleanText[valueStart] == ':' ||
                cleanText[valueStart] == '-'))
            valueStart++;

        if (valueStart >= cleanText.Length)
            return null;

        var valueEnd = FindNextKnownLabelIndex(cleanText, valueStart);
        var rawValue = valueEnd > valueStart
            ? cleanText.Substring(valueStart, valueEnd - valueStart)
            : cleanText.Substring(valueStart);

        var value = rawValue.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string NormalizePdfText(string text)
    {
        return text
            .Replace("\"", "")
            .Replace(",", "")
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");
    }

    private static int FindNextKnownLabelIndex(string text, int startIndex)
    {
        var knownLabels = new[]
        {
            "Address",
            "Fixed Wireless Job Details",
            "Work Order ID",
            "Client Name",
            "Region",
            "Job Type",
            "Installation Date",
            "Appointment Time Slot",
            "Final Job Status",
            "EPS Appointment ID",
            "Location ID",
            "NTD ID",
            "Job Times",
            "Job Start Time",
            "Job Finish Time",
            "Fixed Wireless Job Closure Information",
            "Service Fault Main",
            "PreQual Signal",
            "Prequal Signal Final",
            "Delta",
            "RSRP",
            "Cable Type Used",
            "Mount Type Status",
            "Mount Type Installed",
            "Prequal Mount Height",
            "Prequal Cell Final",
            "Prequal Cell Direction Final",
            "Prequal Cell Direction (Alternative)",
            "Current Cell LTE Threshold",
            "Current Cell Delta Threshold",
            "Current Cell LTE RSRP Target",
            "WISDM LOC To Target Site Line of",
            "WISDM Number of Candidate Target",
            "Link Speed Cable Used",
            "Link Speed",
            "Fixed Wireless Inventory",
            "ODU Serial Number Barcode Installed",
            "ODU IMEI Barcode Installed",
            "IDU Serial Number Barcode Installed",
            "Old ODU Serial",
            "Old IMEI Number",
            "Old IDU Serial",
            "Comments",
            "Additional Information",
            "Complaint Resolved",
            "GPS Location and Signatures",
            "Technician Declaration GPS Latitude",
            "Technician Declaration GPS Longitude",
            "Customer Declaration GPS Latitude",
            "Customer Declaration GPS Longitude",
            "Technician Signoff GPS Latitude",
            "Technician Signoff GPS Longitude",
            "Photos"
        };

        var bestIndex = -1;

        foreach (var label in knownLabels)
        {
            var index = text.IndexOf(label, startIndex, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                continue;

            if (bestIndex < 0 || index < bestIndex)
                bestIndex = index;
        }

        return bestIndex;
    }
}

public class ParsedJobData
{
    public string JobType { get; set; }

    // Installed Hardware
    public string InstalledOdu { get; set; }
    public string InstalledIdu { get; set; }

    // E-Waste Hardware
    public string RemovedOdu { get; set; }
    public string RemovedIdu { get; set; }

    // Mounts
    public int MountsConsumed { get; set; }
    public string MountType { get; set; }

    // Wall Plates
    public int WallPlatesConsumed { get; set; }
    public string WallPlateType { get; set; }
}