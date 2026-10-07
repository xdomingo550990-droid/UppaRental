using RentalManagementSystem.Model;
using RentalManagementSystem.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class TenantOverviewPage : UserControl
    {
        private TenantOverviewViewModel ViewModel => DataContext as TenantOverviewViewModel;

        public TenantOverviewPage()
        {
            InitializeComponent();
            DataContext = new TenantOverviewViewModel();
            BindItemsSource();
        }

        public TenantOverviewPage(User loggedInUser)
        {
            InitializeComponent();
            DataContext = new TenantOverviewViewModel(loggedInUser);
            BindItemsSource();
        }

        private void BindItemsSource()
        {
            if (icSections != null && ViewModel != null)
            {
                icSections.ItemsSource = ViewModel.Sections;
            }
        }

        // Called by dashboard search bar
        public void SetSearch(string text)
        {
            if (ViewModel != null)
            {
                ViewModel.SearchQuery = text?.Trim() ?? "";
            }
        }

        private void Category_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && ViewModel != null)
            {
                ViewModel.Filter = rb.Tag?.ToString() ?? "All";
            }
        }

        private void ReadMore_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button b && b.DataContext is Property p)
            {
                MessageBox.Show(
                    $"{p.Name}\n{p.Location}\n\n{p.Description}\n\nStatus: {p.Status} · Rent: ₱{p.MonthlyRent:N0}/mo",
                    "Property Details");
            }
        }
    }
}