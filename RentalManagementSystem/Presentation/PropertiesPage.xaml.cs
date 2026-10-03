using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class UnitsPage : UserControl
    {
        public ObservableCollection<UnitViewModel> UnitsList { get; set; } = new ObservableCollection<UnitViewModel>();

        public UnitsPage()
        {
            InitializeComponent();
            LoadInitialUnits();
            dgUnits.ItemsSource = UnitsList;
        }

        private void LoadInitialUnits()
        {
            UnitsList.Add(new UnitViewModel { RoomNo = "Unit 101", Floor = "1st Floor", RoomType = "Studio", MonthlyRate = "₱15,000", Status = "Available", Actions = "Edit / View" });
            UnitsList.Add(new UnitViewModel { RoomNo = "Unit 102", Floor = "1st Floor", RoomType = "1-Bedroom", MonthlyRate = "₱18,000", Status = "Occupied", Actions = "Edit / View" });
            UnitsList.Add(new UnitViewModel { RoomNo = "Unit 201", Floor = "2nd Floor", RoomType = "2-Bedroom", MonthlyRate = "₱25,000", Status = "Reserved", Actions = "Edit / View" });
        }

        private void AddProperty_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddPropertyWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (addWindow.ShowDialog() == true)
            {
                PropertyFormResult result = addWindow.Result;

                var newUnit = new UnitViewModel
                {
                    RoomNo = result.Name,
                    Floor = string.IsNullOrEmpty(result.Floor) ? "N/A" : result.Floor,
                    RoomType = string.IsNullOrEmpty(result.RoomType) ? "N/A" : result.RoomType,
                    MonthlyRate = $"₱{result.MonthlyRent:N0}",
                    Status = result.IsDraft ? "Draft" : result.Status,
                    Actions = "Edit / View"
                };

                UnitsList.Insert(0, newUnit);
            }
        }

        private void dgUnits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Selection behavior handling
        }
    }

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