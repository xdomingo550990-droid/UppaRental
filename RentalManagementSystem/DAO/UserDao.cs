using MySql.Data.MySqlClient;
using RentalManagementSystem.Model;
using RentalManagementSystem.Services;
using System;

namespace RentalManagementSystem.DAO
{
    /// <summary>Profile reads and updates for the <c>users</c> table.</summary>
    public static class UserDao
    {
        /// <summary>Loads one user's profile fields (name, email, phone), or null when not found.</summary>
        public static User? GetById(int userId)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                "SELECT user_id, first_name, last_name, email_address, phone FROM users WHERE user_id = @id LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@id", userId);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            var u = new User { UserId = userId };
            u.FirstName = r.IsDBNull(r.GetOrdinal("first_name")) ? "" : r.GetString("first_name");
            u.LastName = r.IsDBNull(r.GetOrdinal("last_name")) ? "" : r.GetString("last_name");
            u.EmailAddress = r.IsDBNull(r.GetOrdinal("email_address")) ? "" : r.GetString("email_address");
            u.Phone = r.IsDBNull(r.GetOrdinal("phone")) ? "" : r.GetString("phone");
            return u;
        }

        /// <summary>Saves name, email and phone. Also keeps the matching row in <c>renters</c> in step.</summary>
        public static void UpdateProfile(User u)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            using (var cmd = new MySqlCommand(@"
                UPDATE users SET first_name = @first, last_name = @last,
                                 email_address = @email, phone = @phone
                WHERE user_id = @id", conn, tx))
            {
                Bind(cmd, u);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new MySqlCommand(@"
                UPDATE renters SET first_name = @first, last_name = @last,
                                   email_address = @email, phone = @phone
                WHERE user_id = @id", conn, tx))
            {
                Bind(cmd, u);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }

        private static void Bind(MySqlCommand cmd, User u)
        {
            cmd.Parameters.AddWithValue("@first", u.FirstName ?? "");
            cmd.Parameters.AddWithValue("@last", u.LastName ?? "");
            cmd.Parameters.AddWithValue("@email", u.EmailAddress ?? "");
            cmd.Parameters.AddWithValue("@phone", u.Phone ?? "");
            cmd.Parameters.AddWithValue("@id", u.UserId);
        }
    }
}
