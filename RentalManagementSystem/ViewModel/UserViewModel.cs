using RentalManagementSystem.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace RentalManagementSystem.ViewModel
{
    public class UserViewModel : INotifyPropertyChanged
    {
     
        public event PropertyChangedEventHandler  PropertyChanged;
        // OnPropertyChanged method - ADD THIS

        public void OnPropertyChanged([CallerMemberName] string
        propertyName = "")
        {
            PropertyChanged?.Invoke(this, new
           PropertyChangedEventArgs(propertyName));
        }

        // attributes
        private User _user;
        public User User
        {
            get => _user;
            set
            {
                _user = value;
                OnPropertyChanged(nameof(User));
            }
        }

    }
}
