using NbnStock.Core.Models;
using NbnStock.Core.Repositories;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace NbnStock.Core.Services;

public class JobCardProcessor
{
    private readonly EmailHookService _emailService;
    private readonly JobCardParser _parser;
    private readonly SerialisedUnitRepository _serialisedRepo;
    private readonly StockRepository _stockRepo;

    public JobCardProcessor(EmailConfig emailConfig)
    {
        _emailService = new EmailHookService(emailConfig);
        _parser = new JobCardParser();
        _stockRepo = new StockRepository();
        _serialisedRepo = new SerialisedUnitRepository();
    }

    /// <summary>
    ///     Runs the full automation pipeline: Downloads, parses, and updates the database.
    ///     Returns a tuple with the count of successfully processed and failed cards.
    /// </summary>
    public async Task<(int Processed, int Errors)> RunSyncAsync()
    {
        var processedCount = 0;
        var errorCount = 0;

        // 1. Download fresh job cards
        var newPdfs = await _emailService.DownloadNewJobCardsAsync();

        foreach (var pdfPath in newPdfs)
            try
            {
                // 2. Extract the text using PdfPig
                var fullText = ExtractTextFromPdf(pdfPath);

                // 3. Parse the data
                var jobData = _parser.ParseJobCard(fullText);

                if (IsNoOpJobCard(jobData))
                    throw new InvalidOperationException(
                        $"Job card parsed but no stock or e-waste actions were detected. PDF: {Path.GetFileName(pdfPath)}");

                // 4. Commit to the database
                ApplyToDatabase(jobData);
                processedCount++;
                // Clean up the file to keep your drive clear
                File.Delete(pdfPath);
            }
            catch (Exception ex)
            {
                errorCount++;

                var errorFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NbnStockSuite",
                    "JobCardErrors");

                Directory.CreateDirectory(errorFolder);

                var errorFile = Path.Combine(errorFolder, "sync-errors.txt");

                File.AppendAllText(errorFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {Path.GetFileName(pdfPath)}{Environment.NewLine}" +
                    $"{ex}{Environment.NewLine}{Environment.NewLine}");
            }

        return (processedCount, errorCount);
    }

    private static bool IsNoOpJobCard(ParsedJobData data)
    {
        return string.IsNullOrWhiteSpace(data.InstalledOdu)
               && string.IsNullOrWhiteSpace(data.InstalledIdu)
               && string.IsNullOrWhiteSpace(data.RemovedOdu)
               && string.IsNullOrWhiteSpace(data.RemovedIdu)
               && data.WallPlatesConsumed <= 0
               && data.MountsConsumed <= 0;
    }

    private string ExtractTextFromPdf(string filePath)
    {
        var text = string.Empty;
        using (var document = PdfDocument.Open(filePath))
        {
            foreach (var page in document.GetPages())
                // This forces PdfPig to analyze the columns and insert proper line breaks!
                text += ContentOrderTextExtractor.GetText(page) + "\n";
        }

        return text;
    }

    private void ApplyToDatabase(ParsedJobData data)
    {
        // --- 1. Deduct Consumables (Wall Plates & Mounts) ---
        if (data.WallPlatesConsumed > 0 && !string.IsNullOrEmpty(data.WallPlateType))
        {
            var wallPlate = _stockRepo.GetStockItemByName(data.WallPlateType);
            if (wallPlate != null) _stockRepo.ConsumeStock(wallPlate.Id, data.WallPlatesConsumed);
        }

        if (data.MountsConsumed > 0 && !string.IsNullOrEmpty(data.MountType))
        {
            var mount = _stockRepo.GetStockItemByName(data.MountType);
            if (mount != null) _stockRepo.ConsumeStock(mount.Id, data.MountsConsumed);
        }

        // --- 2. Mark Installed Units (STRICT INVENTORY MATCH REQUIRED) ---
        if (!string.IsNullOrEmpty(data.InstalledOdu))
        {
            var odu = _serialisedRepo.GetSerialisedUnitBySerial(data.InstalledOdu);
            if (odu != null)
                _serialisedRepo.UpdateSerialisedUnitStatus(odu.Id, UnitStatus.Installed);
            else
                // Strict DB enforcement: If it's not in the DB, reject the sync!
                throw new Exception($"ODU Serial {data.InstalledOdu} not found in inventory. Please receive it first.");

            // --- AUTO-CONSUME BRACKET ---
            // Every time an ODU is installed, automatically deduct 1 ODU Mounting Bracket
            var bracket = _stockRepo.GetStockItemByName("ODU Mounting Bracket");
            if (bracket != null) _stockRepo.ConsumeStock(bracket.Id, 1);
        }

        // --- 3. Stage E-Waste Units ---
        ProcessEwaste(data.RemovedOdu);
        ProcessEwaste(data.RemovedIdu);
    }

    private void ProcessEwaste(string serial)
    {
        if (string.IsNullOrEmpty(serial)) return;

        var existingUnit = _serialisedRepo.GetSerialisedUnitBySerial(serial);

        if (existingUnit != null)
            _serialisedRepo.UpdateSerialisedUnitStatus(existingUnit.Id, UnitStatus.EwastePendingSubmission);
        // Legacy Unit pulled off a roof (Not in DB). 
        // You will need to pass an ID for a generic "Legacy Hardware" StockItem here.
        // _serialisedRepo.AddUnitToEwaste(genericLegacyStockItemId, serial);
    }
}