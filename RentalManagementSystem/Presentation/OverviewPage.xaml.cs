using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class OverviewPage : UserControl
    {
        private const int MaxPerSection = 4;   // cards per section when "All" is selected
        private string _filter = "All";
        private string _query = "";            // text from the Dashboard's top search bar

        public OverviewPage()
        {
            InitializeComponent();
            LoadSections();
        }

        // Called by the Dashboard's top search bar
        public void SetSearch(string text)
        {
            _query = text?.Trim() ?? "";

            if (icSections == null) return;
            LoadSections();
        }

        private void Category_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb) _filter = rb.Tag?.ToString() ?? "All";

            // Fires once during InitializeComponent, before icSections exists.
            if (icSections == null) return;
            LoadSections();
        }

        private void LoadSections()
        {
            IEnumerable<RentalProperty> pool = PropertyStore.All;

            if (_query.Length > 0)
            {
                pool = pool.Where(p =>
                    Has(p.Name, _query) || Has(p.Location, _query) || Has(p.Description, _query) ||
                    Has(p.Status, _query) || Has(p.RoomType, _query));
            }

            // While searching, show every match instead of just the top few per category.
            int perSection = _query.Length > 0 ? int.MaxValue : MaxPerSection;

            icSections.ItemsSource = PropertyStore.BuildSections(pool, _filter, perSection, includeDrafts: false);
        }

        private static bool Has(string text, string query) =>
            !string.IsNullOrEmpty(text) && text.Contains(query, StringComparison.OrdinalIgnoreCase);

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