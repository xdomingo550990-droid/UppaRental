using MySql.Data.MySqlClient;
using RentalManagementSystem.Presentation;
using RentalManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace RentalManagementSystem.DAO
{
    /// <summary>Reads and updates the <c>reservations</c> table for the tenant's My Reservations page.</summary>
    public static class ReservationDao
    {
        /// <summary>All reservations of one tenant, newest move-in first.</summary>
        public static List<ReservationRow> GetByUser(int userId)
        {
            var list = new List<ReservationRow>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            const string sql = @"
                SELECT r.reservation_id, p.name AS unit_name, r.start_date, r.rental_duration,
                       r.total_rent, r.down_payment, r.status
                FROM reservations r
                LEFT JOIN properties p ON p.property_id = r.property_id
                WHERE r.user_id = @userId OR r.renter_id = @userId
                ORDER BY r.start_date DESC, r.reservation_id DESC";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            using DbDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                int id = r.GetInt32(r.GetOrdinal("reservation_id"));
                list.Add(new ReservationRow
                {
                    ReservationId = id,
                    ReservationNo = "RES-" + id.ToString("D4"),
                    UnitLabel = GetString(r, "unit_name", "Unit"),
                    MoveInDate = r.IsDBNull(r.GetOrdinal("start_date")) ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("start_date")),
                    TermMonths = r.IsDBNull(r.GetOrdinal("rental_duration")) ? 0 : r.GetInt32(r.GetOrdinal("rental_duration")),
                    TotalRent = r.IsDBNull(r.GetOrdinal("total_rent")) ? 0m : r.GetDecimal(r.GetOrdinal("total_rent")),
                    DownpaymentAmount = r.IsDBNull(r.GetOrdinal("down_payment")) ? 0m : r.GetDecimal(r.GetOrdinal("down_payment")),
                    Status = GetString(r, "status", "Pending")
                });
            }
            return list;
        }

        /// <summary>Sets a reservation's status (Pending, Confirmed, Cancelled).</summary>
        public static void UpdateStatus(int reservationId, string status)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                "UPDATE reservations SET status = @status WHERE reservation_id = @id", conn);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", reservationId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Confirms a Pending reservation and records the downpayment in <c>payments</c>, in one transaction.
        /// Returns the receipt number. Throws if the reservation is no longer Pending.
        /// </summary>
        public static string PayDownpayment(int reservationId, int userId, decimal amount)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            int changed;
            using (var cmd = new MySqlCommand(
                "UPDATE reservations SET status = 'Confirmed' WHERE reservation_id = @id AND status = 'Pending'", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", reservationId);
                changed = cmd.ExecuteNonQuery();
            }

            if (changed == 0)
            {
                tx.Rollback();
                throw new InvalidOperationException("This reservation is no longer pending.");
            }

            long paymentId;
            using (var cmd = new MySqlCommand(@"
                INSERT INTO payments (user_id, reservation_id, payment_date, amount, payment_method, reference_number, invoice_number, period)
                VALUES (@userId, @resId, @date, @amount, 'Online payment', @reference, '', 'Downpayment')", conn, tx))
            {
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@resId", reservationId);
                cmd.Parameters.AddWithValue("@date", DateTime.Now);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@reference", "REF-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                cmd.ExecuteNonQuery();
                paymentId = cmd.LastInsertedId;
            }

            string receipt = "RCT-" + paymentId.ToString("D4");
            using (var cmd = new MySqlCommand(
                "UPDATE payments SET receipt_number = @receipt WHERE payment_id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@receipt", receipt);
                cmd.Parameters.AddWithValue("@id", paymentId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
            return receipt;
        }

        private static string GetString(DbDataReader r, string column, string fallback)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? fallback : r.GetString(i);
        }
    }
}
