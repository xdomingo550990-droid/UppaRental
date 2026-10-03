using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RentalManagementSystem.Presentation
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            // TODO: load the saved profile/preferences from your database and fill the fields here.
        }

        // Saves profile, preferences and notification switches.
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Please enter your full name.", "Settings");
                return;
            }

            if (!int.TryParse(txtGraceDays.Text, out int graceDays) || graceDays < 0)
            {
                MessageBox.Show("Grace period must be a whole number of days (0 or more).", "Settings");
                return;
            }

            txtAvatar.Text = txtFullName.Text.Trim().Substring(0, 1).ToUpper();

            // TODO: save these to your database / DAO:
            // txtFullName.Text, txtEmail.Text, txtPhone.Text, txtBusinessName.Text,
            // cmbCurrency.SelectedIndex, cmbDueDay.SelectedIndex, graceDays,
            // chkPaymentAlerts.IsChecked, chkOverdueAlerts.IsChecked,
            // chkReservationAlerts.IsChecked, chkMessageAlerts.IsChecked

            MessageBox.Show("Your settings have been saved.", "Settings");
        }

        private void UpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            string current = pwdCurrent.Password;
            string newPwd = pwdNew.Password;
            string confirm = pwdConfirm.Password;

            if (current.Length == 0 || newPwd.Length == 0 || confirm.Length == 0)
            {
                ShowPasswordMessage("Please fill in all three password fields.", false);
                return;
            }
            if (newPwd.Length < 8)
            {
                ShowPasswordMessage("The new password must be at least 8 characters.", false);
                return;
            }
            if (newPwd != confirm)
            {
                ShowPasswordMessage("The new password and confirmation do not match.", false);
                return;
            }

            // TODO: verify the current password and save the new one (hashed) through your DAO.

            pwdCurrent.Clear();
            pwdNew.Clear();
            pwdConfirm.Clear();
            ShowPasswordMessage("Password updated.", true);
        }

        private void ShowPasswordMessage(string message, bool success)
        {
            txtPasswordMessage.Text = message;
            txtPasswordMessage.Foreground = new SolidColorBrush(success
                ? Color.FromRgb(0x27, 0x89, 0x4C)
                : Color.FromRgb(0xC8, 0x4B, 0x41));
            txtPasswordMessage.Visibility = Visibility.Visible;
        }
    }
}