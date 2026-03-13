using System;
using Microsoft.Data.Sqlite;
using System.IO;

namespace NbnStock.Core.Data
{
    public static class DatabaseInitaliser
    {
        public static string DatabasePath { get; private set; }
        public static void Initialise()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolderPath = System.IO.Path.Combine(appDataPath, "NbnStock");
            string databaseFilePath = System.IO.Path.Combine(appFolderPath, "NbnStock.db");
            DatabasePath = databaseFilePath;
            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }

            string connectionString = $"Data Source={databaseFilePath}";
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS StockItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ItemCode TEXT NOT NULL UNIQUE,
                        Name TEXT NOT NULL,
                        Category TEXT,
                        Quantity INTEGER NOT NULL DEFAULT 0,
                        Unit TEXT NOT NULL,
                        MinimumStock INTEGER NOT NULL DEFAULT 0,
                        IsSerialised INTEGER NOT NULL,
                        Notes TEXT,
                        LastUpdatedUtc TEXT NOT NULL);
                ";
                string createSerialisedUnitsTableSql = @"
                    CREATE TABLE IF NOT EXISTS SerialisedUnits (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        StockItemId INTEGER NOT NULL,
                        SerialNumber TEXT NOT NULL UNIQUE,
                        Status TEXT NOT NULL,
                        Notes TEXT,
                        LastUpdatedUtc TEXT NOT NULL,
                        FOREIGN KEY (StockItemId) REFERENCES StockItems(Id)
                    );
                ";

                using (var command = new SqliteCommand(createTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }
                using (var command = new SqliteCommand(createSerialisedUnitsTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            
        }
    }
}
