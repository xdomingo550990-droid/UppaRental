using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace RentalManagementSystem.ViewModel
{
    public class UserReservationViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler
       PropertyChanged;
        // OnPropertyChanged method - ADD THIS
        public void OnPropertyChanged([CallerMemberName] string
        propertyName = "")

        {
            PropertyChanged?.Invoke(this, new
           PropertyChangedEventArgs(propertyName));
        }
    }
}
