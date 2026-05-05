using System;
using System.Collections.Generic;
using System.Data.SQLite;
using ESCenter.Services;

namespace ESCenter.Data
{
    public class TypeDefinitionsRepository
    {
        private SQLiteConnection GetConnection() => new($"Data Source={DatabasePathService.CurrentDatabasePath};Version=3;");

        public List<string> GetTypes(string scope)
        {
            var result = new List<string>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SQLiteCommand("SELECT TypeName FROM TypeDefinitions WHERE Scope=@scope ORDER BY TypeName;", conn);
            cmd.Parameters.AddWithValue("@scope", scope);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result.Add(reader["TypeName"]?.ToString() ?? string.Empty);
            return result;
        }

        public void UpsertType(string scope, string typeName, string unit1, string unit2)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SQLiteCommand(@"INSERT INTO TypeDefinitions (Scope,TypeName,Unit1,Unit2)
VALUES (@scope,@type,@unit1,@unit2)
ON CONFLICT(Scope,TypeName) DO UPDATE SET Unit1=@unit1, Unit2=@unit2;", conn);
            cmd.Parameters.AddWithValue("@scope", scope);
            cmd.Parameters.AddWithValue("@type", typeName.Trim());
            cmd.Parameters.AddWithValue("@unit1", unit1?.Trim());
            cmd.Parameters.AddWithValue("@unit2", unit2?.Trim());
            cmd.ExecuteNonQuery();
        }

        public (string Unit1, string Unit2) GetTypeUnits(string scope, string typeName)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SQLiteCommand("SELECT Unit1, Unit2 FROM TypeDefinitions WHERE Scope=@scope AND TypeName=@type LIMIT 1;", conn);
            cmd.Parameters.AddWithValue("@scope", scope);
            cmd.Parameters.AddWithValue("@type", typeName);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return (string.Empty, string.Empty);
            return (r["Unit1"]?.ToString() ?? string.Empty, r["Unit2"]?.ToString() ?? string.Empty);
        }
    }
}
