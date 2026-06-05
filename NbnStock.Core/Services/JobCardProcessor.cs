using NbnStock.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Microsoft.Data.Sqlite;
using NbnStock.Core.Data;

namespace NbnStock.Core.Services;

public class JobCardProcessor
{
    private readonly EmailHookService _emailService;
    private readonly JobCardParser _parser;

    public JobCardProcessor(EmailConfig emailConfig)
    {
        _emailService = new EmailHookService(emailConfig);
        _parser = new JobCardParser();
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
        var connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            ApplyToDatabase(data, connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
    private void ApplyToDatabase(ParsedJobData data, SqliteConnection connection, SqliteTransaction transaction)
    {
        if (data.WallPlatesConsumed > 0 && !string.IsNullOrEmpty(data.WallPlateType))
        {
            var wallPlate = GetStockItemIdByNameOrThrow(connection, transaction, data.WallPlateType);
            ConsumeStock(connection, transaction, wallPlate, data.WallPlatesConsumed);
        }

        if (data.MountsConsumed > 0 && !string.IsNullOrEmpty(data.MountType))
        {
            var mount = GetStockItemIdByNameOrThrow(connection, transaction, data.MountType);
            ConsumeStock(connection, transaction, mount, data.MountsConsumed);
        }

        if (!string.IsNullOrEmpty(data.InstalledOdu))
        {
            MarkInstalledOrThrow(connection, transaction, data.InstalledOdu, "ODU");

            var bracket = GetStockItemIdByNameOrThrow(connection, transaction, "ODU Mounting Bracket");
            ConsumeStock(connection, transaction, bracket, 1);
        }

        if (!string.IsNullOrEmpty(data.InstalledIdu))
            MarkInstalledOrThrow(connection, transaction, data.InstalledIdu, "IDU");

        ProcessEwaste(connection, transaction, data.RemovedOdu, "Outdoor Unit (ODU)");
        ProcessEwaste(connection, transaction, data.RemovedIdu, "Indoor Unit (IDU)");
    }

    private static int GetStockItemIdByNameOrThrow(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string stockItemName)
    {
        const string sql = @"
            SELECT Id
            FROM StockItems
            WHERE Name = @Name;
        ";

        using var command = new SqliteCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@Name", stockItemName);

        var result = command.ExecuteScalar();
        if (result == null || result == DBNull.Value)
            throw new Exception($"Stock item '{stockItemName}' was not found in the database.");

        return Convert.ToInt32(result);
    }

    private static void ConsumeStock(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int stockItemId,
        int quantityUsed)
    {
        if (quantityUsed <= 0)
            throw new Exception("Quantity used must be greater than zero.");

        const string sql = @"
            UPDATE StockItems
            SET Quantity = Quantity - @QuantityUsed,
                LastUpdatedUtc = @LastUpdatedUtc
            WHERE Id = @Id
              AND Quantity >= @QuantityUsed;
        ";

        using var command = new SqliteCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@QuantityUsed", quantityUsed);
        command.Parameters.AddWithValue("@LastUpdatedUtc", DateTime.UtcNow.ToString("o"));
        command.Parameters.AddWithValue("@Id", stockItemId);

        var rowsAffected = command.ExecuteNonQuery();
        if (rowsAffected != 1)
            throw new Exception($"Insufficient stock for stock item Id {stockItemId}.");
    }

    private static void MarkInstalledOrThrow(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string serial,
        string unitType)
    {
        const string selectSql = @"
            SELECT Id, Status
            FROM SerialisedUnits
            WHERE SerialNumber = @SerialNumber;
        ";

        int unitId;
        string status;

        using (var selectCommand = new SqliteCommand(selectSql, connection, transaction))
        {
            selectCommand.Parameters.AddWithValue("@SerialNumber", serial);

            using var reader = selectCommand.ExecuteReader();
            if (!reader.Read())
                throw new Exception($"{unitType} Serial {serial} not found in inventory. Please receive it first.");

            unitId = reader.GetInt32(0);
            status = reader.GetString(1);
        }

        if (!string.Equals(status, UnitStatus.OnHand.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new Exception($"{unitType} Serial {serial} is not currently OnHand. Current status: {status}.");

        const string updateSql = @"
            UPDATE SerialisedUnits
            SET Status = @Status,
                LastUpdatedUtc = @LastUpdatedUtc
            WHERE Id = @Id;
        ";

        using var updateCommand = new SqliteCommand(updateSql, connection, transaction);
        updateCommand.Parameters.AddWithValue("@Status", UnitStatus.Installed.ToString());
        updateCommand.Parameters.AddWithValue("@LastUpdatedUtc", DateTime.UtcNow.ToString("o"));
        updateCommand.Parameters.AddWithValue("@Id", unitId);

        updateCommand.ExecuteNonQuery();
    }

    private static void ProcessEwaste(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string serial,
        string stockItemName)
    {
        if (string.IsNullOrWhiteSpace(serial))
            return;

        const string existingSql = @"
            SELECT Id
            FROM SerialisedUnits
            WHERE SerialNumber = @SerialNumber;
        ";

        int? existingUnitId = null;

        using (var existingCommand = new SqliteCommand(existingSql, connection, transaction))
        {
            existingCommand.Parameters.AddWithValue("@SerialNumber", serial);
            var result = existingCommand.ExecuteScalar();

            if (result != null && result != DBNull.Value)
                existingUnitId = Convert.ToInt32(result);
        }

        if (existingUnitId.HasValue)
        {
            const string updateSql = @"
                UPDATE SerialisedUnits
                SET Status = @Status,
                    LastUpdatedUtc = @LastUpdatedUtc
                WHERE Id = @Id;
            ";

            using var updateCommand = new SqliteCommand(updateSql, connection, transaction);
            updateCommand.Parameters.AddWithValue("@Status", UnitStatus.EwastePendingSubmission.ToString());
            updateCommand.Parameters.AddWithValue("@LastUpdatedUtc", DateTime.UtcNow.ToString("o"));
            updateCommand.Parameters.AddWithValue("@Id", existingUnitId.Value);
            updateCommand.ExecuteNonQuery();
            return;
        }

        var stockItemId = GetStockItemIdByNameOrThrow(connection, transaction, stockItemName);

        const string insertSql = @"
            INSERT INTO SerialisedUnits
                (StockItemId, SerialNumber, Status, Notes, LastUpdatedUtc)
            VALUES
                (@StockItemId, @SerialNumber, @Status, @Notes, @LastUpdatedUtc);
        ";

        using var insertCommand = new SqliteCommand(insertSql, connection, transaction);
        insertCommand.Parameters.AddWithValue("@StockItemId", stockItemId);
        insertCommand.Parameters.AddWithValue("@SerialNumber", serial.Trim());
        insertCommand.Parameters.AddWithValue("@Status", UnitStatus.EwastePendingSubmission.ToString());
        insertCommand.Parameters.AddWithValue("@Notes", "Removed from site");
        insertCommand.Parameters.AddWithValue("@LastUpdatedUtc", DateTime.UtcNow.ToString("o"));
        insertCommand.ExecuteNonQuery();
    }
}