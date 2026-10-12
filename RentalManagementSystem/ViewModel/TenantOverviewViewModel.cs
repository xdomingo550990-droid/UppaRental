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
        private string _filter = "All";
        private string _searchQuery = "";
        private ObservableCollection<PropertySection> _sections;
        private List<Property> _allProperties = new();
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
                    ApplyFilters();
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
                    ApplyFilters();
                }
            }
        }

        public TenantOverviewViewModel(User user = null)
        {
            _currentUser = user;
            Sections = new ObservableCollection<PropertySection>();
            LoadSections();
        }

        /// <summary>Reloads the properties from the database, then applies the current chip and search.</summary>
        public void LoadSections()
        {
            _allProperties = LoadPropertiesFromDatabase();
            ApplyFilters();
        }

        /// <summary>Filters the already-loaded list (no database call), so typing stays fast.</summary>
        private void ApplyFilters()
        {
            IEnumerable<Property> pool = _allProperties;

            string q = SearchQuery?.Trim() ?? "";
            if (q.Length > 0)
            {
                pool = pool.Where(p =>
                    Has(p.Name, q) ||
                    Has(p.Location, q) ||
                    Has(p.Description, q) ||
                    Has(p.Status, q) ||
                    Has(p.PropertyType.ToString(), q) ||
                    Has(p.MonthlyRent.ToString(), q));
            }

            var builtSections = BuildSections(pool.ToList(), Filter);

            Sections.Clear();
            foreach (var section in builtSections)
                Sections.Add(section);
        }

        private List<Property> LoadPropertiesFromDatabase()
        {
            var properties = new List<Property>();

            string query = @"SELECT name, location, number_of_rooms, number_of_floors, number_of_bathrooms,
                                    maximum_capacity, security_deposit, monthly_rent, description, status, property_type,
                                    photo_paths
                             FROM properties
                             WHERE IFNULL(status, '') <> 'Draft'
                             ORDER BY property_id DESC";

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
                                PhotoPaths = GetDbString(reader, "photo_paths")
                                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .Where(s => s.Length > 0)
                                    .ToList(),
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
                MessageBox.Show($"Database Error loading properties: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return properties;
        }

        private static List<PropertySection> BuildSections(IEnumerable<Property> properties, string filter)
        {
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