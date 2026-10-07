using MySql.Data.MySqlClient;
using RentalManagementSystem.Model;
using RentalManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace RentalManagementSystem.ViewModel
{
    public class PropertySection
    {

        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public Visibility DividerVisibility { get; set; } = Visibility.Visible;
        public List<Property> Items { get; set; } = new();
    }

    public class TenantOverviewViewModel : INotifyPropertyChanged
    {
        private const int MaxPerSection = 4;

        private string _filter = "All";
        private string _searchQuery = "";
        private ObservableCollection<PropertySection> _sections;
        private User _currentUser;

        public ObservableCollection<PropertySection> Sections
        {
            get => _sections;
            set
            {
                _sections = value;
                OnPropertyChanged();
            }
        }

        public string Filter
        {
            get => _filter;
            set
            {
                if (_filter != value)
                {
                    _filter = value;
                    OnPropertyChanged();
                    LoadSections();
                }
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnPropertyChanged();
                    LoadSections();
                }
            }
        }
        public void LoadPropertiesFromDb()
        {
            var allProperties = new List<Property>();

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT * FROM properties ORDER BY property_id DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var p = new Property
                            {
                                PropertyId = Convert.ToInt32(reader["property_id"]),
                                LandlordId = reader["landlord_id"] != DBNull.Value ? Convert.ToInt32(reader["landlord_id"]) : (int?)null,
                                Name = reader["name"]?.ToString() ?? "",
                                Location = reader["location"]?.ToString() ?? "",
                                NumberOfRooms = reader["number_of_rooms"] != DBNull.Value ? Convert.ToInt32(reader["number_of_rooms"]) : 1,
                                NumberOfFloors = reader["number_of_floors"] != DBNull.Value ? Convert.ToInt32(reader["number_of_floors"]) : 1,
                                NumberofBathrooms = reader["number_of_bathrooms"] != DBNull.Value ? Convert.ToInt32(reader["number_of_bathrooms"]) : 1,
                                MaximumCapacity = reader["maximum_capacity"] != DBNull.Value ? Convert.ToInt32(reader["maximum_capacity"]) : 1,
                                SizeUnit = reader["size_unit"] != DBNull.Value ? Convert.ToInt32(reader["size_unit"]) : 0,

                                // Converted using Convert.ToInt32 to match Property model int types
                                SecurityDeposit = reader["security_deposit"] != DBNull.Value ? Convert.ToInt32(Convert.ToDecimal(reader["security_deposit"])) : 0,
                                MonthlyRent = reader["monthly_rent"] != DBNull.Value ? Convert.ToInt32(Convert.ToDecimal(reader["monthly_rent"])) : 0,

                                isElectricIncluded = reader["is_electric_included"] != DBNull.Value && Convert.ToBoolean(reader["is_electric_included"]),
                                ElectricBill = reader["electric_bill"] != DBNull.Value ? Convert.ToInt32(Convert.ToDecimal(reader["electric_bill"])) : 0,

                                isWaterIncluded = reader["is_water_included"] != DBNull.Value && Convert.ToBoolean(reader["is_water_included"]),
                                WaterBill = reader["water_bill"] != DBNull.Value ? Convert.ToInt32(Convert.ToDecimal(reader["water_bill"])) : 0,

                                isWifiIncluded = reader["is_wifi_included"] != DBNull.Value && Convert.ToBoolean(reader["is_wifi_included"]),
                                WifiBill = reader["wifi_bill"] != DBNull.Value ? Convert.ToInt32(Convert.ToDecimal(reader["wifi_bill"])) : 0,

                                Description = reader["description"]?.ToString() ?? "",
                                Notes = reader["notes"]?.ToString() ?? "",
                                Status = reader["status"]?.ToString() ?? "Available"
                            };

                            allProperties.Add(p);
                        }
                    }
                }

                // Passed all 3 required arguments (properties list, filter string, landlord/user ID)
                string currentFilter = Filter ?? "All";
                int landlordId = _currentUser?.UserId ?? 0; // Or pass 0 if displaying all properties

                BuildSections(allProperties, currentFilter, landlordId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading properties: {ex.Message}");
            }
        }
        

        public TenantOverviewViewModel(User user = null)
        {
            _currentUser = user;
            Sections = new ObservableCollection<PropertySection>();
            LoadSections();
        }

        public void LoadSections()
        {
            List<Property> pool = LoadPropertiesFromDatabase();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                pool = pool.Where(p =>
                    Has(p.Name, SearchQuery) ||
                    Has(p.Location, SearchQuery) ||
                    Has(p.Description, SearchQuery) ||
                    Has(p.Status, SearchQuery) ||
                    Has(p.PropertyType.ToString(), SearchQuery)
                ).ToList();
            }

            int perSection = !string.IsNullOrWhiteSpace(SearchQuery) ? int.MaxValue : MaxPerSection;

            var builtSections = BuildSections(pool, Filter, perSection);

            Sections.Clear();
            foreach (var section in builtSections)
            {
                Sections.Add(section);
            }
        }

        private List<Property> LoadPropertiesFromDatabase()
        {
            var properties = new List<Property>();

            string query = @"SELECT name, location, number_of_rooms, number_of_floors, number_of_bathrooms,
                                    maximum_capacity, security_deposit, monthly_rent, description, status, property_type
                             FROM properties";

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string propertyTypeStr = GetDbString(reader, "property_type");

                            var property = new Property
                            {
                                Name = GetDbString(reader, "name"),
                                Location = GetDbString(reader, "location"),
                                NumberOfRooms = GetDbInt(reader, "number_of_rooms"),
                                NumberOfFloors = GetDbInt(reader, "number_of_floors"),
                                NumberofBathrooms = GetDbInt(reader, "number_of_bathrooms"),
                                MaximumCapacity = GetDbInt(reader, "maximum_capacity"),
                                SecurityDeposit = GetDbInt(reader, "security_deposit"),
                                MonthlyRent = GetDbInt(reader, "monthly_rent"),
                                Description = GetDbString(reader, "description"),
                                Status = GetDbString(reader, "status"),
                                PropertyType = Enum.TryParse<PropertyType>(propertyTypeStr, true, out var parsedType)
                                    ? parsedType
                                    : PropertyType.Studio
                            };

                            properties.Add(property);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Database Error loading properties: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            return properties;
        }

        private static List<PropertySection> BuildSections(IEnumerable<Property> properties, string filter, int landlordId)
        {
            // If the pool is already scoped to the logged-in landlord via ViewModel, 
            // simply filter by the status category:
            var filtered = properties;

            if (!string.Equals(filter, "All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(p => string.Equals(p.Status, filter, StringComparison.OrdinalIgnoreCase));
            }

            return filtered
                .GroupBy(p => string.IsNullOrEmpty(p.Status) ? "Available" : p.Status)
                .Select(g =>
                {
                    var list = g.ToList();
                    return new PropertySection
                    {
                        Title = g.Key,
                        Subtitle = $"{list.Count} {(list.Count == 1 ? "Property" : "Properties")}",
                        DividerVisibility = Visibility.Visible,
                        Items = list
                    };
                })
                .ToList();
        }
        private static bool Has(string text, string query) =>
            !string.IsNullOrEmpty(text) && text.Contains(query, StringComparison.OrdinalIgnoreCase);

        private static string GetDbString(MySqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static int GetDbInt(MySqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}