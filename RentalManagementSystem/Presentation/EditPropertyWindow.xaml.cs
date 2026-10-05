using Microsoft.Win32;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace RentalManagementSystem.Presentation
{
    /// <summary>
    /// Edit popup for one property. The property is only changed when Save is pressed
    /// and every field is valid, so Cancel never leaves a half-edited property behind.
    /// </summary>
    public partial class EditPropertyWindow : Window
    {
        private readonly RentalProperty _property;

        public EditPropertyWindow(RentalProperty property)
        {
            InitializeComponent();
            _property = property ?? throw new ArgumentNullException(nameof(property));

            txtSubtitle.Text = $"Editing \"{_property.Name}\"";
            txtName.Text = _property.Name;
            txtLocation.Text = _property.Location;
            cmbType.Text = _property.RoomType;
            txtBedrooms.Text = _property.Bedrooms.ToString(CultureInfo.InvariantCulture);
            txtBathrooms.Text = _property.Bathrooms.ToString(CultureInfo.InvariantCulture);
            txtSqFt.Text = _property.SqFt.ToString(CultureInfo.InvariantCulture);
            txtPrice.Text = _property.Price.ToString("0.##", CultureInfo.InvariantCulture);
            txtDescription.Text = _property.Description;
            txtPhoto.Text = _property.ImagePath;

            Select(cmbTerm, _property.Term);
            Select(cmbStatus, _property.Status);
        }

        private static void Select(ComboBox box, string value)
        {
            foreach (ComboBoxItem item in box.Items)
            {
                if (string.Equals(item.Content as string, value, StringComparison.OrdinalIgnoreCase))
                {
                    box.SelectedItem = item;
                    return;
                }
            }
            box.SelectedIndex = 0;
        }

        private static string SelectedText(ComboBox box) =>
            (box.SelectedItem as ComboBoxItem)?.Content as string ?? "";

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
            // ----- validate everything first -----
            string name = txtName.Text.Trim();
            if (name.Length == 0) { Fail("Please enter a property name."); return; }

            if (!int.TryParse(txtBedrooms.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int bedrooms) || bedrooms < 0)
            { Fail("Bedrooms must be a whole number, 0 or more."); return; }

            if (!int.TryParse(txtBathrooms.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int bathrooms) || bathrooms < 0)
            { Fail("Bathrooms must be a whole number, 0 or more."); return; }

            string sqftText = txtSqFt.Text.Replace(",", "").Trim();
            int sqft = 0;
            if (sqftText.Length > 0 &&
                (!int.TryParse(sqftText, NumberStyles.Integer, CultureInfo.InvariantCulture, out sqft) || sqft < 0))
            { Fail("Size must be a whole number, 0 or more (or leave it empty)."); return; }

            string priceText = txtPrice.Text.Replace("₱", "").Replace(",", "").Trim();
            if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price) || price < 0)
            { Fail("Price must be a number, 0 or more."); return; }

            // ----- apply -----
            string roomType = (cmbType.Text ?? "").Trim();

            _property.Name = name;
            _property.Location = txtLocation.Text.Trim();
            _property.RoomType = roomType;
            _property.Type = roomType;
            _property.Bedrooms = bedrooms;
            _property.Bathrooms = bathrooms;
            _property.SqFt = sqft;
            _property.Price = price;
            _property.Term = SelectedText(cmbTerm);
            _property.Status = SelectedText(cmbStatus);
            _property.Description = txtDescription.Text.Trim();

            string photo = txtPhoto.Text.Trim();
            _property.ImagePath = photo.Length > 0 ? photo : PropertyStore.DefaultImage;

            DialogResult = true;
        }

        private void Fail(string message) => txtError.Text = message;
    }
}