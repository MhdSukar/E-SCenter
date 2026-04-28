using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ESCenter.Models;
using ESCenter.Services;

namespace ESCenter.Data
{
    public class CustomerRepository
    {
        private string ConnectionString => $"Data Source={DatabasePathService.CurrentDatabasePath};Version=3;";

        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(ConnectionString);
        }

        public List<CustomerModel> GetAll()
        {
            var list = new List<CustomerModel>();

            using var conn = GetConnection();
            conn.Open();

            const string sql = "SELECT * FROM Customers ORDER BY FullName ASC;";
            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(MapCustomer(reader));
            }

            return list;
        }

        public Task<List<CustomerModel>> GetAllAsync()
            => Task.Run(GetAll);

        public List<CustomerModel> Search(string query)
        {
            var list = new List<CustomerModel>();
            var trimmedQuery = query?.Trim() ?? string.Empty;

            using var conn = GetConnection();
            conn.Open();

            const string sql = @"
                SELECT * FROM Customers
                WHERE FullName LIKE @q OR PhoneNumber LIKE @q OR Email LIKE @q
                ORDER BY FullName ASC LIMIT 20;";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@q", $"%{trimmedQuery}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapCustomer(reader));
            }

            return list;
        }

        public Task<List<CustomerModel>> SearchAsync(string query)
            => Task.Run(() => Search(query));

        public int Insert(CustomerModel c)
        {
            using var conn = GetConnection();
            conn.Open();

            const string sql = @"
                INSERT INTO Customers (FullName, PhoneNumber, Email, Address, Notes)
                VALUES (@FullName, @PhoneNumber, @Email, @Address, @Notes);
                SELECT last_insert_rowid();";

            using var cmd = new SQLiteCommand(sql, conn);
            BindParams(cmd, c);

            var id = Convert.ToInt32(cmd.ExecuteScalar());
            c.CustomerId = id;
            return id;
        }

        public Task<int> InsertAsync(CustomerModel c)
            => Task.Run(() => Insert(c));

        public void Update(CustomerModel c)
        {
            using var conn = GetConnection();
            conn.Open();

            const string sql = @"
                UPDATE Customers
                SET FullName=@FullName,
                    PhoneNumber=@PhoneNumber,
                    Email=@Email,
                    Address=@Address,
                    Notes=@Notes
                WHERE CustomerId=@id;";

            using var cmd = new SQLiteCommand(sql, conn);
            BindParams(cmd, c);
            cmd.Parameters.AddWithValue("@id", c.CustomerId);
            cmd.ExecuteNonQuery();
        }

        public Task UpdateAsync(CustomerModel c)
            => Task.Run(() => Update(c));

        public void Delete(int customerId)
        {
            using var conn = GetConnection();
            conn.Open();

            const string sql = "DELETE FROM Customers WHERE CustomerId=@id;";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", customerId);
            cmd.ExecuteNonQuery();
        }

        public Task DeleteAsync(int customerId)
            => Task.Run(() => Delete(customerId));

        private static void BindParams(SQLiteCommand cmd, CustomerModel customer)
        {
            cmd.Parameters.AddWithValue("@FullName", customer.FullName);
            cmd.Parameters.AddWithValue("@PhoneNumber", customer.PhoneNumber);
            cmd.Parameters.AddWithValue("@Email", customer.Email);
            cmd.Parameters.AddWithValue("@Address", customer.Address);
            cmd.Parameters.AddWithValue("@Notes", customer.Notes);
        }

        private static CustomerModel MapCustomer(SQLiteDataReader reader)
        {
            return new CustomerModel
            {
                CustomerId = Convert.ToInt32(reader["CustomerId"]),
                FullName = reader["FullName"]?.ToString() ?? string.Empty,
                PhoneNumber = reader["PhoneNumber"]?.ToString() ?? string.Empty,
                Email = reader["Email"]?.ToString() ?? string.Empty,
                Address = reader["Address"]?.ToString() ?? string.Empty,
                Notes = reader["Notes"]?.ToString() ?? string.Empty,
                CreatedAt = reader["CreatedAt"]?.ToString() ?? string.Empty
            };
        }
    }
}
