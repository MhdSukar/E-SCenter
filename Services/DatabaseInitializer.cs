using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using ESCenter.Core;

namespace ESCenter.Services
{
    public class DatabaseInitializer
    {
        private readonly string _dbPath;

        public DatabaseInitializer(string databasePath)
        {
            _dbPath = databasePath;
        }

        public string EnsureDatabaseReady()
        {
            EnsureDatabaseExists();
            return EnsureTablesExistAndValid();
        }

        private void EnsureDatabaseExists()
        {
            var directory = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
            }
        }

        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection($"Data Source={_dbPath};Version=3;");
        }

        private string EnsureTablesExistAndValid()
        {
            using var conn = GetConnection();
            conn.Open();

            var reports = new List<string>
            {
                EnsureTicketsTable(conn),
                EnsureTicketStatusHistoryTable(conn),
                EnsurePartsTable(conn),
                EnsureInventoryTable(conn),
                EnsureBoneyardTable(conn),
                EnsureAlBarakaTable(conn),
                EnsurePartCategoriesTable(conn)
            };

            return string.Join(Environment.NewLine, reports);
        }


        private string EnsurePartCategoriesTable(SQLiteConnection conn)
        {
            const string tableName = "PartCategories";

            var expectedColumns = new Dictionary<string, string>
            {
                ["CategoryId"] = "INTEGER PRIMARY KEY AUTOINCREMENT",
                ["CategoryName"] = "TEXT NOT NULL",
                ["ForType"] = "TEXT NOT NULL CHECK(ForType IN ('Parts','Inventory'))"
            };

            var report = EnsureTable(conn, tableName, expectedColumns);

            using var uniqueIndexCmd = new SQLiteCommand("CREATE UNIQUE INDEX IF NOT EXISTS IX_PartCategories_CategoryName_ForType ON PartCategories (CategoryName, ForType);", conn);
            uniqueIndexCmd.ExecuteNonQuery();

            using var insertPartsNormal = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Normal', 'Parts');", conn);
            insertPartsNormal.ExecuteNonQuery();
            using var insertPartsSmd = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('SMD', 'Parts');", conn);
            insertPartsSmd.ExecuteNonQuery();
            using var insertPartsThroughHole = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Through-Hole', 'Parts');", conn);
            insertPartsThroughHole.ExecuteNonQuery();
            using var insertPartsModule = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Module', 'Parts');", conn);
            insertPartsModule.ExecuteNonQuery();
            using var insertPartsConnector = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Connector', 'Parts');", conn);
            insertPartsConnector.ExecuteNonQuery();

            using var insertInvScreen = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Screen', 'Inventory');", conn);
            insertInvScreen.ExecuteNonQuery();
            using var insertInvBattery = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Battery', 'Inventory');", conn);
            insertInvBattery.ExecuteNonQuery();
            using var insertInvSpeaker = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Speaker', 'Inventory');", conn);
            insertInvSpeaker.ExecuteNonQuery();
            using var insertInvCamera = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Camera', 'Inventory');", conn);
            insertInvCamera.ExecuteNonQuery();
            using var insertInvTransformer = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Transformer', 'Inventory');", conn);
            insertInvTransformer.ExecuteNonQuery();
            using var insertInvFlex = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Flex', 'Inventory');", conn);
            insertInvFlex.ExecuteNonQuery();
            using var insertInvBoard = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Board', 'Inventory');", conn);
            insertInvBoard.ExecuteNonQuery();
            using var insertInvAdapter = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Adapter', 'Inventory');", conn);
            insertInvAdapter.ExecuteNonQuery();
            using var insertInvOther = new SQLiteCommand("INSERT OR IGNORE INTO PartCategories (CategoryName, ForType) VALUES ('Other', 'Inventory');", conn);
            insertInvOther.ExecuteNonQuery();

            return report;
        }
        private string EnsureAlBarakaTable(SQLiteConnection conn)
        {
            const string tableName = "AlBaraka";

            var expectedColumns = new Dictionary<string, string>
            {
                ["AlBarakaId"] = "INTEGER PRIMARY KEY AUTOINCREMENT",
                ["Date"] = "TEXT NOT NULL",
                ["ItemName"] = "TEXT NOT NULL",
                ["Price"] = "REAL",
                ["PriceCurrency"] = "TEXT DEFAULT 'S.P'",
                ["Category"] = "TEXT",
                ["Account"] = "TEXT",
                ["CreatedAt"] = "TEXT DEFAULT CURRENT_TIMESTAMP",
                ["UpdatedAt"] = "TEXT DEFAULT CURRENT_TIMESTAMP"
            };

            var report = EnsureTable(conn, tableName, expectedColumns);
            MigratePartsUnitColumns(conn);
            return report;
        }

        private string EnsureTicketsTable(SQLiteConnection conn)
        {
            const string tableName = "TicketsDB";

            var expectedColumns = new Dictionary<string, string>
            {
                ["TicketId"] = "INTEGER PRIMARY KEY AUTOINCREMENT",
                ["EscTicketId"] = "TEXT UNIQUE NOT NULL",
                ["CustomerId"] = "INTEGER",
                ["CustomerName"] = "TEXT NOT NULL",
                ["PhoneNumber"] = "TEXT NOT NULL",
                ["ContactMethod"] = "TEXT NOT NULL",
                ["DeviceCategory"] = "TEXT NOT NULL",
                ["DeviceBrand"] = "TEXT",
                ["DeviceModel"] = "TEXT NOT NULL",
                ["SerialIMEI"] = "TEXT",
                ["DamageHistory"] = "TEXT",
                ["BoardModifications"] = "TEXT",
                ["ProblemDescription"] = "TEXT",
                ["Notes"] = "TEXT",
                ["RepairStatus"] = "TEXT NOT NULL DEFAULT 'Received'",
                ["PriorityLevel"] = "TEXT NOT NULL DEFAULT 'Normal'",
                ["EstimatedCost"] = "REAL",
                ["EstimatedCostCurrency"] = "TEXT DEFAULT 'S.P'",
                ["FinalCost"] = "REAL",
                ["FinalCostCurrency"] = "TEXT DEFAULT 'S.P'",
                ["HasWarranty"] = "INTEGER DEFAULT 0",
                ["WarrantyPeriod"] = "TEXT",
                ["IsWarrantyRepair"] = "INTEGER DEFAULT 0",
                ["RootCause"] = "TEXT",
                ["PartsUsed"] = "TEXT",
                ["ReceiveDate"] = "TEXT NOT NULL",
                ["DeliveryDate"] = "TEXT",
                ["IsReadyForPickup"] = "INTEGER DEFAULT 0",
                ["DeviceChecklistJson"] = "TEXT DEFAULT '{}'",
                ["AccessoriesJson"] = "TEXT DEFAULT '{}'",
                ["CreatedAt"] = "TEXT DEFAULT CURRENT_TIMESTAMP",
                ["UpdatedAt"] = "TEXT DEFAULT CURRENT_TIMESTAMP"
            };

            var report = EnsureTable(conn, tableName, expectedColumns);
            MigratePartsUnitColumns(conn);
            return report;
        }

        private string EnsurePartsTable(SQLiteConnection conn)
        {
            const string tableName = "Parts";
            var expectedColumns = new Dictionary<string, string>
            {
                ["PartId"] = "INTEGER PRIMARY KEY AUTOINCREMENT",
                ["SKU"] = "TEXT",
                ["PartCode"] = "TEXT",
                ["PartType"] = "TEXT NOT NULL",
                ["QuantityOnHand"] = "INTEGER DEFAULT 0",
                ["Price"] = "REAL DEFAULT 0",
                ["PriceCurrency"] = "TEXT DEFAULT 'S.P'",
                ["QualityGrade"] = "INTEGER DEFAULT 3",
                ["LocationShelf"] = "TEXT",
                ["LocationBin"] = "TEXT",
                ["UnitValue1"] = "REAL DEFAULT 0",
                ["UnitCode1"] = "TEXT",
                ["UnitValue2"] = "REAL DEFAULT 0",
                ["UnitCode2"] = "TEXT",
                ["ChipPartNumber"] = "TEXT",
                ["Category"] = "TEXT",
                ["Description"] = "TEXT"
            };

            var report = EnsureTable(conn, tableName, expectedColumns);
            MigratePartsUnitColumns(conn);
            return report;
        }

        private string EnsureTicketStatusHistoryTable(SQLiteConnection conn)
        {
            const string tableName = "TicketStatusHistory";

            var expectedColumns = new Dictionary<string, string>
            {
                ["HistoryId"] = "INTEGER PRIMARY KEY AUTOINCREMENT",
                ["TicketId"] = "INTEGER NOT NULL",
                ["OldStatus"] = "TEXT",
                ["NewStatus"] = "TEXT NOT NULL",
                ["Note"] = "TEXT",
                ["ChangedAt"] = "TEXT NOT NULL"
            };

            return EnsureTable(conn, tableName, expectedColumns);
        }

        private string EnsureInventoryTable(SQLiteConnection conn)
        {
            const string tableName = "Inventory";

            var expectedColumns = new Dictionary<string, string>
            {
                ["InventoryId"] = "INTEGER PRIMARY KEY AUTOINCREMENT",
                ["ItemType"] = "TEXT NOT NULL",
                ["Brand"] = "TEXT",
                ["Model"] = "TEXT",
                ["Variant"] = "TEXT",
                ["Compatibility"] = "TEXT",
                ["Specs"] = "TEXT",
                ["Size"] = "TEXT",
                ["QuantityOnHand"] = "INTEGER DEFAULT 0",
                ["Price"] = "REAL DEFAULT 0",
                ["PriceCurrency"] = "TEXT DEFAULT 'S.P'",
                ["Condition"] = "TEXT",
                ["QualityGrade"] = "INTEGER DEFAULT 3",
                ["Source"] = "TEXT",
                ["LocationBox"] = "TEXT",
                ["Description"] = "TEXT",
                ["Notes"] = "TEXT",
                ["Tags"] = "TEXT"
            };

            return EnsureTable(conn, tableName, expectedColumns);
        }

        private string EnsureBoneyardTable(SQLiteConnection conn)
        {
            const string tableName = "Boneyard";

            var expectedColumns = new Dictionary<string, string>
            {
                ["DeviceId"] = "INTEGER PRIMARY KEY AUTOINCREMENT",
                ["DeviceType"] = "INTEGER NOT NULL",
                ["Brand"] = "TEXT",
                ["Model"] = "TEXT",
                ["Condition"] = "TEXT",
                ["HolderID"] = "TEXT",
                ["Notes"] = "TEXT",
                ["Price"] = "REAL DEFAULT 0",
                ["PriceCurrency"] = "TEXT DEFAULT 'S.P'",
                ["AddedAt"] = "TEXT"
            };

            return EnsureTable(conn, tableName, expectedColumns);
        }

        private string EnsureTable(SQLiteConnection conn, string tableName, Dictionary<string, string> expectedColumns)
        {
            if (!TableExists(conn, tableName))
            {
                CreateTable(conn, tableName, expectedColumns);
                return $"{tableName}: created ({expectedColumns.Count} columns).";
            }

            var existingColumns = GetExistingColumns(conn, tableName);
            var addedColumns = new List<string>();

            foreach (var col in expectedColumns)
            {
                if (!existingColumns.Contains(col.Key))
                {
                    AddColumn(conn, tableName, col.Key, col.Value);
                    addedColumns.Add(col.Key);
                }
            }

            return addedColumns.Count == 0
                ? $"{tableName}: structure is valid."
                : $"{tableName}: fixed missing columns [{string.Join(", ", addedColumns)}].";
        }

        private bool TableExists(SQLiteConnection conn, string tableName)
        {
            using var cmd = new SQLiteCommand(
                "SELECT name FROM sqlite_master WHERE type='table' AND name=@name;", conn);
            cmd.Parameters.AddWithValue("@name", tableName);
            return cmd.ExecuteScalar() != null;
        }

        private HashSet<string> GetExistingColumns(SQLiteConnection conn, string tableName)
        {
            using var cmd = new SQLiteCommand($"PRAGMA table_info({tableName});", conn);
            using var reader = cmd.ExecuteReader();

            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                columns.Add(reader["name"].ToString() ?? string.Empty);
            }

            return columns;
        }


        private void MigratePartsUnitColumns(SQLiteConnection conn)
        {
            try
            {
                var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                using (var pragma = new SQLiteCommand("PRAGMA table_info(Parts);", conn))
                using (var reader = pragma.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader["name"]?.ToString() ?? string.Empty;
                        var type = reader["type"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            typeMap[name] = type.Trim().ToUpperInvariant();
                        }
                    }
                }

                var unitValue2Wrong = typeMap.TryGetValue("UnitValue2", out var unitValue2Type)
                    && unitValue2Type.Contains("TEXT", StringComparison.OrdinalIgnoreCase);
                var unitCode2Wrong = typeMap.TryGetValue("UnitCode2", out var unitCode2Type)
                    && unitCode2Type.Contains("REAL", StringComparison.OrdinalIgnoreCase);

                if (!unitValue2Wrong && !unitCode2Wrong)
                {
                    return;
                }

                using var tx = conn.BeginTransaction();

                using (var renameCmd = new SQLiteCommand("ALTER TABLE Parts RENAME TO Parts_old;", conn, tx))
                {
                    renameCmd.ExecuteNonQuery();
                }

                var expectedColumns = new Dictionary<string, string>
                {
                    ["PartId"] = "INTEGER PRIMARY KEY AUTOINCREMENT",
                    ["SKU"] = "TEXT",
                    ["PartCode"] = "TEXT",
                    ["PartType"] = "TEXT NOT NULL",
                    ["QuantityOnHand"] = "INTEGER DEFAULT 0",
                    ["Price"] = "REAL DEFAULT 0",
                    ["PriceCurrency"] = "TEXT DEFAULT 'S.P'",
                    ["QualityGrade"] = "INTEGER DEFAULT 3",
                    ["LocationShelf"] = "TEXT",
                    ["LocationBin"] = "TEXT",
                    ["UnitValue1"] = "REAL DEFAULT 0",
                    ["UnitCode1"] = "TEXT",
                    ["UnitValue2"] = "REAL DEFAULT 0",
                    ["UnitCode2"] = "TEXT",
                    ["ChipPartNumber"] = "TEXT",
                    ["Category"] = "TEXT",
                    ["Description"] = "TEXT"
                };

                var defs = string.Join(", ", expectedColumns.Select(c => $"{c.Key} {c.Value}"));
                using (var createCmd = new SQLiteCommand($"CREATE TABLE Parts ({defs});", conn, tx))
                {
                    createCmd.ExecuteNonQuery();
                }

                const string copySql = @"
                    INSERT INTO Parts (PartId, SKU, PartCode, PartType, QuantityOnHand, Price, PriceCurrency, QualityGrade, LocationShelf, LocationBin, UnitValue1, UnitCode1, UnitValue2, UnitCode2, ChipPartNumber, Category, Description)
                    SELECT PartId, SKU, PartCode, PartType, QuantityOnHand, Price, PriceCurrency, QualityGrade, LocationShelf, LocationBin, UnitValue1, UnitCode1,
                           CASE
                               WHEN UnitValue2 IS NULL OR TRIM(CAST(UnitValue2 AS TEXT)) = '' THEN 0
                               ELSE CAST(UnitValue2 AS REAL)
                           END AS UnitValue2,
                           CAST(UnitCode2 AS TEXT) AS UnitCode2,
                           ChipPartNumber, Category, Description
                    FROM Parts_old;";

                using (var copyCmd = new SQLiteCommand(copySql, conn, tx))
                {
                    copyCmd.ExecuteNonQuery();
                }

                using (var dropCmd = new SQLiteCommand("DROP TABLE Parts_old;", conn, tx))
                {
                    dropCmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to migrate Parts unit columns: {ex.Message}");
            }
        }

        private void CreateTable(SQLiteConnection conn, string tableName, Dictionary<string, string> columns)
        {
            var columnDefs = string.Join(", ", columns.Select(c => $"{c.Key} {c.Value}"));
            using var cmd = new SQLiteCommand(
                $"CREATE TABLE {tableName} ({columnDefs});", conn);
            cmd.ExecuteNonQuery();
        }

        private void AddColumn(SQLiteConnection conn, string tableName, string columnName, string columnDef)
        {
            using var cmd = new SQLiteCommand(
                $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDef};", conn);
            cmd.ExecuteNonQuery();
        }
    }
}
