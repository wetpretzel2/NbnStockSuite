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
                    Notes, SupplyType, LastUpdatedUtc)
                    VALUES
                    (@ItemCode, @Name, @Category, @Quantity, @Unit, @MinimumStock,
                    @IsSerialised, @Notes, @SupplyType, @LastUpdatedUtc);
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
                    command.Parameters.AddWithValue("@SupplyType", item.SupplyType.ToString());
                    command.Parameters.AddWithValue("@LastUpdatedUtc", item.LastUpdatedUtc.ToString("o"));

                    command.ExecuteNonQuery();

                }
            }
            
        }
        public List<StockItem> GetAllStockItems()
        {
            var items = new List<StockItem>();

            string connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string sql = @"SELECT Id, ItemCode, Name, Category, Quantity, Unit, MinimumStock, IsSerialised, SupplyType, Notes, LastUpdatedUtc
                       FROM StockItems
                       ORDER BY Name;";

                using (var command = new SqliteCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new StockItem
                        {
                            Id = reader.GetInt32(0),
                            ItemCode = reader.GetString(1),
                            Name = reader.GetString(2),
                            Category = reader.GetString(3),
                            Quantity = reader.GetInt32(4),
                            Unit = reader.GetString(5),
                            MinimumStock = reader.GetInt32(6),
                            IsSerialised = reader.GetInt32(7) == 1,
                            SupplyType = Enum.Parse<SupplyType>(reader.GetString(8)),
                            Notes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                            LastUpdatedUtc = DateTime.Parse(reader.GetString(10))
                        });
                    }
                }
            }

            return items;
        }

        public List<StockItem> GetStockItemsBySupplyType(SupplyType supplyType)
        {
            var items = new List<StockItem>();

            string connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string sql = @"SELECT Id, ItemCode, Name, Category, Quantity, Unit, MinimumStock, IsSerialised, SupplyType, Notes, LastUpdatedUtc
                       FROM StockItems
                       WHERE SupplyType = @SupplyType
                       ORDER BY Name;";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@SupplyType", supplyType.ToString());

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new StockItem
                            {
                                Id = reader.GetInt32(0),
                                ItemCode = reader.GetString(1),
                                Name = reader.GetString(2),
                                Category = reader.GetString(3),
                                Quantity = reader.GetInt32(4),
                                Unit = reader.GetString(5),
                                MinimumStock = reader.GetInt32(6),
                                IsSerialised = reader.GetInt32(7) == 1,
                                SupplyType = Enum.Parse<SupplyType>(reader.GetString(8)),
                                Notes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                LastUpdatedUtc = DateTime.Parse(reader.GetString(10))
                            });
                        }
                    }
                }
            }

            return items;
        }
        public StockItem? GetStockItemById(int id)
        {
            string connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string sql = @"
            SELECT Id, ItemCode, Name, Category, Quantity, Unit, MinimumStock, IsSerialised, SupplyType, Notes, LastUpdatedUtc
            FROM StockItems
            WHERE Id = @Id;
        ";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new StockItem
                            {
                                Id = reader.GetInt32(0),
                                ItemCode = reader.GetString(1),
                                Name = reader.GetString(2),
                                Category = reader.GetString(3),
                                Quantity = reader.GetInt32(4),
                                Unit = reader.GetString(5),
                                MinimumStock = reader.GetInt32(6),
                                IsSerialised = reader.GetInt32(7) == 1,
                                SupplyType = Enum.Parse<SupplyType>(reader.GetString(8)),
                                Notes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                LastUpdatedUtc = DateTime.Parse(reader.GetString(10))
                            };
                        }
                    }
                }
            }

            return null;
        }

        public StockItem? GetStockItemByCode(string itemCode)
        {
            string connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string sql = @"
            SELECT Id, ItemCode, Name, Category, Quantity, Unit, MinimumStock, IsSerialised, SupplyType, Notes, LastUpdatedUtc
            FROM StockItems
            WHERE ItemCode = @ItemCode;
        ";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ItemCode", itemCode);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new StockItem
                            {
                                Id = reader.GetInt32(0),
                                ItemCode = reader.GetString(1),
                                Name = reader.GetString(2),
                                Category = reader.GetString(3),
                                Quantity = reader.GetInt32(4),
                                Unit = reader.GetString(5),
                                MinimumStock = reader.GetInt32(6),
                                IsSerialised = reader.GetInt32(7) == 1,
                                SupplyType = Enum.Parse<SupplyType>(reader.GetString(8)),
                                Notes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                LastUpdatedUtc = DateTime.Parse(reader.GetString(10))
                            };
                        }
                    }
                }
            }

            return null;
        }

        public void UpdateStockQuantity(int id, int quantity)
        {
            string connectionString = $"Data Source={DatabaseInitialiser.DatabasePath}";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                string sql = @"
            UPDATE StockItems
            SET Quantity = @Quantity,
                LastUpdatedUtc = @LastUpdatedUtc
            WHERE Id = @Id;
        ";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Quantity", quantity);
                    command.Parameters.AddWithValue("@LastUpdatedUtc", DateTime.UtcNow.ToString("o"));
                    command.Parameters.AddWithValue("@Id", id);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void AdjustStockQuantity(int id, int changeAmount)
        {
            var item = GetStockItemById(id);

            if (item == null)
            {
                throw new Exception($"Stock item with Id {id} was not found.");
            }

            int newQuantity = item.Quantity + changeAmount;

            if (newQuantity < 0)
            {
                throw new Exception("Stock quantity cannot go below zero.");
            }

            UpdateStockQuantity(id, newQuantity);
        }
        public void ReceiveStock(int id, int quantityReceived)
        {
            if (quantityReceived <= 0)
            {
                throw new Exception("Quantity received must be greater than zero.");
            }

            AdjustStockQuantity(id, quantityReceived);
        }

        public void ConsumeStock(int id, int quantityUsed)
        {
            if (quantityUsed <= 0)
            {
                throw new Exception("Quantity used must be greater than zero.");
            }

            AdjustStockQuantity(id, -quantityUsed);
        }
    }
}
