using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using NbnStock.Core.Data;
using NbnStock.Core.Models;

namespace NbnStock.Core.Repositories;

public class SerialisedUnitRepository
{
    private StockItem GetValidSerialisedStockItem(int stockItemId)
    {
        var stockRepo = new StockRepository();
        var item = stockRepo.GetStockItemById(stockItemId);

        if (item == null) throw new InvalidOperationException("Stock item not found.");

        if (!item.IsSerialised) throw new InvalidOperationException("Stock item is not serialised.");

        return item;
    }

    public int AddSerialisedUnit(SerialisedUnit unit)
    {
        var connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var sql = @"INSERT INTO SerialisedUnits
                       (StockItemId, SerialNumber, Status, Notes, LastUpdatedUtc)
                       VALUES
                       (@StockItemId, @SerialNumber, @Status, @Notes, @LastUpdatedUtc);
                       
                       SELECT last_insert_rowid();";

            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@StockItemId", unit.StockItemId);
                command.Parameters.AddWithValue("@SerialNumber", unit.SerialNumber);
                command.Parameters.AddWithValue("@Status", unit.Status.ToString());
                command.Parameters.AddWithValue("@Notes", unit.Notes ?? "");
                command.Parameters.AddWithValue("@LastUpdatedUtc", unit.LastUpdatedUtc.ToString("o"));

                var result = command.ExecuteScalar();

                return Convert.ToInt32(result);
            }
        }
    }

    public List<SerialisedUnit> GetAllSerialisedUnits()
    {
        var units = new List<SerialisedUnit>();
        var connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var sql = @"SELECT Id, StockItemId, SerialNumber, Status, Notes, LastUpdatedUtc 
                               FROM SerialisedUnits;";

            using (var command = new SqliteCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var unit = new SerialisedUnit
                    {
                        Id = reader.GetInt32(0),
                        StockItemId = reader.GetInt32(1),
                        SerialNumber = reader.GetString(2),
                        Status = Enum.Parse<UnitStatus>(reader.GetString(3)),
                        Notes = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        LastUpdatedUtc = DateTime.Parse(reader.GetString(5))
                    };

                    units.Add(unit);
                }
            }
        }

        return units;
    }

    public List<SerialisedUnit> GetSerialisedUnitsByStatus(UnitStatus status)
    {
        var units = new List<SerialisedUnit>();

        var connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var sql = @"SELECT Id, StockItemId, SerialNumber, Status, Notes, LastUpdatedUtc 
                               FROM SerialisedUnits
                               WHERE Status = @Status;";

            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Status", status.ToString());

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var unit = new SerialisedUnit
                        {
                            Id = reader.GetInt32(0),
                            StockItemId = reader.GetInt32(1),
                            SerialNumber = reader.GetString(2),
                            Status = Enum.Parse<UnitStatus>(reader.GetString(3)),
                            Notes = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            LastUpdatedUtc = DateTime.Parse(reader.GetString(5))
                        };

                        units.Add(unit);
                    }
                }
            }
        }

        return units;
    }

    public void UpdateSerialisedUnitStatus(int id, UnitStatus status)
    {
        var connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var sql = @"UPDATE SerialisedUnits
                               SET Status = @Status,
                                   LastUpdatedUtc = @LastUpdatedUtc
                               WHERE Id = @Id;";

            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Status", status.ToString());
                command.Parameters.AddWithValue("@LastUpdatedUtc", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@Id", id);

                command.ExecuteNonQuery();
            }
        }
    }

    public SerialisedUnit? GetSerialisedUnitBySerial(string serialNumber)
    {
        var connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var sql = @"SELECT Id, StockItemId, SerialNumber, Status, Notes, LastUpdatedUtc
                               FROM SerialisedUnits
                               WHERE SerialNumber = @SerialNumber;";

            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@SerialNumber", serialNumber);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                        return new SerialisedUnit
                        {
                            Id = reader.GetInt32(0),
                            StockItemId = reader.GetInt32(1),
                            SerialNumber = reader.GetString(2),
                            Status = Enum.Parse<UnitStatus>(reader.GetString(3)),
                            Notes = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            LastUpdatedUtc = DateTime.Parse(reader.GetString(5))
                        };
                }
            }
        }

        return null;
    }

    public void ReceiveSerialisedUnit(int stockItemId, string serialNumber)
    {
        serialNumber = serialNumber?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new InvalidOperationException("Serial number cannot be empty.");

        GetValidSerialisedStockItem(stockItemId);

        var existing = GetSerialisedUnitBySerial(serialNumber);

        if (existing != null) throw new InvalidOperationException("Serial number already exists.");

        var unit = new SerialisedUnit
        {
            StockItemId = stockItemId,
            SerialNumber = serialNumber,
            Status = UnitStatus.OnHand,
            Notes = "",
            LastUpdatedUtc = DateTime.UtcNow
        };

        AddSerialisedUnit(unit);
    }

    public void MarkUnitInstalled(string serialNumber)
    {
        serialNumber = serialNumber?.Trim() ?? "";
        var unit = GetSerialisedUnitBySerial(serialNumber);

        if (unit == null) throw new InvalidOperationException("Serialised unit not found.");

        if (unit.Status != UnitStatus.OnHand)
            throw new InvalidOperationException("Only OnHand units can be installed.");

        UpdateSerialisedUnitStatus(unit.Id, UnitStatus.Installed);
    }

    public void AddUnitToEwaste(int stockItemId, string serialNumber)
    {
        serialNumber = serialNumber?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new InvalidOperationException("Serial number cannot be empty.");

        GetValidSerialisedStockItem(stockItemId);

        var existing = GetSerialisedUnitBySerial(serialNumber);

        if (existing != null) throw new InvalidOperationException("Serial number already exists.");

        var unit = new SerialisedUnit
        {
            StockItemId = stockItemId,
            SerialNumber = serialNumber,
            Status = UnitStatus.EwastePendingSubmission,
            Notes = "Removed from site",
            LastUpdatedUtc = DateTime.UtcNow
        };

        AddSerialisedUnit(unit);
    }

    public void MoveToNextEwasteStage(string serialNumber)
    {
        serialNumber = serialNumber?.Trim() ?? "";
        var unit = GetSerialisedUnitBySerial(serialNumber);

        if (unit == null) throw new InvalidOperationException("Serialised unit not found.");

        var nextStatus = unit.Status switch
        {
            UnitStatus.EwastePendingSubmission => UnitStatus.EwasteAwaitingApproval,
            UnitStatus.EwasteAwaitingApproval => UnitStatus.ApprovedForDisposal,
            UnitStatus.ApprovedForDisposal => UnitStatus.Disposed,
            _ => throw new InvalidOperationException("Invalid ewaste transition.")
        };

        UpdateSerialisedUnitStatus(unit.Id, nextStatus);
    }
}