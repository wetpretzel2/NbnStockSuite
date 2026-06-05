using Microsoft.Data.Sqlite;

namespace NbnStock.Core.Data;

public static class DatabaseInitialiser
{
    public static string DatabasePath { get; private set; }

    public static void Initialise()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolderPath = Path.Combine(appDataPath, "NbnStock");
        var databaseFilePath = Path.Combine(appFolderPath, "NbnStock.db");
        DatabasePath = databaseFilePath;

        if (!Directory.Exists(appFolderPath)) Directory.CreateDirectory(appFolderPath);

        var connectionString = $"Data Source={databaseFilePath}";
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            // 1. Create Tables
            var createTableSql = @"
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

            var createSerialisedUnitsTableSql = @"
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

            // 2. Seed Default Data 
            SeedDefaultStockItems(connection);

            // 3. Clean up any existing 'S' prefixes from earlier scans
            ScrubExistingSerialNumbers(connection);
        }
    }

    private static void SeedDefaultStockItems(SqliteConnection connection)
    {
        var checkSql = "SELECT COUNT(*) FROM StockItems";
        using (var command = new SqliteCommand(checkSql, connection))
        {
            var count = (long)command.ExecuteScalar();
            if (count > 0) return;
        }

        var seedSql = @"
                INSERT OR IGNORE INTO StockItems (ItemCode, Name, Category, Quantity, Unit, MinimumStock, IsSerialised, SupplyType, LastUpdatedUtc) VALUES 
                ('NBN-ODU', 'Outdoor Unit (ODU)', 'Hardware', 0, 'Each', 5, 1, 'NbnSupplied', datetime('now')),
                ('NBN-IDU', 'Indoor Unit (IDU)', 'Hardware', 0, 'Each', 5, 1, 'NbnSupplied', datetime('now')),
                ('CAB-CAT5E', 'Cat 5e Cable Roll', 'Cabling', 0, 'Roll', 2, 0, 'NbnSupplied', datetime('now')),
                ('CAB-CAT6', 'Cat 6 Cable Roll', 'Cabling', 0, 'Roll', 2, 0, 'NbnSupplied', datetime('now')),
                ('WP-CAT5E', 'Cat 5e Wallplate', 'Hardware', 0, 'Each', 10, 0, 'NbnSupplied', datetime('now')),
                ('WP-CAT6', 'Cat 6 Wallplate', 'Hardware', 0, 'Each', 10, 0, 'NbnSupplied', datetime('now')),
                ('MNT-VF', 'Vertical/Fascia Mount', 'Mounts', 0, 'Each', 5, 0, 'NbnSupplied', datetime('now')),
                ('MNT-VF-EXT', 'Vertical/Fascia Extended', 'Mounts', 0, 'Each', 5, 0, 'NbnSupplied', datetime('now')),
                ('MNT-1M', '1m Tin Mount', 'Mounts', 0, 'Each', 2, 0, 'NbnSupplied', datetime('now')),
                ('MNT-2M', '2m Tin Mount', 'Mounts', 0, 'Each', 2, 0, 'NbnSupplied', datetime('now')),
                ('MNT-3M', '3m Tin Mount', 'Mounts', 0, 'Each', 2, 0, 'NbnSupplied', datetime('now')),
                ('MNT-GUT', 'Gutter Mount', 'Mounts', 0, 'Each', 5, 0, 'NbnSupplied', datetime('now')),
                ('MNT-BKT', 'ODU Mounting Bracket', 'Mounts', 0, 'Each', 10, 0, 'NbnSupplied', datetime('now')),
                ('TECH-COND', 'Conduit (Length)', 'Consumables', 0, 'Length', 5, 0, 'TechSupplied', datetime('now')),
                ('TECH-FIT', 'Conduit Fittings', 'Consumables', 0, 'Each', 20, 0, 'TechSupplied', datetime('now'));
            ";

        using (var command = new SqliteCommand(seedSql, connection))
        {
            command.ExecuteNonQuery();
        }
    }

    private static void ScrubExistingSerialNumbers(SqliteConnection connection)
    {
        var sql = @"UPDATE SerialisedUnits 
                           SET SerialNumber = SUBSTR(SerialNumber, 2) 
                           WHERE SerialNumber LIKE 'S%';";

        using (var command = new SqliteCommand(sql, connection))
        {
            command.ExecuteNonQuery();
        }
    }
}