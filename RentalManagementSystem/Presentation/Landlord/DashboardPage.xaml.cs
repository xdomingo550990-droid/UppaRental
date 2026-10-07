using RentalManagementSystem.Model;   // for User
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

        // Logged-in user (passed from AuthCard)
        private readonly User? _currentUser;

        // Empty constructor, kept so the designer still works
        public DashboardPage() : this(null) { }

        // FIX: constructor that accepts the user (AuthCard calls this)
        public DashboardPage(User? user)
        {
            InitializeComponent();
            _currentUser = user;

            // Overview is the first page shown
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

            // Hide brand text when collapsed
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

            // Clear search on every page change
            txtSearch.Clear();

            // Search bar hidden on Properties and Profile
            bool hideSearch = nav.Name == nameof(btnSettings) || nav.Name == nameof(btnProperties);
            SearchBar.Visibility = hideSearch ? Visibility.Collapsed : Visibility.Visible;

            MainContentFrame.Content = nav.Name switch
            {
                nameof(btnOverview) => new OverviewPage(),
                nameof(btnProperties) => new PropertiesPage(),   // FIX: was UnitsPage (not in project)
                nameof(btnRenters) => new RentersPage(),
                nameof(btnBilling) => new BillingPage(),
                nameof(btnSettings) => new ProfilePage(),
                _ => MainContentFrame.Content
            };
        }

        // Send search text to the open page
        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MainContentFrame == null) return;

            string query = txtSearch.Text;

            switch (MainContentFrame.Content)
            {
                case OverviewPage overview:
                    overview.SetSearch(query);
                    break;
                case RentersPage renters:
                    renters.ApplyGlobalSearch(query);
                    break;
                case BillingPage billing:
                    billing.ApplyGlobalSearch(query);
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
                MessageBoxResult.No);   // "No" is default

            if (result != MessageBoxResult.Yes)
                return;

            // Clear session here if you add one
            _ = _currentUser;

            var landing = new LandingPage();
            landing.Show();
            this.Close();
        }

        // Unused handler, kept in case the XAML references it
        private void btnProperties_Checked(object sender, RoutedEventArgs e)
        {
        }
    }
}