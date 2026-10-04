using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class UnitsPage : UserControl
    {
        private const int MaxPerSection = 4;   // cards/rows per section when "All" is selected
        private string _filter = "All";

        public UnitsPage()
        {
            InitializeComponent();
            LoadSections();
        }

        // ---------- Add Property popup ----------

        private void AddProperty_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddPropertyWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (addWindow.ShowDialog() == true)
            {
                // Goes into the shared list, so it also appears on the Overview page.
                PropertyStore.Add(PropertyStore.FromForm(addWindow.Result));
                LoadSections();
            }
        }

        // ---------- View switch, chips, search ----------

        private void View_Checked(object sender, RoutedEventArgs e)
        {
            // Fires during InitializeComponent, before the views exist.
            if (scrList == null || scrGallery == null || rbGallery == null) return;

            bool gallery = rbGallery.IsChecked == true;
            scrList.Visibility = gallery ? Visibility.Collapsed : Visibility.Visible;
            scrGallery.Visibility = gallery ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Category_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb) _filter = rb.Tag?.ToString() ?? "All";

            if (icListSections == null) return;
            LoadSections();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (icListSections == null) return;
            LoadSections();
        }

        // ---------- Sections ----------

        private void LoadSections()
        {
            string query = txtSearch?.Text?.Trim() ?? "";

            IEnumerable<RentalProperty> pool = PropertyStore.All;
            if (query.Length > 0)
            {
                pool = pool.Where(p =>
                    Has(p.Name, query) || Has(p.Location, query) || Has(p.Description, query) ||
                    Has(p.Status, query) || Has(p.RoomType, query));
            }

            // While searching, show every match instead of just the top few per category.
            int perSection = query.Length > 0 ? int.MaxValue : MaxPerSection;

            var sections = PropertyStore.BuildSections(pool, _filter, perSection, includeDrafts: true);

            icListSections.ItemsSource = sections;
            icGallerySections.ItemsSource = sections;
            txtEmpty.Visibility = sections.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static bool Has(string text, string query) =>
            !string.IsNullOrEmpty(text) && text.Contains(query, StringComparison.OrdinalIgnoreCase);

        // ---------- Gallery "Read more" (placeholder: replace with a details popup) ----------

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

    // Kept only in case another page still references it. If the build has no errors
    // after you delete this class, you can remove it.
    public class UnitViewModel
    {
        public string RoomNo { get; set; } = "";
        public string Floor { get; set; } = "";
        public string RoomType { get; set; } = "";
        public string MonthlyRate { get; set; } = "";
        public string Status { get; set; } = "";
        public string Actions { get; set; } = "";
    }
}