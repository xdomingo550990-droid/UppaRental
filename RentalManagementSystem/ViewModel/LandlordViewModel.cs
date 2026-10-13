using RentalManagementSystem.DAO;
using RentalManagementSystem.Model;
using RentalManagementSystem.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RentalManagementSystem.ViewModel
{
    public class LandlordViewModel : INotifyPropertyChanged
    {
        private Property? _newProperty;

        public ObservableCollection<Property> Properties { get; } = new ObservableCollection<Property>();

        public Property? NewProperty
        {
            get => _newProperty;
            set
            {
                _newProperty = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddPropertyCommand { get; }

        public LandlordViewModel()
        {
            AddPropertyCommand = new UserViewModel.RelayCommand(_ => AddProperty());
            LoadProperties();
        }

        private void LoadProperties()
        {
            try
            {
                Properties.Clear();
                int landlordId = UserSession.LandlordId;
                if (landlordId <= 0) return;   // no landlord logged in: nothing to show

                foreach (var p in PropertyDao.GetByLandlord(landlordId))
                    Properties.Add(p);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Landlord property load failed: {ex.Message}");
            }
        }

        private void AddProperty()
        {
            if (NewProperty == null) return;

            var toAdd = NewProperty;
            NewProperty = null;

            try
            {
                PropertyDao.Insert(toAdd);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"The property was added locally but could not be saved to the database.\n\n{ex.Message}",
                    "Save Warning",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }

            Properties.Insert(0, toAdd);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
