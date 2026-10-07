using Microsoft.Win32;
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
        private readonly RentalProperty _property;

        public EditPropertyWindow(RentalProperty property)
        {
            InitializeComponent();
            _property = property ?? throw new ArgumentNullException(nameof(property));

            txtSubtitle.Text = $"Editing \"{_property.Name}\"";
            txtName.Text = _property.Name;
            txtLocation.Text = _property.Location;
            txtFloors.Text = Math.Max(1, _property.Floors).ToString(CultureInfo.InvariantCulture);
            txtPrice.Text = _property.Price.ToString("0.##", CultureInfo.InvariantCulture);
            txtBedrooms.Text = _property.Bedrooms.ToString(CultureInfo.InvariantCulture);
            txtBathrooms.Text = _property.Bathrooms.ToString(CultureInfo.InvariantCulture);
            txtSqFt.Text = _property.SqFt.ToString(CultureInfo.InvariantCulture);
            txtCapacity.Text = Math.Max(1, _property.GetMaxCapacity()).ToString(CultureInfo.InvariantCulture);
            txtDescription.Text = _property.Description;
            txtPhoto.Text = _property.ImagePath;

            // Populate Utility Billing setup from existing property settings
            var utilities = _property.GetUtilities();

            if (utilities.Water != null)
            {
                chkWater.IsChecked = !utilities.Water.Included;
                txtWaterRate.Text = utilities.Water.Amount > 0 ? utilities.Water.Amount.ToString("0.##", CultureInfo.InvariantCulture) : "100";
            }

            if (utilities.Electricity != null)
            {
                chkElectricity.IsChecked = !utilities.Electricity.Included;
                txtElectricityRate.Text = utilities.Electricity.Amount > 0 ? utilities.Electricity.Amount.ToString("0.##", CultureInfo.InvariantCulture) : "15";
            }

            if (utilities.Wifi != null)
            {
                chkWifi.IsChecked = !utilities.Wifi.Included;
                txtWifiRate.Text = utilities.Wifi.Amount > 0 ? utilities.Wifi.Amount.ToString("0.##", CultureInfo.InvariantCulture) : "99";
            }

            cmbType.SelectedIndex = -1;
            foreach (ComboBoxItem item in cmbType.Items)
            {
                if (string.Equals(item.Content as string, _property.RoomType, StringComparison.OrdinalIgnoreCase))
                {
                    cmbType.SelectedItem = item;
                    break;
                }
            }

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
            // Validation
            string name = txtName.Text.Trim();
            if (name.Length == 0) { Fail("Please enter a property name."); return; }

            if (!int.TryParse(txtFloors.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int floors) || floors < 1)
            { Fail("Number of floors must be a whole number of at least 1."); return; }

            string roomType = SelectedText(cmbType);
            if (roomType.Length == 0) { Fail("Please choose a property type."); return; }

            string priceText = txtPrice.Text.Replace("₱", "").Replace(",", "").Trim();
            if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price) || price < 0)
            { Fail("Price must be a number, 0 or more."); return; }

            if (!int.TryParse(txtBedrooms.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int bedrooms) || bedrooms < 0)
            { Fail("Bedrooms must be a whole number, 0 or more."); return; }

            if (!int.TryParse(txtBathrooms.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int bathrooms) || bathrooms < 0)
            { Fail("Bathrooms must be a whole number, 0 or more."); return; }

            string sqftText = txtSqFt.Text.Replace(",", "").Trim();
            int sqft = 0;
            if (sqftText.Length > 0 &&
                (!int.TryParse(sqftText, NumberStyles.Integer, CultureInfo.InvariantCulture, out sqft) || sqft < 0))
            { Fail("Size must be a whole number, 0 or more (or leave it empty)."); return; }

            if (!int.TryParse(txtCapacity.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int capacity) || capacity < 1)
            { Fail("Maximum capacity must be a whole number of at least 1."); return; }

            // Validate utility input rates
            if (!decimal.TryParse(txtWaterRate.Text.Replace(",", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal waterFee) || waterFee < 0)
            { Fail("Please enter a valid water rate."); return; }

            if (!decimal.TryParse(txtElectricityRate.Text.Replace(",", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal electricityFee) || electricityFee < 0)
            { Fail("Please enter a valid electricity rate."); return; }

            if (!decimal.TryParse(txtWifiRate.Text.Replace(",", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal wifiFee) || wifiFee < 0)
            { Fail("Please enter a valid Wi-Fi rate."); return; }

            // Apply updates to model
            _property.Name = name;
            _property.Location = txtLocation.Text.Trim();
            _property.RoomType = roomType;
            _property.Type = roomType;
            _property.Floors = floors;
            _property.Price = price;
            _property.Bedrooms = bedrooms;
            _property.Bathrooms = bathrooms;
            _property.SqFt = sqft;
            _property.SetMaxCapacity(capacity);

            _property.SetUtilities(new UtilitySettings
            {
                Water = chkWater.IsChecked == true
                    ? new UtilityOption { Included = false, Billing = UtilityBilling.FixedMonthlyFee, Amount = waterFee }
                    : new UtilityOption { Included = true },
                Electricity = chkElectricity.IsChecked == true
                    ? new UtilityOption { Included = false, Billing = UtilityBilling.SubMetered, Amount = electricityFee }
                    : new UtilityOption { Included = true },
                Wifi = chkWifi.IsChecked == true
                    ? new UtilityOption { Included = false, Billing = UtilityBilling.FixedMonthlyFee, Amount = wifiFee }
                    : new UtilityOption { Included = true }
            });

            _property.Status = SelectedText(cmbStatus);
            _property.Description = txtDescription.Text.Trim();

            string photo = txtPhoto.Text.Trim();
            _property.ImagePath = photo.Length > 0 ? photo : PropertyStore.DefaultImage;

            DialogResult = true;
        }

        // Number-only input constraints
        private static bool AllDigits(string? text) =>
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