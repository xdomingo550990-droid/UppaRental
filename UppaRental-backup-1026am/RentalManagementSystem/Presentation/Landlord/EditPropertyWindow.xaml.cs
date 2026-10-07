using Microsoft.Win32;
using RentalManagementSystem.Model;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RentalManagementSystem.Presentation
{
    public partial class EditPropertyWindow : Window
    {
        private readonly Property _property;

        public EditPropertyWindow(Property property)
        {
            InitializeComponent();
            _property = property ?? throw new ArgumentNullException(nameof(property));

            // Basic Information
            txtSubtitle.Text = $"Editing \"{_property.Name}\"";
            txtName.Text = _property.Name;
            txtLocation.Text = _property.Location;
            txtFloors.Text = Math.Max(1, _property.NumberOfFloors).ToString(CultureInfo.InvariantCulture);
            txtBedrooms.Text = _property.NumberOfRooms.ToString(CultureInfo.InvariantCulture);
            txtBathrooms.Text = _property.NumberofBathrooms.ToString(CultureInfo.InvariantCulture);
            txtSqFt.Text = _property.SizeUnit.ToString(CultureInfo.InvariantCulture);
            txtPrice.Text = _property.MonthlyRent.ToString("0.##", CultureInfo.InvariantCulture);
            txtDescription.Text = string.IsNullOrEmpty(_property.Description) ? _property.Notes : _property.Description;
            txtCapacity.Text = Math.Max(1, _property.MaximumCapacity).ToString(CultureInfo.InvariantCulture);

            // Set Property Type ComboBox (Fixed non-nullable enum .ToString())
            SelectTypeCombo(_property.PropertyType.ToString());

            // Set Status ComboBox
            SelectCombo(cmbStatus, _property.Status);
        }

        private void SelectTypeCombo(string typeValue)
        {
            if (cmbType == null) return;
            cmbType.SelectedIndex = -1;
            foreach (ComboBoxItem item in cmbType.Items)
            {
                if (string.Equals(item.Content as string, typeValue, StringComparison.OrdinalIgnoreCase))
                {
                    cmbType.SelectedItem = item;
                    break;
                }
            }
        }

        private static void SelectCombo(ComboBox box, string value)
        {
            if (box == null) return;
            foreach (ComboBoxItem item in box.Items)
            {
                if (string.Equals(item.Content as string, value, StringComparison.OrdinalIgnoreCase))
                {
                    box.SelectedItem = item;
                    return;
                }
            }
            if (box.Items.Count > 0) box.SelectedIndex = 0;
        }

        private static string SelectedText(ComboBox box) =>
            (box?.SelectedItem as ComboBoxItem)?.Content as string ?? "";

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose a photo",
                Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) == true)
                txtPhoto.Text = dialog.FileName;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // ----- Validation -----
            string name = txtName.Text.Trim();
            if (name.Length == 0) { Fail("Please enter a property name."); return; }

            if (!int.TryParse(txtFloors.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int floors) || floors < 1)
            { Fail("Number of floors must be a whole number of at least 1."); return; }

            string roomTypeStr = SelectedText(cmbType);
            if (roomTypeStr.Length == 0) { Fail("Please choose a property type."); return; }

            if (!int.TryParse(txtBedrooms.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int bedrooms) || bedrooms < 0)
            { Fail("Bedrooms must be a whole number, 0 or more."); return; }

            if (!int.TryParse(txtBathrooms.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int bathrooms) || bathrooms < 0)
            { Fail("Bathrooms must be a whole number, 0 or more."); return; }

            string sqftText = txtSqFt.Text.Replace(",", "").Trim();
            int sqft = 0;
            if (sqftText.Length > 0 &&
                (!int.TryParse(sqftText, NumberStyles.Integer, CultureInfo.InvariantCulture, out sqft) || sqft < 0))
            { Fail("Size must be a whole number, 0 or more (or leave empty)."); return; }

            string priceText = txtPrice.Text.Replace("₱", "").Replace(",", "").Trim();
            if (!int.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out int price) || price < 0)
            { Fail("Price must be a number, 0 or more."); return; }

            if (!int.TryParse(txtCapacity.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int capacity) || capacity < 1)
            { Fail("Maximum capacity must be a whole number of at least 1."); return; }

            // ----- Apply changes to local object -----
            _property.Name = name;
            _property.Location = txtLocation.Text.Trim();
            _property.NumberOfFloors = floors;
            _property.NumberOfRooms = bedrooms;
            _property.NumberofBathrooms = bathrooms;
            _property.SizeUnit = sqft;
            _property.MonthlyRent = price;
            _property.MaximumCapacity = capacity;
            _property.Status = SelectedText(cmbStatus);
            _property.Description = txtDescription.Text.Trim();
            _property.Notes = txtDescription.Text.Trim();

            DialogResult = true; // Signal success to UnitsPage
        }

        // ----- Helper Methods for Numeric Input -----
        private static bool AllDigits(string text) =>
            !string.IsNullOrEmpty(text) && text.All(c => c >= '0' && c <= '9');

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !AllDigits(e.Text);

        private void NumberOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)) &&
                e.DataObject.GetData(typeof(string)) is string text && AllDigits(text.Trim()))
                return;
            e.CancelCommand();
        }

        private void Fail(string message) => txtError.Text = message;
    }
}