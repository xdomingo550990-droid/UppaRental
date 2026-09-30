using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class DashboardPage : Window
    {
        public DashboardPage()
        {
            InitializeComponent();
            
            // Set default view on launch
            MainContentFrame.Content = new OverviewPage();
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                // 1. Reset menu styles for all sidebar buttons
                btnOverview.Style = (Style)FindResource("NavButtonStyle");
                btnUnits.Style = (Style)FindResource("NavButtonStyle");
                btnReservations.Style = (Style)FindResource("NavButtonStyle");
                btnRenters.Style = (Style)FindResource("NavButtonStyle");
                btnBilling.Style = (Style)FindResource("NavButtonStyle");
                btnMessages.Style = (Style)FindResource("NavButtonStyle");
                btnReports.Style = (Style)FindResource("NavButtonStyle");
                btnSettings.Style = (Style)FindResource("NavButtonStyle");

                // 2. Highlight selected button
                clickedButton.Style = (Style)FindResource("ActiveNavButtonStyle");

                // 3. Swap view content and update top header title dynamically
                if (clickedButton == btnOverview)
                {
                    txtPageTitle.Text = "Overview";
                    MainContentFrame.Content = new OverviewPage();
                }
                else if (clickedButton == btnUnits)
                {
                    txtPageTitle.Text = "Units";
                    MainContentFrame.Content = new UnitsPage();
                }
                else if (clickedButton == btnReservations)
                {
                    txtPageTitle.Text = "Reservations";
                    MainContentFrame.Content = new ReservationsPage();
                }
                else if (clickedButton == btnRenters)
                {
                    txtPageTitle.Text = "Renters";
                    MainContentFrame.Content = new RentersPage();
                }
                else if (clickedButton == btnBilling)
                {
                    txtPageTitle.Text = "Billing and Payments";
                    MainContentFrame.Content = new BillingPage();
                }
                else if (clickedButton == btnMessages)
                {
                    txtPageTitle.Text = "Messages";
                    MainContentFrame.Content = new MessagesPage();
                }
                else if (clickedButton == btnReports)
                {
                    txtPageTitle.Text = "Reports";
                    MainContentFrame.Content = new ReportsPage();
                }
                else if (clickedButton == btnSettings)
                {
                    txtPageTitle.Text = "Settings";
                    MainContentFrame.Content = new SettingsPage();
                }
            }
        }

        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            LandingPage landingPage = new LandingPage();
            landingPage.Show();
            this.Close();
        }
    }
}