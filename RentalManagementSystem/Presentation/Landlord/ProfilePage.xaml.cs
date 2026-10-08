using RentalManagementSystem.DAO;
using RentalManagementSystem.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class ProfilePage : UserControl
    {
        public ProfilePage()
        {
            InitializeComponent();
            LoadProfile();
        }

        private void LoadProfile()
        {
            var session = UserSession.CurrentUser;
            if (session == null) return;

            try
            {
                // Fresh copy from the database (login does not load the phone number)
                var user = UserDao.GetById(session.UserId) ?? session;

                string fullName = $"{user.FirstName} {user.LastName}".Trim();
                txtFullName.Text = fullName;
                txtEmail.Text = user.EmailAddress ?? "";
                txtPhone.Text = user.Phone ?? "";
                txtAvatar.Text = fullName.Length > 0 ? fullName.Substring(0, 1).ToUpper() : "L";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Profile load failed: {ex.Message}");
            }

            int units = PropertyStore.All.Count;
            txtUnits.Text = units == 1 ? "1 unit" : $"{units} units";
        }

        // Saves the profile details to the users table.
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Please enter your full name.", "Profile");
                return;
            }

            var user = UserSession.CurrentUser;
            if (user == null)
            {
                MessageBox.Show("No user is logged in.", "Profile");
                return;
            }

            string name = txtFullName.Text.Trim();
            var parts = name.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

            user.FirstName = parts.Length > 0 ? parts[0] : "";
            user.LastName = parts.Length > 1 ? parts[1] : "";
            user.EmailAddress = txtEmail.Text.Trim();
            user.Phone = txtPhone.Text.Trim();

            try
            {
                UserDao.UpdateProfile(user);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save your profile.\n\n{ex.Message}", "Profile",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            txtAvatar.Text = name.Substring(0, 1).ToUpper();
            MessageBox.Show("Your profile has been saved.", "Profile");
        }
    }
}
