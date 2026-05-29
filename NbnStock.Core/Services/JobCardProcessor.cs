using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using NbnStock.Core.Models;
using NbnStock.Core.Repositories;

namespace NbnStock.Core.Services
{
    public class JobCardProcessor
    {
        private readonly EmailHookService _emailService;
        private readonly JobCardParser _parser;
        private readonly StockRepository _stockRepo;
        private readonly SerialisedUnitRepository _serialisedRepo;

        public JobCardProcessor(EmailConfig emailConfig)
        {
            _emailService = new EmailHookService(emailConfig);
            _parser = new JobCardParser();
            _stockRepo = new StockRepository();
            _serialisedRepo = new SerialisedUnitRepository();
        }

        /// <summary>
        /// Runs the full automation pipeline: Downloads, parses, and updates the database.
        /// Returns a tuple with the count of successfully processed and failed cards.
        /// </summary>
        public async Task<(int Processed, int Errors)> RunSyncAsync()
        {
            int processedCount = 0;
            int errorCount = 0;

            // 1. Download fresh job cards
            List<string> newPdfs = await _emailService.DownloadNewJobCardsAsync();

            foreach (var pdfPath in newPdfs)
            {
                try
                {
                    // 2. Extract the text using PdfPig
                    string fullText = ExtractTextFromPdf(pdfPath);

                    // 3. Parse the data
                    ParsedJobData jobData = _parser.ParseJobCard(fullText);

                    // 4. Commit to the database
                    ApplyToDatabase(jobData);

                    processedCount++;

                    // Clean up the file to keep your drive clear
                    File.Delete(pdfPath);
                }
                catch (Exception)
                {
                    errorCount++;
                    // Optional: Move failed PDFs to an "Error" folder instead of deleting them
                }
            }

            return (processedCount, errorCount);
        }

        private string ExtractTextFromPdf(string filePath)
        {
            string text = string.Empty;
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    // This forces PdfPig to analyze the columns and insert proper line breaks!
                    text += UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor.GetText(page) + "\n";
                }
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
                {
                    _serialisedRepo.UpdateSerialisedUnitStatus(odu.Id, UnitStatus.Installed);
                }
                else
                {
                    // Strict DB enforcement: If it's not in the DB, reject the sync!
                    throw new Exception($"ODU Serial {data.InstalledOdu} not found in inventory. Please receive it first.");
                }
            }

            if (!string.IsNullOrEmpty(data.InstalledIdu))
            {
                var idu = _serialisedRepo.GetSerialisedUnitBySerial(data.InstalledIdu);
                if (idu != null)
                {
                    _serialisedRepo.UpdateSerialisedUnitStatus(idu.Id, UnitStatus.Installed);
                }
                else
                {
                    // Strict DB enforcement: If it's not in the DB, reject the sync!
                    throw new Exception($"IDU Serial {data.InstalledIdu} not found in inventory. Please receive it first.");
                }
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
            {
                _serialisedRepo.UpdateSerialisedUnitStatus(existingUnit.Id, UnitStatus.EwastePendingSubmission);
            }
            else
            {
                // Legacy Unit pulled off a roof (Not in DB). 
                // You will need to pass an ID for a generic "Legacy Hardware" StockItem here.
                // _serialisedRepo.AddUnitToEwaste(genericLegacyStockItemId, serial);
            }
        }
    }
}