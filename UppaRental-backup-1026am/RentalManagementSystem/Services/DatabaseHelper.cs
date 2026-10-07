using MySql.Data.MySqlClient;
using System;

namespace RentalManagementSystem.Services
{
    public static class DatabaseHelper
    {
        // Connection string for MariaDB 10.4.32 on local Apache/XAMPP setup
        private static readonly string ConnectionString =
         "Server=localhost;Port=3306;Database=rental_db;Uid=root;Pwd=;";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public static bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    return conn.State == System.Data.ConnectionState.Open;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Connection failed: {ex.Message}");
                return false;
            }
        }
    }
}