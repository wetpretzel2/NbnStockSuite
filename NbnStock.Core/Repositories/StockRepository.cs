using System;
using NbnStock.Core.Data;
using NbnStock.Core.Models;
using Microsoft.Data.Sqlite;

namespace NbnStock.Core.Repositories
{
    public class StockRepository
    {
        public void AddStockItem(StockItem item)
        {
            string connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string sql = @"
                INSERT INTO StockItems
                    (ItemCode, Name, Category, Quantity, Unit, MinimumStock, IsSerialised,
                    Notes, LastUpdatedUtc)
                    VALUES
                    (@ItemCode, @Name, @Category, @Quantity, @Unit, @MinimumStock,
                    @IsSerialised, @Notes, @LastUpdatedUtc);
                ";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ItemCode", item.ItemCode);
                    command.Parameters.AddWithValue("@Name", item.Name);
                    command.Parameters.AddWithValue("@Category", item.Category ?? "");
                    command.Parameters.AddWithValue("@Quantity", item.Quantity);
                    command.Parameters.AddWithValue("@Unit", item.Unit);
                    command.Parameters.AddWithValue("@MinimumStock", item.MinimumStock);
                    command.Parameters.AddWithValue("@IsSerialised", item.IsSerialised ? 1 : 0);
                    command.Parameters.AddWithValue("@Notes", item.Notes ?? "");
                    command.Parameters.AddWithValue("@LastUpdatedUtc", item.LastUpdatedUtc.ToString("o"));

                    command.ExecuteNonQuery();

                }
            }
            
        }
    }
}
