using MySql.Data.MySqlClient;   // If your project uses MySqlConnector, change this to: using MySqlConnector;
using RentalManagementSystem.Model;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace RentalManagementSystem.DAO
{
    public static class PaymentDao
    {
        // TODO: replace with the same connection string / helper your other DAOs use.
        private const string ConnectionString =
            "Server=127.0.0.1;Port=3306;Database=rental_db;Uid=root;Pwd=;";

        /// <summary>All payments made by one user, newest first.</summary>
        public static List<Payment> GetByUser(int userId)
        {
            var list = new List<Payment>();

            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                SELECT payment_id, user_id, bill_id, reservation_id, payment_date, amount,
                       payment_method, reference_number, receipt_number, invoice_number, period, created_at
                FROM payments
                WHERE user_id = @userId
                ORDER BY payment_date DESC, payment_id DESC";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            using DbDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Payment
                {
                    PaymentId = r.GetInt32(r.GetOrdinal("payment_id")),
                    UserId = r.GetInt32(r.GetOrdinal("user_id")),
                    BillId = GetNullableInt(r, "bill_id"),
                    ReservationId = GetNullableInt(r, "reservation_id"),
                    PaymentDate = r.GetDateTime(r.GetOrdinal("payment_date")),
                    Amount = r.GetDecimal(r.GetOrdinal("amount")),
                    PaymentMethod = GetString(r, "payment_method"),
                    ReferenceNumber = GetString(r, "reference_number"),
                    ReceiptNumber = GetString(r, "receipt_number"),
                    InvoiceNumber = GetString(r, "invoice_number"),
                    Period = GetString(r, "period"),
                    CreatedAt = r.GetDateTime(r.GetOrdinal("created_at"))
                });
            }

            return list;
        }

        /// <summary>
        /// Inserts the payment, generates its receipt number (RCT-0001, ...) and writes
        /// PaymentId / ReceiptNumber back onto the object. Throws on failure.
        /// </summary>
        public static void Insert(Payment p)
        {
            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            const string insertSql = @"
                INSERT INTO payments
                    (user_id, bill_id, reservation_id, payment_date, amount,
                     payment_method, reference_number, invoice_number, period)
                VALUES
                    (@userId, @billId, @reservationId, @date, @amount,
                     @method, @reference, @invoice, @period)";

            using (var cmd = new MySqlCommand(insertSql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@userId", p.UserId);
                cmd.Parameters.AddWithValue("@billId", (object?)p.BillId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@reservationId", (object?)p.ReservationId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@date", p.PaymentDate);
                cmd.Parameters.AddWithValue("@amount", p.Amount);
                cmd.Parameters.AddWithValue("@method", p.PaymentMethod);
                cmd.Parameters.AddWithValue("@reference", p.ReferenceNumber);
                cmd.Parameters.AddWithValue("@invoice", p.InvoiceNumber);
                cmd.Parameters.AddWithValue("@period", p.Period);
                cmd.ExecuteNonQuery();
                p.PaymentId = (int)cmd.LastInsertedId;
            }

            p.ReceiptNumber = "RCT-" + p.PaymentId.ToString("D4");

            using (var cmd = new MySqlCommand(
                "UPDATE payments SET receipt_number = @receipt WHERE payment_id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@receipt", p.ReceiptNumber);
                cmd.Parameters.AddWithValue("@id", p.PaymentId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
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
