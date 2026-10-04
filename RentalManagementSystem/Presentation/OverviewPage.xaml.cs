using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class OverviewPage : UserControl
    {
        private const int MaxPerSection = 4;   // cards per section when "All" is selected
        private string _filter = "All";

        public OverviewPage()
        {
            InitializeComponent();
            LoadSections();
        }

        private void Category_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb) _filter = rb.Tag?.ToString() ?? "All";

            // Fires once during InitializeComponent, before the list exists.
            if (icSections == null) return;
            LoadSections();
        }

        // Drafts are not shown on the Overview.
        private void LoadSections()
        {
            icSections.ItemsSource = PropertyStore.BuildSections(PropertyStore.All, _filter, MaxPerSection, includeDrafts: false);
        }

        // Placeholder: replace with your property details page or popup.
        private void ReadMore_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button b && b.DataContext is RentalProperty p)
            {
                MessageBox.Show(
                    $"{p.Name}\n{p.Location}\n\n{p.Description}\n\n{p.Summary}\n{p.Status} · {p.PriceWithUnit}",
                    "Property details");
            }
        }
    }
}