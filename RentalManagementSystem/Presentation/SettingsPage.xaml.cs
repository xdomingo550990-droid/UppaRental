using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            // TODO: load the saved profile from your database and fill the fields here.
        }

        // Saves the profile details.
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Please enter your full name.", "Settings");
                return;
            }

            txtAvatar.Text = txtFullName.Text.Trim().Substring(0, 1).ToUpper();

            // TODO: save these to your database / DAO:
            // txtFullName.Text, txtEmail.Text, txtPhone.Text

            MessageBox.Show("Your settings have been saved.", "Settings");
        }
    }
}
