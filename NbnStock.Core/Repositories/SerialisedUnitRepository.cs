using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using NbnStock.Core.Models;
using NbnStock.Core.Data;

namespace NbnStock.Core.Repositories
{
    public class SerialisedUnitRepository
    {
        public void AddSerialisedUnit(SerialisedUnit unit)
        {
            string connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string sql = @"
            INSERT INTO SerialisedUnits
            (StockItemId, SerialNumber, Status, Notes, LastUpdatedUtc)
            VALUES
            (@StockItemId, @SerialNumber, @Status, @Notes, @LastUpdatedUtc);
        ";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@StockItemId", unit.StockItemId);
                    command.Parameters.AddWithValue("@SerialNumber", unit.SerialNumber);
                    command.Parameters.AddWithValue("@Status", unit.Status);
                    command.Parameters.AddWithValue("@Notes", unit.Notes ?? "");
                    command.Parameters.AddWithValue("@LastUpdatedUtc", unit.LastUpdatedUtc.ToString("o"));

                    command.ExecuteNonQuery();
                }
            }
        }

        public List<SerialisedUnit> GetAllSerialisedUnits()
        {
            var units = new List<SerialisedUnit>();
            string connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string sql = @"SELECT Id, StockItemId, SerialNumber, Status, Notes, LastUpdatedUtc FROM SerialisedUnits;";

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
                            Status = reader.GetString(3),
                            Notes = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            LastUpdatedUtc = DateTime.Parse(reader.GetString(5))
                        };

                        units.Add(unit);
                    }
                }
            }
            return units;
        }
        public List<SerialisedUnit> GetSerialisedUnitsByStatus(string status)
        {
            var units = new List<SerialisedUnit>();

            string connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string sql = @"SELECT Id, StockItemId, SerialNumber, Status, Notes, LastUpdatedUtc 
                       FROM SerialisedUnits
                       WHERE Status = @Status;";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Status", status);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var unit = new SerialisedUnit
                            {
                                Id = reader.GetInt32(0),
                                StockItemId = reader.GetInt32(1),
                                SerialNumber = reader.GetString(2),
                                Status = reader.GetString(3),
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
        public void UpdateSerialisedUnitStatus(int id, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Status cannot be empty.");

            string connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string sql = @"
            UPDATE SerialisedUnits
            SET Status = @Status,
                LastUpdatedUtc = @LastUpdatedUtc
            WHERE Id = @Id;
        ";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@LastUpdatedUtc", DateTime.UtcNow.ToString("o"));

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
