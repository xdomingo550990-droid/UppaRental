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

        public TenantDashboardPage()
        {
            InitializeComponent();

            // Overview is the page shown when the dashboard opens
            MainContentFrame.Content = new TenantOverviewPage();
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

            txtPageTitle.Text = nav.Content?.ToString();

            // Start every page with an empty search box
            txtSearch.Clear();

            // The top search bar is hidden on the Profile page
            SearchBar.Visibility = nav.Name == nameof(btnSettings)
                ? Visibility.Collapsed
                : Visibility.Visible;

            MainContentFrame.Content = nav.Name switch
            {
                nameof(btnOverview) => new TenantOverviewPage(),
                nameof(btnReservations) => new TenantReservationsPage(),
                nameof(btnBilling) => new TenantBillingPage(),
                nameof(btnSettings) => new TenantProfilePage(),    // opened by the profile card at the bottom of the sidebar
                _ => MainContentFrame.Content
            };
        }

        // Sends what you type in the top search bar to the page that is open
        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MainContentFrame == null) return;

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
                MessageBoxResult.No);          // "No" is the default so Enter doesn't log you out by accident

            if (result != MessageBoxResult.Yes)
                return;

            // If you keep any session or current-user state, clear it here
            // e.g. SessionManager.CurrentUser = null;

            var landing = new LandingPage();
            landing.Show();                    // open the landing page first...
            this.Close();                      // ...then close the dashboard
        }
    }
}
