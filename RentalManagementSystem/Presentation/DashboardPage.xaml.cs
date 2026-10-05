using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace RentalManagementSystem.Presentation
{
    public partial class DashboardPage : Window
    {
        private const double SidebarExpandedWidth = 260;
        private const double SidebarCollapsedWidth = 72;
        private bool _sidebarCollapsed;

        public DashboardPage()
        {
            InitializeComponent();

            // Overview is the page shown when the dashboard opens
            MainContentFrame.Content = new OverviewPage();
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
            if (sender is not RadioButton nav) return;

            txtPageTitle.Text = nav.Content?.ToString();

            MainContentFrame.Content = nav.Name switch
            {
                nameof(btnOverview) => new OverviewPage(),
                nameof(btnProperties) => new UnitsPage(),
                nameof(btnReservations) => new ReservationsPage(),
                nameof(btnRenters) => new RentersPage(),
                nameof(btnBilling) => new BillingPage(),
                nameof(btnMessages) => new MessagesPage(),
                nameof(btnReports) => new ReportsPage(),
                nameof(btnSettings) => new SettingsPage(),
                _ => MainContentFrame.Content
            };
        }

        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Ask for confirmation
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to log out?", 
                "Confirm Logout", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 2. Open LandingPage (or AuthCard/LandingPage depending on your login setup)
                LandingPage landingPage = new LandingPage();
                landingPage.Show();

                // 3. Close current window
                Window.GetWindow(this)?.Close();
            }
        }
    }
}