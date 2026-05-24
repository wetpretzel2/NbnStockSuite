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
                    text += page.Text + "\n";
                }
            }
            return text;
        }

        private void ApplyToDatabase(ParsedJobData data)
        {
            // --- 1. Deduct Consumables (Wall Plates & Mounts) ---
            // Note: Make sure your StockRepository has a method to consume by name. 
            // e.g., _stockRepo.ConsumeStockItem(itemName, quantity)
            if (data.WallPlatesConsumed > 0 && !string.IsNullOrEmpty(data.WallPlateType))
            {
                // _stockRepo.ConsumeStockItem(data.WallPlateType, data.WallPlatesConsumed);
            }

            if (data.MountsConsumed > 0 && !string.IsNullOrEmpty(data.MountType))
            {
                // _stockRepo.ConsumeStockItem(data.MountType, data.MountsConsumed);
            }

            // --- 2. Mark Installed Units ---
            if (!string.IsNullOrEmpty(data.InstalledOdu))
            {
                var odu = _serialisedRepo.GetSerialisedUnitBySerial(data.InstalledOdu);
                if (odu != null) _serialisedRepo.UpdateSerialisedUnitStatus(odu.Id, UnitStatus.Installed);
            }

            if (!string.IsNullOrEmpty(data.InstalledIdu))
            {
                var idu = _serialisedRepo.GetSerialisedUnitBySerial(data.InstalledIdu);
                if (idu != null) _serialisedRepo.UpdateSerialisedUnitStatus(idu.Id, UnitStatus.Installed);
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