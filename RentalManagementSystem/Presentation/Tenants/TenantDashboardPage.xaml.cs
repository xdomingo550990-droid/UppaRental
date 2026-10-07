using RentalManagementSystem.Model;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace RentalManagementSystem.Presentation
{
    public partial class TenantDashboardPage : Window
    {
        private const double SidebarExpandedWidth = 260;
        private const double SidebarCollapsedWidth = 72;
        private bool _sidebarCollapsed;
        private User loggedInUser;

        public TenantDashboardPage()
        {
            InitializeComponent();
            MainContentFrame.Content = new TenantOverviewPage();
        }

        // Chaining : this() ensures InitializeComponent() and default page setup execute
        public TenantDashboardPage(User loggedInUser) : this()
        {
            this.loggedInUser = loggedInUser;

            if (this.loggedInUser != null)
            {
                string firstName = loggedInUser.getFirstName() ?? "";
                string lastName = loggedInUser.getLastName() ?? "";
                string fullName = $"{firstName} {lastName}".Trim();

                if (btnSettings != null)
                {
                    // Set full name for the TextBlock
                    btnSettings.Content = string.IsNullOrWhiteSpace(fullName) ? "Tenant" : fullName;

                    // Set first letter for the circle avatar (e.g. "K" for Kimy)
                    btnSettings.Tag = !string.IsNullOrEmpty(firstName) ? firstName[0].ToString().ToUpper() : "U";
                }

                // Reload initial page with loggedInUser context
                MainContentFrame.Content = new TenantOverviewPage(this.loggedInUser);
            }
        }
        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            _sidebarCollapsed = !_sidebarCollapsed;

            var animation = new DoubleAnimation
            {
                To = _sidebarCollapsed ? SidebarCollapsedWidth : SidebarExpandedWidth,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            SidebarPanel.BeginAnimation(FrameworkElement.WidthProperty, animation);

            BrandPanel.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
            btnToggleSidebar.HorizontalAlignment = _sidebarCollapsed
                ? HorizontalAlignment.Center
                : HorizontalAlignment.Right;
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            var nav = sender as RadioButton;
            if (nav == null) return;

            // Header title: Displays "Profile" for btnSettings, otherwise uses Content
            txtPageTitle.Text = nav.Name == nameof(btnSettings) ? "Profile" : nav.Content?.ToString();

            if (txtSearch != null)
            {
                txtSearch.Clear();
            }

            if (SearchBar != null)
            {
                SearchBar.Visibility = nav.Name == nameof(btnSettings)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }

            // Pass loggedInUser into child pages on navigation
            MainContentFrame.Content = nav.Name switch
            {
                nameof(btnOverview) => new TenantOverviewPage(loggedInUser),
                nameof(btnReservations) => new TenantReservationsPage(loggedInUser),
                nameof(btnBilling) => new TenantBillingPage(loggedInUser),
                nameof(btnSettings) => new TenantProfilePage(loggedInUser),
                _ => MainContentFrame.Content
            };
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MainContentFrame?.Content == null) return;

            switch (MainContentFrame.Content)
            {
                case TenantOverviewPage overview:
                    overview.SetSearch(txtSearch.Text);
                    break;
                case TenantReservationsPage reservations:
                    reservations.ApplyGlobalSearch(txtSearch.Text);
                    break;
                case TenantBillingPage billing:
                    billing.ApplyGlobalSearch(txtSearch.Text);
                    break;
            }
        }

        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
                return;

            var landing = new LandingPage();
            landing.Show();
            this.Close();
        }
    }
}