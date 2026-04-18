using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

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
                EnsureAlBarakaTable(conn)
            };

            return string.Join(Environment.NewLine, reports);
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

            return EnsureTable(conn, tableName, expectedColumns);
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

            return EnsureTable(conn, tableName, expectedColumns);
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
                ["UnitValue2"] = "TEXT",
                ["UnitCode2"] = "REAL DEFAULT 0",
                ["ChipPartNumber"] = "TEXT",
                ["Category"] = "TEXT",
                ["Description"] = "TEXT"
            };

            return EnsureTable(conn, tableName, expectedColumns);
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
