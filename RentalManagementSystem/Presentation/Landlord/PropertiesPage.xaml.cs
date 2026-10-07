using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class PropertiesPage : UserControl
    {
        private string _filter = "All";

        public PropertiesPage()
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

        // ---------- Read more / Edit / Delete (buttons inside the cards and rows) ----------

        private void Item_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not Button b || b.DataContext is not RentalProperty p) return;

            switch (b.Tag as string)
            {
                case "Edit":
                    EditProperty(p);
                    e.Handled = true;
                    break;
                case "Delete":
                    DeleteProperty(p);
                    e.Handled = true;
                    break;
                case "Read":
                    ShowDetails(p);
                    e.Handled = true;
                    break;
            }
        }

        private void EditProperty(RentalProperty p)
        {
            var editWindow = new EditPropertyWindow(p)
            {
                Owner = Window.GetWindow(this)
            };

            // The window only changes the property when Save is pressed and the form is valid.
            if (editWindow.ShowDialog() == true)
            {
                PropertyStore.NotifyUpdated(p);
                LoadSections();
            }
        }

        private void DeleteProperty(RentalProperty p)
        {
            var answer = MessageBox.Show(
                Window.GetWindow(this),
                $"Delete \"{p.Name}\"?\n\nThis removes it from the Properties and Overview pages.",
                "Delete property",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes) return;

            if (PropertyStore.Remove(p))
                LoadSections();
        }

        private void ShowDetails(RentalProperty p)
        {
            MessageBox.Show(
                $"{p.Name}\n{p.Location}\n\n{p.Description}\n\n{p.Summary}\n{p.Status} · {p.PriceWithUnit}",
                "Property details");
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

            var sections = PropertyStore.BuildStatusSections(pool, _filter);

            icListSections.ItemsSource = sections;
            icGallerySections.ItemsSource = sections;
            txtEmpty.Visibility = sections.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static bool Has(string text, string query) =>
            !string.IsNullOrEmpty(text) && text.Contains(query, StringComparison.OrdinalIgnoreCase);
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