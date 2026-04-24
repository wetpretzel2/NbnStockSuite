using System;
using Microsoft.Data.Sqlite;
using System.IO;

namespace NbnStock.Core.Data
{
    public static class DatabaseInitialiser
    {
        public static string DatabasePath { get; private set; }

        public static void Initialise()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolderPath = Path.Combine(appDataPath, "NbnStock");
            string databaseFilePath = Path.Combine(appFolderPath, "NbnStock.db");
            DatabasePath = databaseFilePath;

            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }

            string connectionString = $"Data Source={databaseFilePath}";
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // 1. Create Tables
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
                        SupplyType TEXT NOT NULL,
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

                // 2. Seed Default Data (Populate the dropdown!)
                SeedDefaultStockItems(connection);
            }
        }

        private static void SeedDefaultStockItems(SqliteConnection connection)
        {
            // Check if we already have items. If we do, don't overwrite them.
            string checkSql = "SELECT COUNT(*) FROM StockItems";
            using (var command = new SqliteCommand(checkSql, connection))
            {
                long count = (long)command.ExecuteScalar();
                if (count > 0) return; // DB is already populated, back out.
            }

            // INSERT OR IGNORE is a SQLite safety net. If an item with the same code already exists, it skips it.
            // Note: 1 = true (serialised), 0 = false (consumable) in SQLite.
            string seedSql = @"
                INSERT OR IGNORE INTO StockItems (ItemCode, Name, Category, Quantity, Unit, MinimumStock, IsSerialised, SupplyType, LastUpdatedUtc) VALUES 
                -- NBN Serialised
                ('NBN-ODU', 'Outdoor Unit (ODU)', 'Hardware', 0, 'Each', 5, 1, 'NbnSupplied', datetime('now')),
                ('NBN-IDU', 'Indoor Unit (IDU)', 'Hardware', 0, 'Each', 5, 1, 'NbnSupplied', datetime('now')),
                
                -- NBN Cable Rolls
                ('CAB-CAT5E', 'Cat 5e Cable Roll', 'Cabling', 0, 'Roll', 2, 0, 'NbnSupplied', datetime('now')),
                ('CAB-CAT6', 'Cat 6 Cable Roll', 'Cabling', 0, 'Roll', 2, 0, 'NbnSupplied', datetime('now')),
                
                -- NBN Wallplates
                ('WP-CAT5E', 'Cat 5e Wallplate', 'Hardware', 0, 'Each', 10, 0, 'NbnSupplied', datetime('now')),
                ('WP-CAT6', 'Cat 6 Wallplate', 'Hardware', 0, 'Each', 10, 0, 'NbnSupplied', datetime('now')),
                
                -- NBN Mounts
                ('MNT-VF', 'Vertical/Fascia Mount', 'Mounts', 0, 'Each', 5, 0, 'NbnSupplied', datetime('now')),
                ('MNT-VF-EXT', 'Vertical/Fascia Extended', 'Mounts', 0, 'Each', 5, 0, 'NbnSupplied', datetime('now')),
                ('MNT-1M', '1m Tin Mount', 'Mounts', 0, 'Each', 2, 0, 'NbnSupplied', datetime('now')),
                ('MNT-2M', '2m Tin Mount', 'Mounts', 0, 'Each', 2, 0, 'NbnSupplied', datetime('now')),
                ('MNT-3M', '3m Tin Mount', 'Mounts', 0, 'Each', 2, 0, 'NbnSupplied', datetime('now')),
                ('MNT-GUT', 'Gutter Mount', 'Mounts', 0, 'Each', 5, 0, 'NbnSupplied', datetime('now')),
                ('MNT-BKT', 'ODU Mounting Bracket', 'Mounts', 0, 'Each', 10, 0, 'NbnSupplied', datetime('now')),

                -- Tech Supplied (Consumables)
                ('TECH-COND', 'Conduit (Length)', 'Consumables', 0, 'Length', 5, 0, 'TechSupplied', datetime('now')),
                ('TECH-FIT', 'Conduit Fittings', 'Consumables', 0, 'Each', 20, 0, 'TechSupplied', datetime('now'));
            ";

            using (var command = new SqliteCommand(seedSql, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}