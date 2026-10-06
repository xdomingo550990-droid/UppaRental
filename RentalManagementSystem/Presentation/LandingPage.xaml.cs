using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RentalManagementSystem.Presentation
{
    /// <summary>
    /// Interaction logic for LandingPage.xaml
    /// </summary>
    public partial class LandingPage : Window

    {
        public LandingPage()
        {
            InitializeComponent();

        }

        

        // Nav "Login" -> open the popup on the Login side
        private void LoginButton_Click(object sender, RoutedEventArgs e) => OpenAuth(login: true);

        // Nav "Sign Up" -> open the popup on the Registration side
        private void SignUpButton_Click(object sender, RoutedEventArgs e) => OpenAuth(login: false);

        // Role picked during Sign Up ("Tenant" or "Landlord"); null if the user came in through Login
        public string? SelectedUserType { get; private set; } = string.Empty;

        private void OpenAuth(bool login)
        {
            AuthOverlay.Visibility = Visibility.Visible;

            if (login)
            {
                // Login: go straight to the login form (no role question)
                UserType.Visibility = Visibility.Collapsed;
                Auth.ShowMode(true);
                Auth.Visibility = Visibility.Visible;
            }
            else
            {
                // Sign Up: first ask tenant or landlord
                UserType.Reset();
                UserType.Visibility = Visibility.Visible;
                Auth.Visibility = Visibility.Collapsed;
            }
        }

        // "Explore the platform" -> open the About page
        private void ExploreButton_Click(object sender, RoutedEventArgs e)
        {
            AboutPage about = new AboutPage();
            about.Show();
            this.Close();
        }

        private void CloseAuth() => AuthOverlay.Visibility = Visibility.Collapsed;

        // Arrow pressed -> remember the role, then show Login / Register
        private void UserType_NextClicked(object sender, EventArgs e)
        {
            SelectedUserType = UserType.SelectedRole;

            UserType.Visibility = Visibility.Collapsed;
            Auth.ShowMode(false);   // role is only asked on Sign Up, so open the Registration side
            Auth.Visibility = Visibility.Visible;
        }

        // Back pressed -> close the popup and return to the landing page
        private void UserType_BackClicked(object sender, EventArgs e) => CloseAuth();

        // Clicking the dark area outside the card closes the popup
        private void AuthOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == AuthOverlay)
                CloseAuth();
        }

        // "Register" clicked inside the login card -> ask tenant/landlord first
        private void Auth_RegisterRequested(object sender, EventArgs e)
        {
            UserType.Reset();
            Auth.Visibility = Visibility.Collapsed;
            UserType.Visibility = Visibility.Visible;
        }

        private void Auth_CloseRequested(object sender, EventArgs e) => CloseAuth();

        // Login succeeded -> open Dashboard, close this window
        private void Auth_LoginSucceeded(object sender, EventArgs e)
        {
            DashboardPage dashboard = new DashboardPage();
            dashboard.Show();
            this.Close();
        }
    }
}