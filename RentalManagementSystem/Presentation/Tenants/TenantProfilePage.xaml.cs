using RentalManagementSystem.DAO;
using RentalManagementSystem.Model;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class TenantProfilePage : UserControl
    {
        private User loggedInUser;

        public TenantProfilePage()
        {
            InitializeComponent();
        }

        public TenantProfilePage(User loggedInUser) : this()
        {
            this.loggedInUser = loggedInUser;
            LoadUserData();
        }

        private void LoadUserData()
        {
            if (loggedInUser == null) return;

            try
            {
                // Login does not load the phone number, so fetch the saved profile
                var fresh = UserDao.GetById(loggedInUser.getUserId());
                if (fresh != null)
                {
                    loggedInUser.setPhone(fresh.Phone);
                    if (!string.IsNullOrWhiteSpace(fresh.EmailAddress)) loggedInUser.setEmailAddress(fresh.EmailAddress);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Profile load failed: {ex.Message}");
            }

            string firstName = loggedInUser.getFirstName() ?? "";
            string lastName = loggedInUser.getLastName() ?? "";
            string fullName = $"{firstName} {lastName}".Trim();

            if (txtFullName != null) txtFullName.Text = fullName;
            if (txtEmail != null) txtEmail.Text = GetUserEmail(loggedInUser);
            if (txtPhone != null) txtPhone.Text = GetUserPhone(loggedInUser);

            if (txtAvatar != null && !string.IsNullOrWhiteSpace(fullName))
            {
                txtAvatar.Text = fullName.Substring(0, 1).ToUpper();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName?.Text))
            {
                MessageBox.Show("Please enter your full name.", "Profile");
                return;
            }

            string name = txtFullName.Text.Trim();

            if (txtAvatar != null)
            {
                txtAvatar.Text = name.Substring(0, 1).ToUpper();
            }

            if (loggedInUser != null)
            {
                var nameParts = name.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                string firstName = nameParts.Length > 0 ? nameParts[0] : "";
                string lastName = nameParts.Length > 1 ? nameParts[1] : "";

                loggedInUser.setFirstName(firstName);
                loggedInUser.setLastName(lastName);

                SetUserEmail(loggedInUser, txtEmail?.Text?.Trim() ?? "");
                SetUserPhone(loggedInUser, txtPhone?.Text?.Trim() ?? "");

                try
                {
                    UserDao.UpdateProfile(loggedInUser);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not save your profile.\n\n{ex.Message}", "Profile",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            MessageBox.Show("Your profile has been saved.", "Profile");
        }

        // --- Helper methods: Match these to the exact property names in your User.cs ---

        private static string GetUserEmail(User u) => u.EmailAddress ?? ""; // Or u.getEmailAddress()
        private static string GetUserPhone(User u) => u.Phone ?? "";        // Or u.getPhone()

        private static void SetUserEmail(User u, string email) => u.EmailAddress = email; // Or u.setEmailAddress(email)
        private static void SetUserPhone(User u, string phone) => u.Phone = phone;        // Or u.setPhone(phone)
    }
}