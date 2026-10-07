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
    public class TenantDashboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- Navigation Events ---
        public event EventHandler? OnLogoutRequested;

        // --- Active Model Context ---
        private Model.User _user;
        public Model.User User
        {
            get => _user;
            set
            {
                _user = value;
                OnPropertyChanged();
                RefreshAllProperties();
            }
        }

        // --- Dynamic View & Layout State ---
        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        private string _currentPageTitle = "Overview";
        public string CurrentPageTitle
        {
            get => _currentPageTitle;
            set
            {
                _currentPageTitle = value;
                OnPropertyChanged();
            }
        }

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
            }
        }

        private bool _isSidebarCollapsed;
        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set
            {
                _isSidebarCollapsed = value;
                OnPropertyChanged();
                SidebarWidth = value ? 68 : 260;
            }
        }

        private double _sidebarWidth = 260;
        public double SidebarWidth
        {
            get => _sidebarWidth;
            set
            {
                _sidebarWidth = value;
                OnPropertyChanged();
            }
        }

        // --- Dashboard Data Summary ---
        private int _activeReservationsCount;
        public int ActiveReservationsCount
        {
            get => _activeReservationsCount;
            set
            {
                _activeReservationsCount = value;
                OnPropertyChanged();
            }
        }

        private decimal _totalPendingAmount;
        public decimal TotalPendingAmount
        {
            get => _totalPendingAmount;
            set
            {
                _totalPendingAmount = value;
                OnPropertyChanged();
            }
        }

        // --- Wrapped User Model Properties for WPF Binding ---

        public int UserId
        {
            get => _user?.getUserId() ?? 0;
            set
            {
                if (_user != null && _user.getUserId() != value)
                {
                    _user.setUserId(value);
                    OnPropertyChanged();
                }
            }
        }

        public string FullName
        {
            get => _user?.getFullName() ?? string.Empty;
            set
            {
                if (_user != null)
                {
                    _user.setFullName(value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(UserInitials));
                }
            }
        }

        public string Username
        {
            get => _user?.getUsername() ?? string.Empty;
            set
            {
                if (_user != null && _user.getUsername() != value)
                {
                    _user.setUsername(value);
                    OnPropertyChanged();
                }
            }
        }

        public Role Role
        {
            get => _user != null ? _user.getRole() : Role.Tenant;
            set
            {
                if (_user != null)
                {
                    _user.setRole(value);
                    OnPropertyChanged();
                }
            }
        }

        public string EmailAddress
        {
            get => _user?.getEmailAddress() ?? string.Empty;
            set
            {
                if (_user != null && _user.getEmailAddress() != value)
                {
                    _user.setEmailAddress(value);
                    OnPropertyChanged();
                }
            }
        }

        public string UserInitials
        {
            get
            {
                string name = FullName;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name[0].ToString().ToUpper();
                }
                return "T";
            }
        }

        // --- Commands ---
        public ICommand NavigateCommand { get; }
        public ICommand ToggleSidebarCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand RefreshDashboardCommand { get; }

        // --- Constructors ---
        public TenantDashboardViewModel()
        {
            _user = new Model.User();

            NavigateCommand = new RelayCommand(ExecuteNavigate);
            ToggleSidebarCommand = new RelayCommand(ExecuteToggleSidebar);
            LogoutCommand = new RelayCommand(ExecuteLogout);
            RefreshDashboardCommand = new RelayCommand(ExecuteRefreshDashboard);
        }

        public TenantDashboardViewModel(Model.User loggedInUser) : this()
        {
            User = loggedInUser;
            LoadTenantDataFromDatabase();
            LoadDashboardStatsFromDatabase();
        }

        public TenantDashboardViewModel(int userId) : this()
        {
            LoadTenantById(userId);
            LoadDashboardStatsFromDatabase();
        }

        // --- Navigation & Toggle Handlers ---
        private void ExecuteNavigate(object? parameter)
        {
            if (parameter is not string destination) return;

            switch (destination.ToLower())
            {
                case "overview":
                    CurrentPageTitle = "Overview";
                    break;
                case "reservations":
                    CurrentPageTitle = "My Reservations";
                    break;
                case "billing":
                    CurrentPageTitle = "Billing and Payments";
                    break;
                case "profile":
                    CurrentPageTitle = "My Profile";
                    break;
            }
        }

        private void ExecuteToggleSidebar(object? parameter)
        {
            IsSidebarCollapsed = !IsSidebarCollapsed;
        }

        private void ExecuteLogout(object? parameter)
        {
            OnLogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteRefreshDashboard(object? parameter)
        {
            LoadTenantDataFromDatabase();
            LoadDashboardStatsFromDatabase();
        }

        // --- Helper & Database Methods ---
        public void RefreshAllProperties()
        {
            OnPropertyChanged(nameof(UserId));
            OnPropertyChanged(nameof(FullName));
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(Role));
            OnPropertyChanged(nameof(EmailAddress));
            OnPropertyChanged(nameof(UserInitials));
        }

        public void LoadTenantById(int userId)
        {
            string query = "SELECT user_id, username, password, role, full_name, email, created_at FROM users WHERE user_id = @userId LIMIT 1";

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var loadedUser = new Model.User();
                                loadedUser.setUserId(reader.GetInt32("user_id"));
                                loadedUser.setUsername(reader.IsDBNull(reader.GetOrdinal("username")) ? "" : reader.GetString("username"));
                                loadedUser.setPassword(reader.IsDBNull(reader.GetOrdinal("password")) ? "" : reader.GetString("password"));

                                string roleStr = reader.IsDBNull(reader.GetOrdinal("role")) ? "Tenant" : reader.GetString("role");
                                if (Enum.TryParse<Role>(roleStr, true, out var parsedRole))
                                {
                                    loadedUser.setRole(parsedRole);
                                }

                                loadedUser.setFullName(reader.IsDBNull(reader.GetOrdinal("full_name")) ? "" : reader.GetString("full_name"));
                                loadedUser.setEmailAddress(reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString("email"));

                                if (!reader.IsDBNull(reader.GetOrdinal("created_at")))
                                {
                                    loadedUser.setCreatedAt(reader.GetDateTime("created_at"));
                                }

                                User = loadedUser;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database Error: {ex.Message}", "Error Loading Tenant", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadTenantDataFromDatabase()
        {
            if (UserId <= 0) return;

            string query = "SELECT user_id, username, password, role, full_name, email, created_at FROM users WHERE user_id = @userId";

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", UserId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _user.setUsername(reader.IsDBNull(reader.GetOrdinal("username")) ? "" : reader.GetString("username"));
                                _user.setFullName(reader.IsDBNull(reader.GetOrdinal("full_name")) ? "" : reader.GetString("full_name"));
                                _user.setEmailAddress(reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString("email"));

                                RefreshAllProperties();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load profile: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadDashboardStatsFromDatabase()
        {
            if (UserId <= 0) return;

            string reservationsQuery = "SELECT COUNT(*) FROM reservations WHERE user_id = @userId AND status = 'Active'";
            string billingQuery = "SELECT IFNULL(SUM(amount), 0) FROM bills WHERE user_id = @userId AND status = 'Pending'";

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand(reservationsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", UserId);
                        ActiveReservationsCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    using (var cmd = new MySqlCommand(billingQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", UserId);
                        TotalPendingAmount = Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard statistics: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- RelayCommand Inner Class ---
        public class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

            public void Execute(object? parameter) => _execute(parameter);

            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}