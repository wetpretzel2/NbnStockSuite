using System;
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
    }
}
