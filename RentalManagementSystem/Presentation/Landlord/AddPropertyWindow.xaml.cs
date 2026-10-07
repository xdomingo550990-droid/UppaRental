using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RentalManagementSystem.Presentation
{
    public class PropertyFormResult
    {
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public string Floor { get; set; } = "";        // same value as Floors, kept for older code
        public int Floors { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public string RoomType { get; set; } = "";
        public decimal DailyRent { get; set; }          // was MonthlyRent
        public decimal SecurityDeposit { get; set; }
        public int SizeSqFt { get; set; }
        public int MaxCapacity { get; set; }
        public string Status { get; set; } = "";
        public bool IsDraft { get; set; }

        public UtilitySettings Utilities { get; set; } = new UtilitySettings();

        public bool WaterIncluded { get; set; }
        public bool ElectricityMetered { get; set; }   // true when electricity is NOT included
        public bool WifiIncluded { get; set; }
        public List<string> Amenities { get; set; } = new();
        public List<string> PhotoPaths { get; set; } = new();
        public string Notes { get; set; } = "";
    }

    public partial class AddPropertyWindow : Window
    {
        private int _currentStep = 1;
        private readonly List<string> _uploadedPhotoPaths = new();

        public PropertyFormResult Result { get; private set; } = new PropertyFormResult();

        public AddPropertyWindow()
        {
            InitializeComponent();
            UpdateStepUI();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void Primary_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            if (_currentStep == 1)
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    ShowError("Please enter a property or unit name.");
                    return;
                }
                if (!int.TryParse(txtFloors.Text.Trim(), out int floors) || floors < 1)
                {
                    ShowError("Number of floors must be a whole number of at least 1.");
                    return;
                }
                if (!int.TryParse(txtBedrooms.Text.Trim(), out _))
                {
                    ShowError("Please enter the number of bedrooms (0 if none).");
                    return;
                }
                if (!int.TryParse(txtBathrooms.Text.Trim(), out _))
                {
                    ShowError("Please enter the number of bathrooms (0 if none).");
                    return;
                }
                if (!int.TryParse(txtCapacity.Text.Trim(), out int capacity) || capacity < 1)
                {
                    ShowError("Maximum capacity must be a whole number of at least 1.");
                    return;
                }
                _currentStep = 2;
                UpdateStepUI();
            }
            else if (_currentStep == 2)
            {
                if (string.IsNullOrWhiteSpace(txtRent.Text) || !decimal.TryParse(txtRent.Text.Replace(",", "").Trim(), out _))
                {
                    ShowError("Please enter a valid daily rent amount.");
                    return;
                }
                _currentStep = 3;
                UpdateStepUI();
            }
            else if (_currentStep == 3)
            {
                PopulateResult(isDraft: false);
                DialogResult = true;
                Close();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 1)
            {
                _currentStep--;
                ErrorText.Visibility = Visibility.Collapsed;
                UpdateStepUI();
            }
        }

        private void SaveDraft_Click(object sender, RoutedEventArgs e)
        {
            PopulateResult(isDraft: true);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void PopulateResult(bool isDraft)
        {
            decimal.TryParse(txtRent.Text.Replace(",", "").Trim(), out decimal rent);
            decimal.TryParse(txtDeposit.Text.Replace(",", "").Trim(), out decimal deposit);

            int.TryParse(txtCapacity.Text.Trim(), out int capacity);   // 0 = not set (drafts only)

            // Size is stored in sq ft; convert if entered in sqm.
            decimal.TryParse(txtSize.Text.Replace(",", "").Trim(), NumberStyles.Number,
                CultureInfo.InvariantCulture, out decimal size);
            if (rbSqm.IsChecked == true) size *= 10.7639m;

            // Get landlord input rates (default to 0 if invalid or empty)
            decimal.TryParse(txtWaterRate.Text.Replace(",", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal waterFee);
            decimal.TryParse(txtElectricityRate.Text.Replace(",", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal electricityFee);
            decimal.TryParse(txtWifiRate.Text.Replace(",", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal wifiFee);

            // Checked   = billed to the tenant at the specified rate (not included in rent)
            // Unchecked = included in rent
            var utilities = new UtilitySettings
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
            };

            int.TryParse(txtFloors.Text.Trim(), out int floors);
            int.TryParse(txtBedrooms.Text.Trim(), out int bedrooms);
            int.TryParse(txtBathrooms.Text.Trim(), out int bathrooms);

            // New properties always start as Available.
            const string status = "Available";

            var amenities = new List<string>();
            if (tbAircon.IsChecked == true) amenities.Add("Air Conditioning");
            if (tbFurnished.IsChecked == true) amenities.Add("Fully Furnished");
            if (tbBalcony.IsChecked == true) amenities.Add("Balcony");
            if (tbPets.IsChecked == true) amenities.Add("Pet Friendly");
            if (tbParking.IsChecked == true) amenities.Add("Parking Space");

            Result = new PropertyFormResult
            {
                Name = txtName.Text.Trim(),
                Location = txtLocation.Text.Trim(),
                Floor = floors.ToString(CultureInfo.InvariantCulture),
                Floors = floors,
                Bedrooms = bedrooms,
                Bathrooms = bathrooms,
                RoomType = (cmbRoomType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                DailyRent = rent,
                SecurityDeposit = deposit,
                SizeSqFt = (int)Math.Round(size),
                MaxCapacity = capacity,
                Status = status,
                IsDraft = isDraft,
                Utilities = utilities,
                WaterIncluded = utilities.Water.Included,
                ElectricityMetered = !utilities.Electricity.Included,
                WifiIncluded = utilities.Wifi.Included,
                Amenities = amenities,
                PhotoPaths = new List<string>(_uploadedPhotoPaths),
                Notes = txtNotes.Text.Trim()
            };
        }

        // ---------- number-only text boxes ----------

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

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void UpdateStepUI()
        {
            Step1Panel.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Panel.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Panel.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

            btnBack.Visibility = _currentStep > 1 ? Visibility.Visible : Visibility.Collapsed;
            btnPrimary.Content = _currentStep == 3 ? "Save Unit" : "Next →";

            StepSubtitle.Text = _currentStep switch
            {
                1 => "Step 1 of 3 · Basic Info",
                2 => "Step 2 of 3 · Financials",
                3 => "Step 3 of 3 · Amenities & Photos",
                _ => ""
            };

            SetIndicator(Dot1, Num1, Label1, Line1, active: _currentStep >= 1, current: _currentStep == 1);
            SetIndicator(Dot2, Num2, Label2, Line2, active: _currentStep >= 2, current: _currentStep == 2);
            SetIndicator(Dot3, Num3, Label3, null, active: _currentStep >= 3, current: _currentStep == 3);
        }

        private static void SetIndicator(Border dot, TextBlock num, TextBlock label, Border? line, bool active, bool current)
        {
            var darkGreen = (Brush)new BrushConverter().ConvertFrom("#1E3223")!;
            var lightGreen = (Brush)new BrushConverter().ConvertFrom("#E3EAE5")!;
            var textMuted = (Brush)new BrushConverter().ConvertFrom("#7C8F80")!;

            if (current || active)
            {
                dot.Background = darkGreen;
                num.Foreground = Brushes.White;
                label.Foreground = darkGreen;
            }
            else
            {
                dot.Background = lightGreen;
                num.Foreground = textMuted;
                label.Foreground = textMuted;
            }

            if (line != null)
            {
                line.Background = active ? darkGreen : (Brush)new BrushConverter().ConvertFrom("#DDE5DF")!;
            }
        }

        private void DropZone_Click(object sender, MouseButtonEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    _uploadedPhotoPaths.Add(filename);
                }
                UpdatePhotoCount();
            }
        }

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                foreach (string file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                    {
                        _uploadedPhotoPaths.Add(file);
                    }
                }
                UpdatePhotoCount();
            }
        }

        private void UpdatePhotoCount()
        {
            if (_uploadedPhotoPaths.Count > 0)
            {
                PhotoCountText.Text = $"✓ {_uploadedPhotoPaths.Count} photo(s) selected.";
                PhotoCountText.Visibility = Visibility.Visible;
            }
            else
            {
                PhotoCountText.Visibility = Visibility.Collapsed;
            }
        }
    }
}