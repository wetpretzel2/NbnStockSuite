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
            string connectionString = $"Data Source={DatabaseInitaliser.DatabasePath}";
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

                }
            }
            
        }
    }
}
