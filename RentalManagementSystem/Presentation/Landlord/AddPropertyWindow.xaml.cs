using Microsoft.Win32;
using RentalManagementSystem.Model;
using RentalManagementSystem.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RentalManagementSystem.Presentation
{
    public partial class AddPropertyWindow : Window
    {
        private int _currentStep = 1;
        private readonly List<string> _uploadedPhotoPaths = new List<string>();
        public LandlordViewModel ViewModel { get; }
        public PropertyFormResult Result { get; private set; }
        public AddPropertyWindow()
        {
            InitializeComponent();
            ViewModel = new LandlordViewModel();
            DataContext = ViewModel;

            UpdateStepUI();
        }

        public AddPropertyWindow(LandlordViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? new LandlordViewModel();
            DataContext = ViewModel;

            UpdateStepUI();
        }

        #region Wizard Navigation & Step Handling

        private void Primary_Click(object sender, RoutedEventArgs e)
        {
            ClearError();

            if (!ValidateCurrentStep())
                return;

            if (_currentStep < 3)
            {
                _currentStep++;
                UpdateStepUI();
            }
            else
            {
                SaveProperty(isDraft: false);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            ClearError();
            if (_currentStep > 1)
            {
                _currentStep--;
                UpdateStepUI();
            }
        }

        private void SaveDraft_Click(object sender, RoutedEventArgs e)
        {
            ClearError();
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ShowError("Please enter at least a Property/Unit Name to save as draft.");
                return;
            }

            SaveProperty(isDraft: true);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UpdateStepUI()
        {
            // Panel Visibilities
            Step1Panel.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Panel.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Panel.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

            // Footer Buttons
            btnBack.Visibility = _currentStep > 1 ? Visibility.Visible : Visibility.Collapsed;
            btnPrimary.Content = _currentStep == 3 ? "Save Property" : "Next →";

            // Header & Indicators
            var activeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3223"));
            var inactiveBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3EAE5"));
            var activeText = Brushes.White;
            var inactiveText = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C8F80"));
            var darkLabel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3223"));
            var mutedLabel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8A9A8D"));

            switch (_currentStep)
            {
                case 1:
                    StepSubtitle.Text = "Step 1 of 3 · Basic Info";

                    Dot1.Background = activeBrush; Num1.Foreground = activeText; Label1.Foreground = darkLabel;
                    Dot2.Background = inactiveBrush; Num2.Foreground = inactiveText; Label2.Foreground = mutedLabel;
                    Dot3.Background = inactiveBrush; Num3.Foreground = inactiveText; Label3.Foreground = mutedLabel;
                    Line1.Background = inactiveBrush; Line2.Background = inactiveBrush;
                    break;

                case 2:
                    StepSubtitle.Text = "Step 2 of 3 · Financials & Utilities";

                    Dot1.Background = activeBrush; Num1.Foreground = activeText; Label1.Foreground = darkLabel;
                    Dot2.Background = activeBrush; Num2.Foreground = activeText; Label2.Foreground = darkLabel;
                    Dot3.Background = inactiveBrush; Num3.Foreground = inactiveText; Label3.Foreground = mutedLabel;
                    Line1.Background = activeBrush; Line2.Background = inactiveBrush;
                    break;

                case 3:
                    StepSubtitle.Text = "Step 3 of 3 · Amenities & Photos";

                    Dot1.Background = activeBrush; Num1.Foreground = activeText; Label1.Foreground = darkLabel;
                    Dot2.Background = activeBrush; Num2.Foreground = activeText; Label2.Foreground = darkLabel;
                    Dot3.Background = activeBrush; Num3.Foreground = activeText; Label3.Foreground = darkLabel;
                    Line1.Background = activeBrush; Line2.Background = activeBrush;
                    break;
            }

            ContentScroll.ScrollToHome();
        }

        private bool ValidateCurrentStep()
        {
            if (_currentStep == 1)
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    ShowError("Property / Unit Name is required.");
                    txtName.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtFloors.Text) || !int.TryParse(txtFloors.Text, out _))
                {
                    ShowError("Please enter a valid number of floors.");
                    txtFloors.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtBedrooms.Text) || !int.TryParse(txtBedrooms.Text, out _))
                {
                    ShowError("Please enter a valid number of bedrooms.");
                    txtBedrooms.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtBathrooms.Text) || !int.TryParse(txtBathrooms.Text, out _))
                {
                    ShowError("Please enter a valid number of bathrooms.");
                    txtBathrooms.Focus();
                    return false;
                }
            }
            else if (_currentStep == 2)
            {
                if (string.IsNullOrWhiteSpace(txtRent.Text) || !decimal.TryParse(txtRent.Text, out decimal rent) || rent <= 0)
                {
                    ShowError("Please enter a valid monthly rent amount.");
                    txtRent.Focus();
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Save Logic & ViewModel Integration

        private void SaveProperty(bool isDraft)
        {
            try
            {
                var amenitiesList = new List<Amenities>();
                if (tbAircon.IsChecked == true) amenitiesList.Add(Amenities.AirConditioning);
                if (tbFurnished.IsChecked == true) amenitiesList.Add(Amenities.FullyFurnished);
                if (tbBalcony.IsChecked == true) amenitiesList.Add(Amenities.Balcony);
                if (tbPets.IsChecked == true) amenitiesList.Add(Amenities.CatFriendly);
                if (tbParking.IsChecked == true) amenitiesList.Add(Amenities.ParkingSpaceIncluded);

                PropertyType parsedType = PropertyType.Studio;
                if (cmbRoomType.SelectedItem is ComboBoxItem item)
                {
                    string selectedText = item.Content.ToString()?.Replace(" ", "") ?? "Studio";
                    Enum.TryParse(selectedText, true, out parsedType);
                }

                // Populate Result before closing
                Result = new PropertyFormResult
                {
                    Name = txtName.Text.Trim(),
                    NumberOfFloors = int.TryParse(txtFloors.Text, out int floors) ? floors : 1,
                    NumberOfRooms = int.TryParse(txtBedrooms.Text, out int beds) ? beds : 1,
                    NumberOfBathrooms = int.TryParse(txtBathrooms.Text, out int baths) ? baths : 1,
                    MaximumCapacity = int.TryParse(txtCapacity.Text, out int cap) ? cap : 2,
                    SizeUnit = int.TryParse(txtSize.Text, out int sz) ? sz : 0,
                    PropertyType = parsedType,
                    MonthlyRent = (int)(decimal.TryParse(txtRent.Text, out decimal rentVal) ? rentVal : 0m),
                    SecurityDeposit = (int)(decimal.TryParse(txtDeposit.Text, out decimal depVal) ? depVal : 0m),
                    Amenities = amenitiesList,
                    PhotoPaths = new List<string>(_uploadedPhotoPaths),
                    Notes = txtNotes.Text.Trim(),
                    Status = isDraft ? "Draft" : "Available"
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to save property: {ex.Message}");
            }
        }

        #endregion

        #region Photo Drag & Drop / File Selection

        private void DropZone_Click(object sender, MouseButtonEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AddPhotoFiles(openFileDialog.FileNames);
            }
        }

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var validImages = files.Where(f =>
                    f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).ToArray();

                AddPhotoFiles(validImages);
            }
        }

        private void AddPhotoFiles(string[] paths)
        {
            foreach (var path in paths)
            {
                if (!_uploadedPhotoPaths.Contains(path))
                {
                    _uploadedPhotoPaths.Add(path);
                }
            }

            if (_uploadedPhotoPaths.Count > 0)
            {
                PhotoCountText.Text = $"✓ {_uploadedPhotoPaths.Count} photo(s) selected: " +
                                     string.Join(", ", _uploadedPhotoPaths.Select(System.IO.Path.GetFileName));
                PhotoCountText.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region Helper Methods & Input Constraints

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void NumberOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, "^[0-9]+$"))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Cancel_Click(sender, e);
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void ClearError()
        {
            ErrorText.Text = string.Empty;
            ErrorText.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}