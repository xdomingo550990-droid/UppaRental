using System;
using RentalManagementSystem.Services;
using System.Configuration;
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

        // Stores the optional user state or session object if passed
        public object CurrentUser { get; private set; }

        /// <summary>
        /// Default parameterless constructor.
        /// </summary>
        public DashboardPage()
        {
            InitializeComponent();

            // Overview is the page shown when the dashboard opens
            MainContentFrame.Content = new OverviewPage();
            LoadSidebarProfile();
        }

        /// <summary>Shows the logged-in landlord's name and unit count in the sidebar profile card.</summary>
        private void LoadSidebarProfile()
        {
            try
            {
                var user = UserSession.CurrentUser;
                if (user == null) return;

                string fullName = $"{user.FirstName} {user.LastName}".Trim();
                if (fullName.Length == 0) fullName = user.Username;
                int units = PropertyStore.All.Count;

                btnSettings.ApplyTemplate();
                if (btnSettings.Template.FindName("txtSbName", btnSettings) is TextBlock name) name.Text = fullName;
                if (btnSettings.Template.FindName("txtSbAvatar", btnSettings) is TextBlock avatar)
                    avatar.Text = fullName.Length > 0 ? fullName.Substring(0, 1).ToUpper() : "L";
                if (btnSettings.Template.FindName("txtSbRole", btnSettings) is TextBlock role)
                    role.Text = $"Landlord • {units} {(units == 1 ? "unit" : "units")}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sidebar profile load failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Overloaded constructor that accepts an argument (e.g., Logged-in User/Session).
        /// Calls the parameterless constructor first using ': this()'.
        /// </summary>
        public DashboardPage(object currentUser) : this()
        {
            CurrentUser = currentUser;
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

            // The top search bar is hidden on the Properties and Settings pages
            bool hideSearch = nav.Name == nameof(btnSettings) || nav.Name == nameof(btnProperties);
            SearchBar.Visibility = hideSearch ? Visibility.Collapsed : Visibility.Visible;

            MainContentFrame.Content = nav.Name switch
            {
                nameof(btnOverview) => new OverviewPage(),
                nameof(btnProperties) => new UnitsPage(),
                nameof(btnRenters) => new RentersPage(),      // Renters + Reservations share this page
                nameof(btnBilling) => new BillingPage(),
                nameof(btnSettings) => new ProfilePage(),    // profile card at the bottom of the sidebar
                _ => MainContentFrame.Content
            };

            LoadSidebarProfile();   // picks up a name or unit-count change
        }

        // Sends what you type in the top search bar to the page that is open
        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MainContentFrame == null) return;

            string query = txtSearch.Text;

            switch (MainContentFrame.Content)
            {
                case OverviewPage overview:
                    overview.SetSearch(query);
                    break;
                case RentersPage renters:      // renters + reservations table
                    renters.ApplyGlobalSearch(query);
                    break;
                case BillingPage billing:      // invoices table
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
                MessageBoxResult.No);          // "No" is the default so Enter doesn't log you out by accident

            if (result != MessageBoxResult.Yes)
                return;

            // Clear session state
            CurrentUser = null;

            var landing = new LandingPage();
            landing.Show();                    // open the landing page first...
            this.Close();                      // ...then close the dashboard
        }

        private void btnProperties_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}