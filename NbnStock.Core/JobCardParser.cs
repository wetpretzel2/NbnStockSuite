using System;
using System.Text.RegularExpressions;

namespace NbnStock.Core.Services
{
    public class JobCardParser
    {
        public ParsedJobData ParseJobCard(string pdfText)
        {
            var jobData = new ParsedJobData();

            // 1. Determine Job Type
            string jobType = ExtractValue(pdfText, "Job Type") ?? "Unknown";
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
                string cableTypeUsed = ExtractValue(pdfText, "Cable Type Used");
                if (cableTypeUsed != null && cableTypeUsed.IndexOf("CAT6", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    jobData.WallPlateType = "Cat 6 Wallplate";
                }
            }

            // 5. Consumables Logic: Mounts
            bool isExistingMount = false;

            if (jobType != null && (
                jobType.IndexOf("Service Call", StringComparison.OrdinalIgnoreCase) >= 0 ||
                jobType.IndexOf("SwapToLatest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                jobType.IndexOf("SwapODU", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                string mountStatus = ExtractValue(pdfText, "Mount Type Status");
                if (mountStatus != null && mountStatus.IndexOf("Existing", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isExistingMount = true;
                }
            }

            if (!isExistingMount)
            {
                string mountInstalled = ExtractValue(pdfText, "Mount Type Installed");

                if (mountInstalled != null && mountInstalled.IndexOf("Flexi Tin", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    jobData.MountsConsumed = 1;
                    jobData.MountType = "1m Tin Mount";
                }
            }

            // Only deduct a mount if it isn't marked as existing
            if (!isExistingMount)
            {
                string mountInstalled = ExtractValue(pdfText, "Mount Type Installed");

                if (string.Equals(mountInstalled, "Flexi Tin", StringComparison.OrdinalIgnoreCase))
                {
                    jobData.MountsConsumed = 1;
                    jobData.MountType = "1m Tin Mount"; // <-- FIXED: Now exactly matches the seeded DB name!
                }
                // You can easily expand this later (e.g., if mountInstalled == "Gutter Mount", map to "Gutter Mount")
            }

            return jobData;
        }

        /// <summary>
        /// Emulates the global V1 scanner fix, ensuring serials from the PDF 
        /// match the clean format in the SQLite database.
        /// </summary>
        private string CleanSerial(string rawSerial)
        {
            if (string.IsNullOrWhiteSpace(rawSerial)) return null;

            // Slices off any extra table headers the Regex accidentally swallowed
            string serialOnly = rawSerial.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];

            return serialOnly.TrimStart('s', 'S').Trim();
        }

        /// <summary>
        /// Pulls values from the pseudo-CSV format extracted by PdfPig.
        /// Safely handles the newline and whitespace formatting inside the quotes.
        /// </summary>
        /// <summary>
        /// Pulls values from the pseudo-CSV format extracted by PdfPig.
        /// Safely handles the newline and whitespace formatting inside the quotes.
        /// </summary>
        private string ExtractValue(string text, string key)
        {
            // Strip any accidental quotes or commas to normalize the raw text
            string cleanText = text.Replace("\"", "").Replace(",", "");

            // 1. Try to find the value sitting on the exact same line, separated by spaces or colons.
            // Matches: "Job Type WNTD Install" or "Job Type: WNTD Install"
            string sameLinePattern = $@"{Regex.Escape(key)}[ \t:]+([^\r\n]+)";
            var match = Regex.Match(cleanText, sameLinePattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                string val = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }

            // 2. Try to find the value pushed to the VERY NEXT line (PdfPig often formats tables this way).
            // Matches: 
            // "ODU Serial Number Barcode Installed"
            // "KLT25210190B"
            string nextLinePattern = $@"{Regex.Escape(key)}[ \t]*\r?\n[ \t]*([^\r\n]+)";
            match = Regex.Match(cleanText, nextLinePattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                string val = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }

            return null;
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
}