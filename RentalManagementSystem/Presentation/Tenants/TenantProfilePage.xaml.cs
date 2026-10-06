using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class TenantProfilePage : UserControl
    {
        public TenantProfilePage()
        {
            InitializeComponent();
            // TODO: load the saved tenant profile from your database and fill the fields here.
        }

        // Saves the profile details.
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Please enter your full name.", "Profile");
                return;
            }

            txtAvatar.Text = txtFullName.Text.Trim().Substring(0, 1).ToUpper();

            // TODO: save these to your database / DAO:
            // txtFullName.Text, txtEmail.Text, txtPhone.Text

            MessageBox.Show("Your profile has been saved.", "Profile");
        }
    }
}
