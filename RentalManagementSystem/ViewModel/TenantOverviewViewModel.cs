using MySql.Data.MySqlClient;
using RentalManagementSystem.Model;
using RentalManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RentalManagementSystem.ViewModel
{
    public class PropertySection
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<Property> Properties { get; set; } = new List<Property>();
    }

    public class TenantOverviewViewModel : INotifyPropertyChanged
    {
        private const int MaxPerSection = 4;

        private string _filter = "All";
        private string _searchQuery = "";
        private ObservableCollection<PropertySection> _sections;
        private User _loggedInUser;

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

        public TenantOverviewViewModel(User user = null)
        {
            _loggedInUser = user;
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

        private IEnumerable<PropertySection> BuildSections(IEnumerable<Property> properties, string filter, int perSection)
        {
            var filtered = properties.AsEnumerable();

            if (!string.Equals(filter, "All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(p =>
                    string.Equals(p.PropertyType.ToString(), filter, StringComparison.OrdinalIgnoreCase));
            }

            return filtered
                .GroupBy(p => p.PropertyType.ToString())
                .Select(g => new PropertySection
                {
                    CategoryName = g.Key,
                    Properties = g.Take(perSection).ToList()
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