using MySql.Data.MySqlClient;
using RentalManagementSystem.Presentation;
using RentalManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;

namespace RentalManagementSystem.DAO
{
    /// <summary>CRUD for the <c>renter_records</c> table, mapped to the RentersPage rows.</summary>
    public static class RenterDao
    {
        private const string DateFormat = "MMM dd, yyyy";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>All renters + reservations, newest first.</summary>
        public static List<RenterRow> GetAll()
        {
            var list = new List<RenterRow>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            const string sql = @"
                SELECT renter_id, name, unit, contact, email, address,
                       lease_start, lease_end, rental_term, rate, months, days,
                       status, is_reservation, reservation_paid, hold_until,
                       advance_amount, deposit_amount, is_archived, move_out_date, archive_reason
                FROM renter_records
                ORDER BY renter_id DESC";

            using var cmd = new MySqlCommand(sql, conn);
            using DbDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(Map(r));
            }
            return list;
        }

        /// <summary>Unit name to monthly rate lookup, pulled from the properties table.</summary>
        public static Dictionary<string, decimal> GetUnitRates()
        {
            var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            const string sql = "SELECT name, monthly_rent FROM properties WHERE monthly_rent > 0";
            using var cmd = new MySqlCommand(sql, conn);
            using DbDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                string name = GetString(r, "name");
                if (name.Length == 0) continue;
                map[name] = r.GetDecimal(r.GetOrdinal("monthly_rent"));
            }
            return map;
        }

        /// <summary>Inserts a renter/reservation and writes back the generated id.</summary>
        public static void Insert(RenterRow row)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            const string sql = @"
                INSERT INTO renter_records
                    (name, unit, contact, email, address, lease_start, lease_end,
                     rental_term, rate, months, days, status, is_reservation,
                     reservation_paid, hold_until, advance_amount, deposit_amount,
                     is_archived, move_out_date, archive_reason)
                VALUES
                    (@name, @unit, @contact, @email, @address, @start, @end,
                     @term, @rate, @months, @days, @status, @isRes,
                     @resPaid, @holdUntil, @advance, @deposit,
                     @archived, @moveOut, @reason)";

            using var cmd = new MySqlCommand(sql, conn);
            BindParameters(cmd, row);
            cmd.ExecuteNonQuery();
            row.DbId = (int)cmd.LastInsertedId;
        }

        /// <summary>Updates an existing renter matched by its database id.</summary>
        public static void Update(RenterRow row)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            const string sql = @"
                UPDATE renter_records SET
                    name = @name, unit = @unit, contact = @contact, email = @email,
                    address = @address, lease_start = @start, lease_end = @end,
                    rental_term = @term, rate = @rate, months = @months, days = @days,
                    status = @status, is_reservation = @isRes, reservation_paid = @resPaid,
                    hold_until = @holdUntil, advance_amount = @advance, deposit_amount = @deposit,
                    is_archived = @archived, move_out_date = @moveOut, archive_reason = @reason
                WHERE renter_id = @id";

            using var cmd = new MySqlCommand(sql, conn);
            BindParameters(cmd, row);
            cmd.Parameters.AddWithValue("@id", row.DbId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Archives (move-out) a renter matched by its database id.</summary>
        public static void Archive(int renterId, DateTime moveOutDate, string reason)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            const string sql = @"
                UPDATE renter_records SET
                    is_archived = 1, status = 'Past',
                    move_out_date = @moveOut, archive_reason = @reason
                WHERE renter_id = @id";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", renterId);
            cmd.Parameters.AddWithValue("@moveOut", moveOutDate.Date);
            cmd.Parameters.AddWithValue("@reason", reason ?? string.Empty);
            cmd.ExecuteNonQuery();
        }

        // --------------------------------------------------------------------

        private static RenterRow Map(DbDataReader r)
        {
            DateTime? leaseStart = GetNullableDate(r, "lease_start");
            DateTime? leaseEnd = GetNullableDate(r, "lease_end");

            var row = new RenterRow
            {
                DbId = r.GetInt32(r.GetOrdinal("renter_id")),
                Name = GetString(r, "name"),
                Unit = GetString(r, "unit"),
                Contact = GetString(r, "contact"),
                Email = GetString(r, "email"),
                Address = GetString(r, "address"),
                LeaseStart = leaseStart?.ToString(DateFormat, Inv) ?? string.Empty,
                LeaseEnd = leaseEnd?.ToString(DateFormat, Inv) ?? string.Empty,
                RentalTerm = GetString(r, "rental_term"),
                Rate = r.GetDecimal(r.GetOrdinal("rate")),
                Months = r.GetInt32(r.GetOrdinal("months")),
                Days = r.GetInt32(r.GetOrdinal("days")),
                Status = GetString(r, "status"),
                IsReservation = GetBool(r, "is_reservation"),
                ReservationPaid = GetBool(r, "reservation_paid"),
                HoldUntil = GetNullableDate(r, "hold_until"),
                Advance = r.GetDecimal(r.GetOrdinal("advance_amount")),
                Deposit = r.GetDecimal(r.GetOrdinal("deposit_amount")),
                IsArchived = GetBool(r, "is_archived"),
                MoveOutDate = GetNullableDate(r, "move_out_date"),
                ArchiveReason = GetString(r, "archive_reason"),
                MoveInRecorded = !GetBool(r, "is_reservation")
            };
            return row;
        }

        private static void BindParameters(MySqlCommand cmd, RenterRow row)
        {
            cmd.Parameters.AddWithValue("@name", row.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@unit", row.Unit ?? string.Empty);
            cmd.Parameters.AddWithValue("@contact", row.Contact ?? string.Empty);
            cmd.Parameters.AddWithValue("@email", row.Email ?? string.Empty);
            cmd.Parameters.AddWithValue("@address", row.Address ?? string.Empty);
            cmd.Parameters.AddWithValue("@start", ParseDate(row.LeaseStart));
            cmd.Parameters.AddWithValue("@end", ParseDate(row.LeaseEnd));
            cmd.Parameters.AddWithValue("@term",
                string.IsNullOrWhiteSpace(row.RentalTerm) ? RenterRow.LongTerm : row.RentalTerm);
            cmd.Parameters.AddWithValue("@rate", row.Rate);
            cmd.Parameters.AddWithValue("@months", row.Months);
            cmd.Parameters.AddWithValue("@days", row.Days);
            cmd.Parameters.AddWithValue("@status",
                string.IsNullOrWhiteSpace(row.Status) ? "Active" : row.Status);
            cmd.Parameters.AddWithValue("@isRes", row.IsReservation);
            cmd.Parameters.AddWithValue("@resPaid", row.ReservationPaid);
            cmd.Parameters.AddWithValue("@holdUntil",
                row.HoldUntil.HasValue ? row.HoldUntil.Value.Date : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@advance", row.Advance);
            cmd.Parameters.AddWithValue("@deposit", row.Deposit);
            cmd.Parameters.AddWithValue("@archived", row.IsArchived);
            cmd.Parameters.AddWithValue("@moveOut",
                row.MoveOutDate.HasValue ? row.MoveOutDate.Value.Date : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@reason", row.ArchiveReason ?? string.Empty);
        }

        private static object ParseDate(string text)
        {
            return DateTime.TryParse(text, Inv, DateTimeStyles.None, out var dt)
                ? dt.Date : (object)DBNull.Value;
        }

        private static string GetString(DbDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? string.Empty : r.GetString(i);
        }

        private static bool GetBool(DbDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return !r.IsDBNull(i) && Convert.ToBoolean(r.GetValue(i));
        }

        private static DateTime? GetNullableDate(DbDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetDateTime(i);
        }
    }
}
