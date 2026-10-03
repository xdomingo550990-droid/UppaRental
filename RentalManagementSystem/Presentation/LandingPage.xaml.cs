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

        private void OpenAuth(bool login)
        {
            Auth.ShowMode(login);
            AuthOverlay.Visibility = Visibility.Visible;
        }

        // "Explore the platform" -> open the About page
        private void ExploreButton_Click(object sender, RoutedEventArgs e)
        {
            AboutPage about = new AboutPage();
            about.Show();
            this.Close();
        }

        private void CloseAuth() => AuthOverlay.Visibility = Visibility.Collapsed;

        // Clicking the dark area outside the card closes the popup
        private void AuthOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == AuthOverlay)
                CloseAuth();
        }

        private void Auth_CloseRequested(object sender, EventArgs e) => CloseAuth();

        // Login succeeded -> your original behavior: open Dashboard, close this window
        private void Auth_LoginSucceeded(object sender, EventArgs e)
        {
            // Instantiate the Dashboard page window
            DashboardPage dashboard = new DashboardPage();

            // Show the Dashboard window
            dashboard.Show();

            // Close the current LandingPage window
            this.Close();
        }
    }
}
