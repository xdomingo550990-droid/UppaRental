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
    public class UserViewModel : INotifyPropertyChanged
    {
        private Model.User? _currentUser;
        public Model.User? CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
            }
        }

        public bool AuthenticateUser(string username, string password)
        {
            string query = @"SELECT user_id, username, password, role, first_name, last_name, email_address, created_at 
                    FROM users 
                    WHERE username = @username AND password = @password 
                    LIMIT 1";

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);

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

                                loadedUser.setFirstName(reader.IsDBNull(reader.GetOrdinal("first_name")) ? "" : reader.GetString("first_name"));
                                loadedUser.setLastName(reader.IsDBNull(reader.GetOrdinal("last_name")) ? "" : reader.GetString("last_name"));
                                loadedUser.setEmailAddress(reader.IsDBNull(reader.GetOrdinal("email_address")) ? "" : reader.GetString("email_address"));

                                if (!reader.IsDBNull(reader.GetOrdinal("created_at")))
                                {
                                    loadedUser.setCreatedAt(reader.GetDateTime("created_at"));
                                }

                                // Store logged-in user context
                                CurrentUser = loadedUser;
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Authentication Error: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            CurrentUser = null;
            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- Navigation Events ---
        public event EventHandler? OnNextRequested;
        public event EventHandler? OnBackRequested;

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

        // --- Observable Collection for Data Binding ---
        private ObservableCollection<Model.User> _users;
        public ObservableCollection<Model.User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
            }
        }

        // --- Role Selection State ---
        private bool _isRoleSelected;
        public bool IsRoleSelected
        {
            get => _isRoleSelected;
            set
            {
                _isRoleSelected = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // --- Selected User in UI ---
        private Model.User? _selectedUser;
        public Model.User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
                if (_selectedUser != null)
                {
                    User = _selectedUser;
                    IsRoleSelected = true;
                }
            }
        }

        // --- Wrapped Model Properties for WPF Binding ---

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

        public string FirstName
        {
            get => _user?.getFirstName() ?? string.Empty;
            set
            {
                if (_user != null && _user.getFirstName() != value)
                {
                    _user.setFirstName(value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FullName));
                }
            }
        }

        public string LastName
        {
            get => _user?.getLastName() ?? string.Empty;
            set
            {
                if (_user != null && _user.getLastName() != value)
                {
                    _user.setLastName(value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FullName));
                }
            }
        }

        public string FullName
        {
            get => $"{FirstName} {LastName}".Trim();
            set
            {
                if (_user != null)
                {
                    _user.setFullName(value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FirstName));
                    OnPropertyChanged(nameof(LastName));
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

        public string Password
        {
            get => _user?.getPassword() ?? string.Empty;
            set
            {
                if (_user != null && _user.getPassword() != value)
                {
                    _user.setPassword(value);
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
                    IsRoleSelected = true;
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

        public string Gender
        {
            get => _user?.getGender() ?? string.Empty;
            set
            {
                if (_user != null && _user.getGender() != value)
                {
                    _user.setGender(value);
                    OnPropertyChanged();
                }
            }
        }

        public string Age
        {
            get => _user?.getAge() ?? string.Empty;
            set
            {
                if (_user != null && _user.getAge() != value)
                {
                    _user.setAge(value);
                    OnPropertyChanged();
                }
            }
        }

        public string Phone
        {
            get => _user?.getPhone() ?? string.Empty;
            set
            {
                if (_user != null && _user.getPhone() != value)
                {
                    _user.setPhone(value);
                    OnPropertyChanged();
                }
            }
        }

        public DateTime CreatedAt
        {
            get => _user?.getCreatedAt() ?? DateTime.MinValue;
            set
            {
                if (_user != null && _user.getCreatedAt() != value)
                {
                    _user.setCreatedAt(value);
                    OnPropertyChanged();
                }
            }
        }

        // --- Commands ---
        public ICommand SaveUserCommand { get; }
        public ICommand ClearFormCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand BackCommand { get; }

        // --- Constructors ---
        public UserViewModel()
        {
            _user = new Model.User();
            _users = new ObservableCollection<Model.User>();

            SaveUserCommand = new RelayCommand(ExecuteSaveUser, CanExecuteSaveUser);
            ClearFormCommand = new RelayCommand(ExecuteClearForm);
            NextCommand = new RelayCommand(ExecuteNext, CanExecuteNext);
            BackCommand = new RelayCommand(ExecuteBack);

            LoadUsersFromDatabase();
        }

        public UserViewModel(Model.User user) : this()
        {
            User = user;
        }

        // --- Navigation Handlers ---
        private bool CanExecuteNext(object? parameter) => IsRoleSelected;

        private void ExecuteNext(object? parameter)
        {
            OnNextRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteBack(object? parameter)
        {
            ResetRoleSelection();
            OnBackRequested?.Invoke(this, EventArgs.Empty);
        }

        public void ResetRoleSelection()
        {
            IsRoleSelected = false;
            OnPropertyChanged(nameof(Role));
            OnPropertyChanged(nameof(IsRoleSelected));
            CommandManager.InvalidateRequerySuggested();
        }

        // --- Helper & Database Methods ---
        public void RefreshAllProperties()
        {
            OnPropertyChanged(nameof(UserId));
            OnPropertyChanged(nameof(FirstName));
            OnPropertyChanged(nameof(LastName));
            OnPropertyChanged(nameof(FullName));
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(Password));
            OnPropertyChanged(nameof(Role));
            OnPropertyChanged(nameof(EmailAddress));
            OnPropertyChanged(nameof(Gender));
            OnPropertyChanged(nameof(Age));
            OnPropertyChanged(nameof(Phone));
            OnPropertyChanged(nameof(CreatedAt));
        }

        private void ExecuteClearForm(object? parameter = null)
        {
            User = new Model.User();
            SelectedUser = null;
            ResetRoleSelection();
        }

        private bool CanExecuteSaveUser(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private void ExecuteSaveUser(object? parameter)
        {
            SaveUserToDatabase();
        }

        public void SaveUserToDatabase()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        int currentUserId = UserId;

                        // 1. Insert or Update into users table using email_address
                        if (currentUserId == 0)
                        {
                            string insertUserQuery = @"INSERT INTO users (username, password, role, first_name, last_name, email_address, gender, age, phone, created_at)
                                               VALUES (@username, @password, @role, @firstName, @lastName, @email, @gender, @age, @phone, @createdAt);
                                               SELECT LAST_INSERT_ID();";

                            using (var cmd = new MySqlCommand(insertUserQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@username", Username);
                                cmd.Parameters.AddWithValue("@password", Password);
                                cmd.Parameters.AddWithValue("@role", Role.ToString());
                                cmd.Parameters.AddWithValue("@firstName", FirstName);
                                cmd.Parameters.AddWithValue("@lastName", LastName);
                                cmd.Parameters.AddWithValue("@email", EmailAddress);
                                cmd.Parameters.AddWithValue("@gender", string.IsNullOrEmpty(Gender) ? DBNull.Value : Gender);
                                cmd.Parameters.AddWithValue("@age", string.IsNullOrEmpty(Age) ? DBNull.Value : Age);
                                cmd.Parameters.AddWithValue("@phone", string.IsNullOrEmpty(Phone) ? DBNull.Value : Phone);
                                cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);

                                currentUserId = Convert.ToInt32(cmd.ExecuteScalar());
                                UserId = currentUserId;
                            }
                        }
                        else
                        {
                            string updateUserQuery = @"UPDATE users 
                                               SET username = @username, password = @password, 
                                                   role = @role, first_name = @firstName, last_name = @lastName, 
                                                   email_address = @email, gender = @gender, age = @age, phone = @phone
                                               WHERE user_id = @userId;";

                            using (var cmd = new MySqlCommand(updateUserQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@userId", currentUserId);
                                cmd.Parameters.AddWithValue("@username", Username);
                                cmd.Parameters.AddWithValue("@password", Password);
                                cmd.Parameters.AddWithValue("@role", Role.ToString());
                                cmd.Parameters.AddWithValue("@firstName", FirstName);
                                cmd.Parameters.AddWithValue("@lastName", LastName);
                                cmd.Parameters.AddWithValue("@email", EmailAddress);
                                cmd.Parameters.AddWithValue("@gender", string.IsNullOrEmpty(Gender) ? DBNull.Value : Gender);
                                cmd.Parameters.AddWithValue("@age", string.IsNullOrEmpty(Age) ? DBNull.Value : Age);
                                cmd.Parameters.AddWithValue("@phone", string.IsNullOrEmpty(Phone) ? DBNull.Value : Phone);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 2. Synchronize with renters table if user is a Tenant
                        if (Role == Role.Tenant)
                        {
                            string syncRenterQuery = @"INSERT INTO renters (user_id, first_name, last_name, email_address, phone, gender, age)
                                               VALUES (@userId, @firstName, @lastName, @email, @phone, @gender, @age)
                                               ON DUPLICATE KEY UPDATE 
                                                   first_name = VALUES(first_name),
                                                   last_name = VALUES(last_name),
                                                   email_address = VALUES(email_address),
                                                   phone = VALUES(phone),
                                                   gender = VALUES(gender),
                                                   age = VALUES(age);";

                            using (var renterCmd = new MySqlCommand(syncRenterQuery, conn, transaction))
                            {
                                renterCmd.Parameters.AddWithValue("@userId", currentUserId);
                                renterCmd.Parameters.AddWithValue("@firstName", FirstName);
                                renterCmd.Parameters.AddWithValue("@lastName", LastName);
                                renterCmd.Parameters.AddWithValue("@email", EmailAddress);
                                renterCmd.Parameters.AddWithValue("@phone", Phone ?? string.Empty);
                                renterCmd.Parameters.AddWithValue("@gender", Gender ?? string.Empty);
                                renterCmd.Parameters.AddWithValue("@age", Age ?? string.Empty);

                                renterCmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        MessageBox.Show("Saved and synchronized successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadUsersFromDatabase();
                        ExecuteClearForm();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Database Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        public void LoadUsersFromDatabase()
        {
            Users.Clear();
            string query = "SELECT user_id, username, password, role, first_name, last_name, email_address, gender, age, phone, created_at FROM users";

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
                            var loadedUser = new Model.User();
                            loadedUser.setUserId(reader.GetInt32("user_id"));
                            loadedUser.setUsername(reader.IsDBNull(reader.GetOrdinal("username")) ? "" : reader.GetString("username"));
                            loadedUser.setPassword(reader.IsDBNull(reader.GetOrdinal("password")) ? "" : reader.GetString("password"));

                            string roleStr = reader.IsDBNull(reader.GetOrdinal("role")) ? "Tenant" : reader.GetString("role");
                            if (Enum.TryParse<Role>(roleStr, true, out var parsedRole))
                            {
                                loadedUser.setRole(parsedRole);
                            }

                            loadedUser.setFirstName(reader.IsDBNull(reader.GetOrdinal("first_name")) ? "" : reader.GetString("first_name"));
                            loadedUser.setLastName(reader.IsDBNull(reader.GetOrdinal("last_name")) ? "" : reader.GetString("last_name"));
                            loadedUser.setEmailAddress(reader.IsDBNull(reader.GetOrdinal("email_address")) ? "" : reader.GetString("email_address"));
                            loadedUser.setGender(reader.IsDBNull(reader.GetOrdinal("gender")) ? "" : reader.GetString("gender"));
                            loadedUser.setAge(reader.IsDBNull(reader.GetOrdinal("age")) ? "" : reader.GetString("age"));
                            loadedUser.setPhone(reader.IsDBNull(reader.GetOrdinal("phone")) ? "" : reader.GetString("phone"));

                            if (!reader.IsDBNull(reader.GetOrdinal("created_at")))
                            {
                                loadedUser.setCreatedAt(reader.GetDateTime("created_at"));
                            }

                            Users.Add(loadedUser);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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