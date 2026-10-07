using RentalManagementSystem.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class UserTypeCard : UserControl
    {
        /// <summary>"Tenant" or "Landlord" (null until the user picks one).</summary>
        public string SelectedRole { get; private set; }

        /// <summary>Raised when the user picks a role and presses the arrow button.</summary>
        public event EventHandler NextClicked;

        /// <summary>Raised when the user presses Back.</summary>
        public event EventHandler BackClicked;

        public UserTypeCard()
        {
            InitializeComponent();
            DataContext = new UserViewModel();
        }

        /// <summary>Clears the selection (call this each time the card is shown).</summary>
        public void Reset()
        {
            TenantRadio.IsChecked = false;
            LandlordRadio.IsChecked = false;
            SelectedRole = null;
            NextButton.IsEnabled = false;
        }

        private void Role_Checked(object sender, RoutedEventArgs e)
        {
            SelectedRole = (sender as RadioButton)?.Tag as string;
            NextButton.IsEnabled = SelectedRole != null;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedRole != null)
                NextClicked?.Invoke(this, EventArgs.Empty);
        }

        private void Back_Click(object sender, RoutedEventArgs e) =>
            BackClicked?.Invoke(this, EventArgs.Empty);
    }
}
