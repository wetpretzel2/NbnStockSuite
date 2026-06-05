using System;
using System.IO;
using System.Threading.Tasks;
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
    ///     Runs the full automation pipeline: downloads each unread job card, parses it,
    ///     updates the database, and only reports success after the database update succeeds.
    /// </summary>
    public async Task<(int Processed, int Errors)> RunSyncAsync()
    {
        return await _emailService.ProcessUnreadJobCardsAsync(ProcessPdfAsync);
    }

    private Task ProcessPdfAsync(string pdfPath)
    {
        // 1. Extract the text using PdfPig
        var fullText = ExtractTextFromPdf(pdfPath);

        // 2. Parse the data
        var jobData = _parser.ParseJobCard(fullText);

        if (IsNoOpJobCard(jobData))
            throw new InvalidOperationException(
                $"Job card parsed but no stock or e-waste actions were detected. PDF: {Path.GetFileName(pdfPath)}");

        // 3. Commit to the database
        ApplyToDatabase(jobData);

        // 4. Clean up the file only after the database update succeeds
        File.Delete(pdfPath);

        return Task.CompletedTask;
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
            MarkInstalledOrThrow(data.InstalledOdu, "ODU");

            // --- AUTO-CONSUME BRACKET ---
            // Every time an ODU is installed, automatically deduct 1 ODU Mounting Bracket
            var bracket = _stockRepo.GetStockItemByName("ODU Mounting Bracket");
            if (bracket == null)
                throw new Exception("Stock item 'ODU Mounting Bracket' was not found in the database.");

            _stockRepo.ConsumeStock(bracket.Id, 1);
        }

        if (!string.IsNullOrEmpty(data.InstalledIdu))
        {
            MarkInstalledOrThrow(data.InstalledIdu, "IDU");
        }

        // --- 3. Stage E-Waste Units ---
        ProcessEwaste(data.RemovedOdu, "Outdoor Unit (ODU)");
        ProcessEwaste(data.RemovedIdu, "Indoor Unit (IDU)");
    }

    private void MarkInstalledOrThrow(string serial, string unitType)
    {
        var unit = _serialisedRepo.GetSerialisedUnitBySerial(serial);
        if (unit == null)
            throw new Exception($"{unitType} Serial {serial} not found in inventory. Please receive it first.");

        if (unit.Status != UnitStatus.OnHand)
            throw new Exception($"{unitType} Serial {serial} is not currently OnHand. Current status: {unit.Status}.");

        _serialisedRepo.UpdateSerialisedUnitStatus(unit.Id, UnitStatus.Installed);
    }

    private void ProcessEwaste(string serial, string stockItemName)
    {
        if (string.IsNullOrWhiteSpace(serial))
            return;

        var existingUnit = _serialisedRepo.GetSerialisedUnitBySerial(serial);

        if (existingUnit != null)
        {
            _serialisedRepo.UpdateSerialisedUnitStatus(existingUnit.Id, UnitStatus.EwastePendingSubmission);
            return;
        }

        var stockItem = _stockRepo.GetStockItemByName(stockItemName);
        if (stockItem == null)
            throw new Exception($"Stock item '{stockItemName}' was not found in the database. Cannot create e-waste record for serial {serial}.");

        _serialisedRepo.AddUnitToEwaste(stockItem.Id, serial);
    }
}