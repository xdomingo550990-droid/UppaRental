using MySql.Data.MySqlClient;
using RentalManagementSystem.Model;
using RentalManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace RentalManagementSystem.DAO
{
    /// <summary>CRUD for the <c>properties</c> table.</summary>
    public static class PropertyDao
    {
        private const string Columns = @"
            property_id, landlord_id, name, location, number_of_rooms, number_of_floors,
            number_of_bathrooms, maximum_capacity, size_unit, security_deposit, monthly_rent,
            property_type, status, amenities, description, notes, photo_paths,
            is_electric_included, electric_bill, electric_kwh_rate,
            is_water_included, water_bill, water_rate_per_cbm,
            is_wifi_included, wifi_bill, inquiry_count, created_at";

        /// <summary>All properties, newest first.</summary>
        public static List<Property> GetAll()
        {
            var list = new List<Property>();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                $"SELECT {Columns} FROM properties ORDER BY created_at DESC, property_id DESC", conn);

            using DbDataReader r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(Map(r));
            return list;
        }

        /// <summary>Single property by id, or null when not found.</summary>
        public static Property? GetById(int propertyId)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                $"SELECT {Columns} FROM properties WHERE property_id = @id LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@id", propertyId);

            using DbDataReader r = cmd.ExecuteReader();
            return r.Read() ? Map(r) : null;
        }

        /// <summary>Inserts the property and writes back the generated PropertyId.</summary>
        public static void Insert(Property p)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            const string sql = @"
                INSERT INTO properties
                    (landlord_id, name, location, number_of_rooms, number_of_floors,
                     number_of_bathrooms, maximum_capacity, size_unit, security_deposit, monthly_rent,
                     property_type, status, amenities, description, notes, photo_paths,
                     is_electric_included, electric_bill, electric_kwh_rate,
                     is_water_included, water_bill, water_rate_per_cbm,
                     is_wifi_included, wifi_bill, inquiry_count)
                VALUES
                    (@landlordId, @name, @location, @rooms, @floors,
                     @baths, @capacity, @size, @deposit, @rent,
                     @type, @status, @amenities, @description, @notes, @photos,
                     @elecInc, @elecBill, @elecRate,
                     @waterInc, @waterBill, @waterRate,
                     @wifiInc, @wifiBill, @inquiries)";

            using var cmd = new MySqlCommand(sql, conn);
            BindParameters(cmd, p);
            cmd.ExecuteNonQuery();
            p.PropertyId = (int)cmd.LastInsertedId;
        }

        /// <summary>Updates all editable columns of an existing property.</summary>
        public static void Update(Property p)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            const string sql = @"
                UPDATE properties SET
                    landlord_id = @landlordId, name = @name, location = @location,
                    number_of_rooms = @rooms, number_of_floors = @floors,
                    number_of_bathrooms = @baths, maximum_capacity = @capacity,
                    size_unit = @size, security_deposit = @deposit, monthly_rent = @rent,
                    property_type = @type, status = @status, amenities = @amenities,
                    description = @description, notes = @notes, photo_paths = @photos,
                    is_electric_included = @elecInc, electric_bill = @elecBill, electric_kwh_rate = @elecRate,
                    is_water_included = @waterInc, water_bill = @waterBill, water_rate_per_cbm = @waterRate,
                    is_wifi_included = @wifiInc, wifi_bill = @wifiBill, inquiry_count = @inquiries
                WHERE property_id = @id";

            using var cmd = new MySqlCommand(sql, conn);
            BindParameters(cmd, p);
            cmd.Parameters.AddWithValue("@id", p.PropertyId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Deletes a property by id. Returns true when a row was removed.</summary>
        public static bool Delete(int propertyId)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                "DELETE FROM properties WHERE property_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", propertyId);
            return cmd.ExecuteNonQuery() > 0;
        }

        // --------------------------------------------------------------------

        private static void BindParameters(MySqlCommand cmd, Property p)
        {
            cmd.Parameters.AddWithValue("@landlordId", (object?)p.LandlordId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@name", p.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@location", p.Location ?? string.Empty);
            cmd.Parameters.AddWithValue("@rooms", p.NumberOfRooms);
            cmd.Parameters.AddWithValue("@floors", p.NumberOfFloors);
            cmd.Parameters.AddWithValue("@baths", p.NumberofBathrooms);
            cmd.Parameters.AddWithValue("@capacity", p.MaximumCapacity);
            cmd.Parameters.AddWithValue("@size", p.SizeUnit);
            cmd.Parameters.AddWithValue("@deposit", p.SecurityDeposit);
            cmd.Parameters.AddWithValue("@rent", p.MonthlyRent);
            cmd.Parameters.AddWithValue("@type", p.PropertyType.ToString());
            cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(p.Status) ? "Available" : p.Status);
            cmd.Parameters.AddWithValue("@amenities",
                p.Amenities == null ? string.Empty : string.Join(",", p.Amenities.Select(a => a.ToString())));
            cmd.Parameters.AddWithValue("@description", p.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@notes", p.Notes ?? string.Empty);
            cmd.Parameters.AddWithValue("@photos",
                p.PhotoPaths == null ? string.Empty : string.Join("\n", p.PhotoPaths));
            cmd.Parameters.AddWithValue("@elecInc", p.isElectricIncluded);
            cmd.Parameters.AddWithValue("@elecBill", p.ElectricBill);
            cmd.Parameters.AddWithValue("@elecRate", p.ElectricKwhRate);
            cmd.Parameters.AddWithValue("@waterInc", p.isWaterIncluded);
            cmd.Parameters.AddWithValue("@waterBill", p.WaterBill);
            cmd.Parameters.AddWithValue("@waterRate", p.WaterRatePerCubicMeter);
            cmd.Parameters.AddWithValue("@wifiInc", p.isWifiIncluded);
            cmd.Parameters.AddWithValue("@wifiBill", p.WifiBill);
            cmd.Parameters.AddWithValue("@inquiries", p.InquiryCount);
        }

        private static Property Map(DbDataReader r)
        {
            var amenitiesText = GetString(r, "amenities");
            var amenities = new List<Amenities>();
            foreach (var token in amenitiesText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Enum.TryParse(token.Trim(), true, out Amenities a))
                    amenities.Add(a);
            }

            var photosText = GetString(r, "photo_paths");
            var photos = photosText
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            var typeText = GetString(r, "property_type");
            if (!Enum.TryParse(typeText, true, out PropertyType type))
                type = PropertyType.Studio;

            return new Property
            {
                PropertyId = r.GetInt32(r.GetOrdinal("property_id")),
                LandlordId = GetNullableInt(r, "landlord_id"),
                Name = GetString(r, "name"),
                Location = GetString(r, "location"),
                NumberOfRooms = r.GetInt32(r.GetOrdinal("number_of_rooms")),
                NumberOfFloors = r.GetInt32(r.GetOrdinal("number_of_floors")),
                NumberofBathrooms = r.GetInt32(r.GetOrdinal("number_of_bathrooms")),
                MaximumCapacity = r.GetInt32(r.GetOrdinal("maximum_capacity")),
                SizeUnit = r.GetInt32(r.GetOrdinal("size_unit")),
                SecurityDeposit = r.GetInt32(r.GetOrdinal("security_deposit")),
                MonthlyRent = r.GetInt32(r.GetOrdinal("monthly_rent")),
                PropertyType = type,
                Status = GetString(r, "status"),
                Amenities = amenities,
                Description = GetString(r, "description"),
                Notes = GetString(r, "notes"),
                PhotoPaths = photos,
                isElectricIncluded = GetBool(r, "is_electric_included"),
                ElectricBill = r.GetInt32(r.GetOrdinal("electric_bill")),
                ElectricKwhRate = r.GetDouble(r.GetOrdinal("electric_kwh_rate")),
                isWaterIncluded = GetBool(r, "is_water_included"),
                WaterBill = r.GetInt32(r.GetOrdinal("water_bill")),
                WaterRatePerCubicMeter = r.GetDouble(r.GetOrdinal("water_rate_per_cbm")),
                isWifiIncluded = GetBool(r, "is_wifi_included"),
                WifiBill = r.GetInt32(r.GetOrdinal("wifi_bill")),
                InquiryCount = r.GetInt32(r.GetOrdinal("inquiry_count")),
                CreatedAt = r.GetDateTime(r.GetOrdinal("created_at"))
            };
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

        private static int? GetNullableInt(DbDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetInt32(i);
        }
    }
}
