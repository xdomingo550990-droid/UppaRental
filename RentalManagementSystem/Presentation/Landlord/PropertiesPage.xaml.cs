#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RentalManagementSystem.Model;
using RentalManagementSystem.ViewModel;

namespace RentalManagementSystem.Presentation
{
    public partial class UnitsPage : UserControl
    {
        private string _filter = "All";
        private readonly LandlordViewModel _viewModel;

        public UnitsPage()
        {
            InitializeComponent();
            _viewModel = new LandlordViewModel();
            DataContext = _viewModel;

            // Refresh layout whenever properties in DB collection change
            _viewModel.Properties.CollectionChanged += (s, e) => LoadSections();

            LoadSections();
        }

        // ---------- Add Property Popup ----------

        private void AddProperty_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddPropertyWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (addWindow.ShowDialog() == true && addWindow.Result != null)
            {
                var form = addWindow.Result;

                // Map PropertyFormResult to Model.Property
                _viewModel.NewProperty = new Property
                {
                    Name = string.IsNullOrWhiteSpace(form.Name) ? "Untitled Property" : form.Name,
                    Location = string.IsNullOrWhiteSpace(form.Location) ? "N/A" : form.Location,
                    MonthlyRent = (int)form.MonthlyRent,
                    NumberOfRooms = form.NumberOfRooms,
                    NumberOfFloors = form.NumberOfFloors,
                    NumberofBathrooms = form.NumberOfBathrooms,
                    MaximumCapacity = form.MaximumCapacity,
                    SizeUnit = form.SizeUnit,
                    SecurityDeposit = (int)form.SecurityDeposit,
                    PropertyType = form.PropertyType,
                    Amenities = form.Amenities ?? new List<Amenities>(),
                    Notes = form.Notes ?? "",
                    Description = form.Notes ?? "",
                    Status = string.IsNullOrEmpty(form.Status) ? "Available" : form.Status
                };

                // Execute directly to trigger MySQL INSERT
                _viewModel.AddPropertyCommand.Execute(null);
            }
        }

        // ---------- Item Actions ----------

        private void Item_Click(object sender, RoutedEventArgs e)
        {
            // Changed type check from RentalProperty to Property
            if (e.OriginalSource is not Button button || button.DataContext is not Property property)
                return;

            switch (button.Tag as string)
            {
                case "Edit":
                    EditProperty(property);
                    e.Handled = true;
                    break;
                case "Delete":
                    DeleteProperty(property);
                    e.Handled = true;
                    break;
                case "Read":
                    ShowDetails(property);
                    e.Handled = true;
                    break;
            }
        }

        // Changed parameter type from RentalProperty to Property
        private void EditProperty(Property property)
        {
            var editWindow = new EditPropertyWindow(property)
            {
                Owner = Window.GetWindow(this)
            };

            if (editWindow.ShowDialog() == true)
            {
                try { RentalManagementSystem.DAO.PropertyDao.Update(property); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Saved locally, but the database update failed.\n\n{ex.Message}",
                        "Save Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                LoadSections();
            }
        }

        private void DeleteProperty(Property property)
        {
            var answer = MessageBox.Show(
                Window.GetWindow(this),
                $"Delete \"{property.Name}\"?\n\nThis removes it permanently from your database.",
                "Delete Property",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes) return;

            try { RentalManagementSystem.DAO.PropertyDao.Delete(property.PropertyId); }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete from the database.\n\n{ex.Message}",
                    "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _viewModel.Properties.Remove(property);
            LoadSections();
        }

        private void ShowDetails(Property property)
        {
            MessageBox.Show(
                $"Name: {property.Name}\n" +
                $"Location: {property.Location}\n" +
                $"Monthly Rent: ₱{property.MonthlyRent:N0}\n" +
                $"Electric Bill: ₱{property.ElectricBill:N0}\n" +
                $"Water Bill: ₱{property.WaterBill:N0}\n" +
                $"Status: {property.Status}\n\n" +
                $"Notes:\n{property.Notes}",
                "Property Details");
        }

        // ---------- View Switch & Search ----------

        private void View_Checked(object sender, RoutedEventArgs e)
        {
            if (scrList == null || scrGallery == null || rbGallery == null) return;

            bool isGallery = rbGallery.IsChecked == true;
            scrList.Visibility = isGallery ? Visibility.Collapsed : Visibility.Visible;
            scrGallery.Visibility = isGallery ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Category_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
                _filter = rb.Tag?.ToString() ?? "All";

            if (icListSections == null) return;
            LoadSections();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (icListSections == null) return;
            LoadSections();
        }

        // ---------- Load & Group Sections ----------

        private void LoadSections()
        {
            string query = txtSearch?.Text?.Trim() ?? "";

            IEnumerable<Property> pool = _viewModel.Properties;

            if (!string.IsNullOrEmpty(query))
            {
                pool = pool.Where(p =>
                    Has(p.Name, query) ||
                    Has(p.Location, query) ||
                    Has(p.Description, query) ||
                    Has(p.Notes, query) ||
                    Has(p.Status, query));
            }

            var sections = BuildStatusSections(pool, _filter);

            icListSections.ItemsSource = sections;
            icGallerySections.ItemsSource = sections;
            txtEmpty.Visibility = sections.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static List<PropertyStatusGroup> BuildStatusSections(IEnumerable<Property> properties, string filter)
        {
            if (filter != "All")
            {
                properties = properties.Where(p => string.Equals(p.Status, filter, StringComparison.OrdinalIgnoreCase));
            }

            return properties
                .GroupBy(p => string.IsNullOrEmpty(p.Status) ? "Available" : p.Status)
                .Select(g => new PropertyStatusGroup
                {
                    StatusName = g.Key,
                    Items = g.ToList()
                })
                .ToList();
        }

        private static bool Has(string text, string query) =>
            !string.IsNullOrEmpty(text) && text.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    public class PropertyStatusGroup
    {
        public string StatusName { get; set; } = "";
        public List<Property> Items { get; set; } = new();
    }
}