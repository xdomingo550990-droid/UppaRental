using MySql.Data.MySqlClient;
using RentalManagementSystem.Model;
using RentalManagementSystem.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace RentalManagementSystem.ViewModel
{
    public class LandlordViewModel : INotifyPropertyChanged
    {
        private Property _newProperty;
        private ObservableCollection<Property> _properties;
        private string _statusMessage;
        private User _currentUser;
        public ObservableCollection<Property> Properties
        {
            get => _properties;
            set { _properties = value; OnPropertyChanged(); }
        }
        public LandlordViewModel(User loggedInUser)
        {
            _currentUser = loggedInUser;
            LoadPropertiesFromDb();
        }

        public Property NewProperty
        {
            get => _newProperty;
            set { _newProperty = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand AddPropertyCommand { get; }
        public ICommand CalculateUtilitiesCommand { get; }

        public LandlordViewModel()
        {
            Properties = new ObservableCollection<Property>();
            NewProperty = new Property();

            AddPropertyCommand = new RelayCommand(_ => AddProperty(), _ => CanAddProperty());
            CalculateUtilitiesCommand = new RelayCommand(_ => CalculateUtilities());

            LoadPropertiesFromDb();
        }

        public void CalculateUtilities()
        {
            if (!NewProperty.isElectricIncluded)
            {
                NewProperty.ElectricBill = (int)Math.Round(NewProperty.CalculateElectricBill());
            }
            else
            {
                NewProperty.ElectricBill = 0;
            }

            if (!NewProperty.isWaterIncluded)
            {
                NewProperty.WaterBill = (int)Math.Round(NewProperty.CalculateWaterBill());
            }
            else
            {
                NewProperty.WaterBill = 0;
            }

            OnPropertyChanged(nameof(NewProperty));
        }

        private bool CanAddProperty()
        {
            // Allow saving as long as NewProperty is instantiated
            return NewProperty != null;
        }

        private void AddProperty()
        {
            CalculateUtilities();

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string sql = @"
                INSERT INTO properties (
                    landlord_id, name, location, number_of_rooms, number_of_floors, number_of_bathrooms,
                    maximum_capacity, size_unit, security_deposit, monthly_rent,
                    is_electric_included, electric_bill,
                    is_water_included, water_bill,
                    is_wifi_included, wifi_bill,
                    property_type, amenities, description, notes, status, created_at
                ) VALUES (
                    @landlordId, @name, @location, @rooms, @floors, @bathrooms,
                    @capacity, @size, @deposit, @rent,
                    @isElectricInc, @electricBill,
                    @isWaterInc, @waterBill,
                    @isWifiInc, @wifiBill,
                    @propertyType, @amenities, @description, @notes, @status, NOW()
                );";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        // Safely assign DBNull.Value for nullable landlord_id
                        cmd.Parameters.AddWithValue("@landlordId", DBNull.Value);

                        cmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(NewProperty.Name) ? "Untitled Property" : NewProperty.Name);
                        cmd.Parameters.AddWithValue("@location", NewProperty.Location ?? "");
                        cmd.Parameters.AddWithValue("@rooms", NewProperty.NumberOfRooms);
                        cmd.Parameters.AddWithValue("@floors", NewProperty.NumberOfFloors);
                        cmd.Parameters.AddWithValue("@bathrooms", NewProperty.NumberofBathrooms);
                        cmd.Parameters.AddWithValue("@capacity", NewProperty.MaximumCapacity);
                        cmd.Parameters.AddWithValue("@size", NewProperty.SizeUnit);
                        cmd.Parameters.AddWithValue("@deposit", NewProperty.SecurityDeposit);
                        cmd.Parameters.AddWithValue("@rent", NewProperty.MonthlyRent);

                        cmd.Parameters.AddWithValue("@isElectricInc", NewProperty.isElectricIncluded);
                        cmd.Parameters.AddWithValue("@electricBill", NewProperty.ElectricBill);

                        cmd.Parameters.AddWithValue("@isWaterInc", NewProperty.isWaterIncluded);
                        cmd.Parameters.AddWithValue("@waterBill", NewProperty.WaterBill);

                        cmd.Parameters.AddWithValue("@isWifiInc", NewProperty.isWifiIncluded);
                        cmd.Parameters.AddWithValue("@wifiBill", NewProperty.WifiBill);

                        cmd.Parameters.AddWithValue("@propertyType", NewProperty.PropertyType.ToString());

                        // Convert List<Amenities> to string safely
                        string amenitiesCsv = NewProperty.Amenities != null && NewProperty.Amenities.Any()
                            ? string.Join(",", NewProperty.Amenities.Select(a => a.ToString()))
                            : "";
                        cmd.Parameters.AddWithValue("@amenities", amenitiesCsv);

                        cmd.Parameters.AddWithValue("@description", NewProperty.Description ?? "");
                        cmd.Parameters.AddWithValue("@notes", NewProperty.Notes ?? "");
                        cmd.Parameters.AddWithValue("@status", string.IsNullOrEmpty(NewProperty.Status) ? "Available" : NewProperty.Status);

                        cmd.ExecuteNonQuery();
                        NewProperty.PropertyId = (int)cmd.LastInsertedId;
                    }
                }

                Properties.Add(NewProperty);
                StatusMessage = $"Property '{NewProperty.Name}' added successfully!";
                MessageBox.Show(StatusMessage, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                NewProperty = new Property(); // Reset form
            }
            catch (Exception ex)
            {
                // Extract inner exception message to expose exact database parameter or execution errors
                string errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                StatusMessage = $"Failed to save property: {errorMessage}";
                MessageBox.Show(StatusMessage, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPropertiesFromDb()
        {
            Properties.Clear();
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
                                Name = reader["name"]?.ToString() ?? "",
                                Location = reader["location"]?.ToString() ?? "",
                                NumberOfRooms = reader["number_of_rooms"] != DBNull.Value ? Convert.ToInt32(reader["number_of_rooms"]) : 1,
                                NumberOfFloors = reader["number_of_floors"] != DBNull.Value ? Convert.ToInt32(reader["number_of_floors"]) : 1,
                                NumberofBathrooms = reader["number_of_bathrooms"] != DBNull.Value ? Convert.ToInt32(reader["number_of_bathrooms"]) : 1,
                                MaximumCapacity = reader["maximum_capacity"] != DBNull.Value ? Convert.ToInt32(reader["maximum_capacity"]) : 1,
                                SizeUnit = reader["size_unit"] != DBNull.Value ? Convert.ToInt32(reader["size_unit"]) : 0,
                                SecurityDeposit = reader["security_deposit"] != DBNull.Value ? Convert.ToInt32(reader["security_deposit"]) : 0,
                                MonthlyRent = Convert.ToInt32(reader["monthly_rent"]),
                                isElectricIncluded = reader["is_electric_included"] != DBNull.Value && Convert.ToBoolean(reader["is_electric_included"]),
                                ElectricBill = reader["electric_bill"] != DBNull.Value ? Convert.ToInt32(reader["electric_bill"]) : 0,
                                isWaterIncluded = reader["is_water_included"] != DBNull.Value && Convert.ToBoolean(reader["is_water_included"]),
                                WaterBill = reader["water_bill"] != DBNull.Value ? Convert.ToInt32(reader["water_bill"]) : 0,
                                isWifiIncluded = reader["is_wifi_included"] != DBNull.Value && Convert.ToBoolean(reader["is_wifi_included"]),
                                WifiBill = reader["wifi_bill"] != DBNull.Value ? Convert.ToInt32(reader["wifi_bill"]) : 0,
                                Description = reader["description"]?.ToString() ?? "",
                                Notes = reader["notes"]?.ToString() ?? "",
                                Status = reader["status"]?.ToString() ?? "Available"
                            };

                            if (Enum.TryParse<PropertyType>(reader["property_type"]?.ToString(), out var pType))
                            {
                                p.PropertyType = pType;
                            }

                            Properties.Add(p);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading properties: {ex.Message}");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Standard ICommand Implementation for MVVM
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}