using MySql.Data.MySqlClient;
using RentalManagementSystem.Presentation;
using RentalManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;

namespace RentalManagementSystem.DAO
{
    /// <summary>
    /// CRUD for the <c>invoices</c> table, mapped to the landlord BillingPage rows.
    /// Each invoice stores the renter's name and, when the renter has a tenant account
    /// (matched by email), that account's user_id so the tenant sees it on their billing page.
    /// </summary>
    public static class InvoiceDao
    {
        private const string DateFormat = "MMM dd, yyyy";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>All invoices, newest due date first.</summary>
        public static List<InvoiceRow> GetAll()
        {
            var list = new List<InvoiceRow>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            const string sql = @"
                SELECT i.invoice_no, i.user_id,
                       COALESCE(NULLIF(i.renter, ''),
                                TRIM(CONCAT(IFNULL(u.first_name, ''), ' ', IFNULL(u.last_name, '')))) AS renter,
                       i.unit, i.period, i.due_date, i.amount, i.status
                FROM invoices i
                LEFT JOIN users u ON u.user_id = i.user_id
                ORDER BY i.due_date DESC, i.invoice_id DESC";

            using var cmd = new MySqlCommand(sql, conn);
            using DbDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new InvoiceRow
                {
                    InvoiceNo = GetString(r, "invoice_no"),
                    UserId = GetNullableInt(r, "user_id"),
                    Renter = GetString(r, "renter"),
                    Unit = GetString(r, "unit"),
                    Period = GetString(r, "period"),
                    DueDate = r.IsDBNull(r.GetOrdinal("due_date"))
                        ? string.Empty
                        : r.GetDateTime(r.GetOrdinal("due_date")).ToString(DateFormat, Inv),
                    Amount = r.GetDecimal(r.GetOrdinal("amount")),
                    Status = GetString(r, "status")
                });
            }
            return list;
        }

        /// <summary>Inserts a new invoice row.</summary>
        public static void Insert(InvoiceRow row)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            const string sql = @"
                INSERT INTO invoices (invoice_no, user_id, renter, unit, period, due_date, amount, status)
                VALUES (@no, @userId, @renter, @unit, @period, @due, @amount, @status)";

            using var cmd = new MySqlCommand(sql, conn);
            BindParameters(cmd, row);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Updates an existing invoice (matched by invoice number).</summary>
        public static void Update(InvoiceRow row)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            const string sql = @"
                UPDATE invoices SET
                    user_id = @userId, renter = @renter, unit = @unit, period = @period,
                    due_date = @due, amount = @amount, status = @status
                WHERE invoice_no = @no";

            using var cmd = new MySqlCommand(sql, conn);
            BindParameters(cmd, row);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Deletes an invoice by number. Returns true when a row was removed.</summary>
        public static bool Delete(string invoiceNo)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                "DELETE FROM invoices WHERE invoice_no = @no", conn);
            cmd.Parameters.AddWithValue("@no", invoiceNo);
            return cmd.ExecuteNonQuery() > 0;
        }

        /// <summary>Marks an invoice as Paid.</summary>
        public static void MarkPaid(string invoiceNo)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                "UPDATE invoices SET status = 'Paid' WHERE invoice_no = @no", conn);
            cmd.Parameters.AddWithValue("@no", invoiceNo);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Generates the next invoice number: highest existing INV-number + 1 (minimum INV-2001).</summary>
        public static string NextInvoiceNo()
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                "SELECT MAX(CAST(SUBSTRING(invoice_no, 5) AS UNSIGNED)) FROM invoices WHERE invoice_no LIKE 'INV-%'", conn);
            object? result = cmd.ExecuteScalar();

            int next = 2001;
            if (result != null && result != DBNull.Value
                && int.TryParse(Convert.ToString(result, Inv), NumberStyles.Integer, Inv, out int max)
                && max >= next)
            {
                next = max + 1;
            }
            return "INV-" + next;
        }

        /// <summary>
        /// Current renters (not archived, not reservations) for the billing dropdown. UserId is the
        /// tenant account whose email matches the renter's email, or null when there is no account.
        /// </summary>
        public static List<RenterOption> GetRenterOptions()
        {
            var list = new List<RenterOption>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            const string sql = @"
                SELECT rr.name, rr.unit,
                       CASE WHEN rr.rental_term = 'Short-Term' THEN rr.rate * rr.days ELSE rr.rate END AS default_rent,
                       (SELECT u.user_id FROM users u
                         WHERE TRIM(rr.email) <> ''
                           AND LOWER(TRIM(u.email_address)) = LOWER(TRIM(rr.email)) COLLATE utf8mb4_general_ci
                         ORDER BY u.user_id LIMIT 1) AS user_id
                FROM renter_records rr
                WHERE rr.is_archived = 0 AND rr.is_reservation = 0 AND rr.status <> 'Past'
                ORDER BY rr.name, rr.unit";

            using var cmd = new MySqlCommand(sql, conn);
            using DbDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new RenterOption
                {
                    Name = GetString(r, "name"),
                    Unit = GetString(r, "unit"),
                    DefaultRent = r.IsDBNull(r.GetOrdinal("default_rent")) ? 0m : r.GetDecimal(r.GetOrdinal("default_rent")),
                    UserId = GetNullableInt(r, "user_id")
                });
            }
            return list;
        }

        // --------------------------------------------------------------------

        private static void BindParameters(MySqlCommand cmd, InvoiceRow row)
        {
            cmd.Parameters.AddWithValue("@no", row.InvoiceNo ?? string.Empty);
            cmd.Parameters.AddWithValue("@userId", (object?)row.UserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@renter", row.Renter ?? string.Empty);
            cmd.Parameters.AddWithValue("@unit", row.Unit ?? string.Empty);
            cmd.Parameters.AddWithValue("@period", row.Period ?? string.Empty);
            cmd.Parameters.AddWithValue("@due",
                DateTime.TryParse(row.DueDate, Inv, DateTimeStyles.None, out var dt)
                    ? dt.Date : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@amount", row.Amount);
            cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(row.Status) ? "Pending" : row.Status);
        }

        private static string GetString(DbDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? string.Empty : r.GetString(i);
        }

        private static int? GetNullableInt(DbDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetInt32(i);
        }
    }
}
