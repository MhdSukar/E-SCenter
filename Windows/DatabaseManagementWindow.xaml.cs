using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;

namespace ESCenter.Windows
{
    public partial class DatabaseManagementWindow : Window
    {
        private const string DatabaseFileName = "E-SCenter.sql";
        private readonly string _dbPath;

        public DatabaseManagementWindow()
        {
            InitializeComponent();
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseFileName);
        }

        private void ReceiveDateabases_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureDatabaseExists();
                EnsureTablesExistAndValid();
                System.Windows.MessageBox.Show("Database and tables verified/created successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Database operation failed:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================
        // DATABASE CORE
        // =========================

        private void EnsureDatabaseExists()
        {
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
            }
        }

        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection($"Data Source={_dbPath};Version=3;");
        }

        // =========================
        // TABLE VALIDATION ENTRY
        // =========================

        private void EnsureTablesExistAndValid()
        {
            using var conn = GetConnection();
            conn.Open();

            // Tickets table
            EnsureTicketsTable(conn);

            // Parts inventory table
            EnsurePartsTable(conn);

            // Optional: Repair templates table
            EnsureInventoryTable(conn);

            // Optional: Pickup reminders table
            EnsureBoneyardTable(conn);
        }

        // =========================
        // TABLE DEFINITIONS
        // =========================

        private void EnsureTicketsTable(SQLiteConnection conn)
        {
            const string tableName = "TicketsDB";

            var expectedColumns = new Dictionary<string, string>
            {
                // Primary Key
                ["TicketId"] = "INTEGER PRIMARY KEY AUTOINCREMENT",
                ["EscTicketId"] = "TEXT UNIQUE NOT NULL",

                // Customer Information
                ["CustomerId"] = "INTEGER",
                ["CustomerName"] = "TEXT NOT NULL",
                ["PhoneNumber"] = "TEXT NOT NULL",
                ["ContactMethod"] = "TEXT NOT NULL",

                // Device Information
                ["DeviceCategory"] = "TEXT NOT NULL",
                ["DeviceBrand"] = "TEXT",
                ["DeviceModel"] = "TEXT NOT NULL",
                ["SerialIMEI"] = "TEXT",
                ["DamageHistory"] = "TEXT",
                ["BoardModifications"] = "TEXT",

                // Problem & Notes
                ["ProblemDescription"] = "TEXT",
                ["Notes"] = "TEXT",

                // Status & Priority
                ["RepairStatus"] = "TEXT NOT NULL DEFAULT 'Received'",
                ["PriorityLevel"] = "TEXT NOT NULL DEFAULT 'Normal'",

                // Cost Information
                ["EstimatedCost"] = "REAL",
                ["EstimatedCostCurrency"] = "TEXT DEFAULT 'EUR'",
                ["FinalCost"] = "REAL",
                ["FinalCostCurrency"] = "TEXT DEFAULT 'EUR'",

                // Warranty Information
                ["HasWarranty"] = "INTEGER DEFAULT 0",
                ["WarrantyPeriod"] = "TEXT",
                ["IsWarrantyRepair"] = "INTEGER DEFAULT 0",

                // Root Cause
                ["RootCause"] = "TEXT",

                // Parts Information
                ["PartsUsed"] = "TEXT",

                // Dates & Flags
                ["ReceiveDate"] = "TEXT NOT NULL",
                ["DeliveryDate"] = "TEXT",
                ["IsReadyForPickup"] = "INTEGER DEFAULT 0",

                // JSON Storage
                ["DeviceChecklistJson"] = "TEXT DEFAULT '{}'",
                ["AccessoriesJson"] = "TEXT DEFAULT '{}'",

                // Timestamps
                ["CreatedAt"] = "TEXT DEFAULT CURRENT_TIMESTAMP",
                ["UpdatedAt"] = "TEXT DEFAULT CURRENT_TIMESTAMP"
            };

            EnsureTable(conn, tableName, expectedColumns);
        }



        private void EnsurePartsTable(SQLiteConnection conn)
        {
            const string tableName = "Parts";
            var expectedColumns = new Dictionary<string, string>
            {
                ["PartId"] = "INTEGER PRIMARY KEY AUTOINCREMENT",
                ["SKU"] = "TEXT",
                ["PartCode"] = "TEXT",
                ["PartType"] = "TEXT NOT NULL",
                ["QuantityOnHand"] = "INTEGER DEFAULT 0",
                ["QualityGrade"] = "INTEGER DEFAULT 3",
                ["LocationShelf"] = "TEXT",
                ["LocationBin"] = "TEXT",
                ["UnitValue1"] = "REAL DEFAULT 0",
                ["UnitCode1"] = "TEXT",
                ["UnitValue2"] = "TEXT",
                ["UnitCode2"] = "REAL DEFAULT 0",
                ["ChipPartNumber"] = "TEXT",
                ["Category"] = "TEXT",
                ["Description"] = "TEXT",
            };

            EnsureTable(conn, tableName, expectedColumns);
        }

        private void EnsureInventoryTable(SQLiteConnection conn)
        {
            const string tableName = "Inventory";

            var expectedColumns = new Dictionary<string, string>
            {
                ["InventoryId"] = "INTEGER PRIMARY KEY AUTOINCREMENT",
                ["ItemType"] = "TEXT NOT NULL",       // Screen, Battery, Speaker, Transformer, Camera, Flex, Board, Adapter
                ["Brand"] = "TEXT",
                ["Model"] = "TEXT",
                ["Variant"] = "TEXT",                 // Pro/Max/Plus, Region, Rev, etc
                ["Compatibility"] = "TEXT",           // iPhone X/XR/XS, Galaxy S21, etc
                ["Specs"] = "TEXT",                // mAh for batteries, VA for transformers, W for adapters
                ["Size"] = "TEXT",                    // 6.1\", 65\", 12V 3A, etc
                ["QuantityOnHand"] = "INTEGER DEFAULT 0",
                ["Condition"] = "TEXT",               // New, Pulled, Refurbished, Tested, Dead
                ["QualityGrade"] = "INTEGER DEFAULT 3",
                ["Source"] = "TEXT",                  // Supplier, Donor, Repair job, etc
                ["LocationBox"] = "TEXT",
                ["Description"] = "TEXT",
                ["Notes"] = "TEXT",
                ["Tags"] = "TEXT",                    // JSON or comma-separated
            };

            EnsureTable(conn, tableName, expectedColumns);
        }


        private void EnsureBoneyardTable(SQLiteConnection conn)
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
                ["AddedAt"] = "TEXT"
            };

            EnsureTable(conn, tableName, expectedColumns);
        }

        // =========================
        // CORE TABLE ENGINE
        // =========================

        private void EnsureTable(SQLiteConnection conn, string tableName, Dictionary<string, string> expectedColumns)
        {
            if (!TableExists(conn, tableName))
            {
                CreateTable(conn, tableName, expectedColumns);
                return;
            }

            var existingColumns = GetExistingColumns(conn, tableName);

            foreach (var col in expectedColumns)
            {
                if (!existingColumns.Contains(col.Key))
                {
                    AddColumn(conn, tableName, col.Key, col.Value);
                }
            }
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
                columns.Add(reader["name"].ToString());
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