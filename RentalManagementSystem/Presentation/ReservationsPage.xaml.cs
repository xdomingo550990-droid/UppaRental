#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RentalManagementSystem.Presentation;

// ===================================================================
//  Simple in-memory models. Later you can move these into your Model
//  folder and load/save them through your DAO / Service layer.
// ===================================================================

/// <summary>One unit tile in the grid. Changing ReservedBy / IsSelected updates the screen instantly.</summary>
public class UnitTile : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public UnitTile(string code, string typeName, decimal monthlyRate)
    {
        Code = code; TypeName = typeName; MonthlyRate = monthlyRate;
    }

    public string Code { get; }
    public string TypeName { get; }
    public decimal MonthlyRate { get; }

    private string _reservedBy;
    private bool _isSelected;

    public string ReservedBy
    {
        get => _reservedBy;
        set
        {
            _reservedBy = value;
            Raise(nameof(ReservedBy)); Raise(nameof(IsReserved)); Raise(nameof(ReservedText));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; Raise(nameof(IsSelected)); }
    }

    public bool IsReserved => !string.IsNullOrEmpty(ReservedBy);
    public string Title => $"Unit {Code}";
    public string RateText => $"{BookingItem.Peso}{MonthlyRate.ToString("N0", BookingItem.Us)}/mo";
    public string ReservedText => $"Reserved by: {ReservedBy}";

    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>One saved reservation (a row in the log).</summary>
public class BookingItem
{
    public const string Peso = "\u20B1";
    public const decimal DownRate = 0.20m;
    public static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

    public string Id { get; set; } = "";
    public string RenterName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string UnitCode { get; set; } = "";
    public string UnitType { get; set; } = "";
    public decimal MonthlyRate { get; set; }
    public DateTime MoveIn { get; set; }
    public int Months { get; set; }

    // calculated
    public decimal TotalRent => MonthlyRate * Months;
    public decimal Downpayment => Math.Round(TotalRent * DownRate, 2);

    // shown in the log
    public string UnitText => $"Unit {UnitCode}";
    public string RenterSub => $"{Id} \u00B7 {Phone}";
    public string MoveInText => MoveIn.ToString("MMM d, yyyy", Us);
    public string TermText => Months == 1 ? "1 month" : $"{Months} months";
    public string TotalText => Money(TotalRent);
    public string DownText => Money(Downpayment);

    public static string Money(decimal v) => Peso + v.ToString("N2", Us);
}

// ===================================================================
//  The page
// ===================================================================

public partial class ReservationsPage : UserControl
{
    private readonly List<UnitTile> _units = new()
    {
        new UnitTile("101", "Studio",       10000m),
        new UnitTile("102", "Studio",       10000m),
        new UnitTile("201", "1-Bedroom",    15000m),
        new UnitTile("202", "1-Bedroom",    15000m),
        new UnitTile("203", "2-Bedroom",    20000m),
        new UnitTile("204", "2-Bedroom",    20000m),
        new UnitTile("301", "Family Suite", 25000m),
        new UnitTile("302", "Family Suite", 25000m),
        new UnitTile("401", "Penthouse",    30000m),
        new UnitTile("402", "Penthouse",    30000m),
    };

    private readonly ObservableCollection<BookingItem> _bookings = new();
    private UnitTile _selected;
    private int _nextId = 1001;
    private bool _ready;

    public ReservationsPage()
    {
        InitializeComponent();

        UnitList.ItemsSource = _units;
        BookingGrid.ItemsSource = _bookings;

        SeedSampleData();
        ResetForm();

        _ready = true;
        UpdateSummary();
        UpdateLog();
    }

    // -----------------------------------------------------------------
    //  Sample data (delete this once your DAO supplies real reservations)
    // -----------------------------------------------------------------
    private void SeedSampleData()
    {
        Seed("Maria Santos",    "+63 917 123 4567", "maria.santos@email.com", "101",  3,  6);
        Seed("Juan Dela Cruz",  "+63 918 555 0142", "juan.dc@email.com",      "203", 10, 12);
        Seed("Mark Villanueva", "+63 917 808 9090", "mark.v@email.com",       "401", 20, 12);
    }

    private void Seed(string name, string phone, string email, string unitCode, int moveInDays, int months)
    {
        var unit = _units.First(u => u.Code == unitCode);
        _bookings.Add(new BookingItem
        {
            Id = "R-" + _nextId++,
            RenterName = name, Phone = phone, Email = email,
            UnitCode = unit.Code, UnitType = unit.TypeName, MonthlyRate = unit.MonthlyRate,
            MoveIn = DateTime.Today.AddDays(moveInDays), Months = months
        });
        unit.ReservedBy = name;
    }

    // -----------------------------------------------------------------
    //  Unit grid: only available units can be clicked
    // -----------------------------------------------------------------
    private void Unit_Click(object sender, MouseButtonEventArgs e)
    {
        var tile = (sender as FrameworkElement)?.DataContext as UnitTile;
        if (tile == null || tile.IsReserved) return;

        if (_selected == tile)                       // click again = unselect
        {
            tile.IsSelected = false;
            _selected = null;
        }
        else
        {
            if (_selected != null) _selected.IsSelected = false;
            tile.IsSelected = true;
            _selected = tile;
        }

        FormMessage.Text = "";
        UpdateSummary();
    }

    // -----------------------------------------------------------------
    //  Live calculation: total rent and 20% downpayment
    // -----------------------------------------------------------------
    private void Duration_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !e.Text.All(char.IsDigit);                  // numbers only

    private void Duration_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_ready) UpdateSummary();
    }

    private int ParseMonths() => int.TryParse(DurationBox.Text.Trim(), out int m) ? m : 0;

    private void UpdateSummary()
    {
        int months = Math.Max(0, ParseMonths());
        decimal rate = _selected?.MonthlyRate ?? 0m;
        decimal total = rate * months;
        decimal down = Math.Round(total * BookingItem.DownRate, 2);

        SumRate.Text = _selected == null ? "\u2014" : BookingItem.Money(rate) + " / month";
        SumTotal.Text = BookingItem.Money(total);
        SumDown.Text = BookingItem.Money(down);

        if (_selected == null)
        {
            SelectedUnitText.Text = "No unit selected \u2013 click an available unit on the left.";
            BannerText.Text = "Select an available unit to see the required 20% downpayment.";
        }
        else
        {
            SelectedUnitText.Text = $"{_selected.Title} \u00B7 {_selected.TypeName} \u00B7 {_selected.RateText}";
            BannerText.Text = months < 1
                ? "Enter the rental duration (in months) to compute the downpayment."
                : $"To reserve this unit, an initial 20% downpayment of {BookingItem.Money(down)} is required.";
        }
    }

    // -----------------------------------------------------------------
    //  Reserve Unit
    // -----------------------------------------------------------------
    private void Reserve_Click(object sender, RoutedEventArgs e)
    {
        string name = NameBox.Text.Trim();
        string phone = PhoneBox.Text.Trim();
        string email = EmailBox.Text.Trim();
        int months = ParseMonths();

        // ---- validation ----
        if (_selected == null) { ShowMessage("Please click an available unit first.", true); return; }
        if (name.Length == 0) { ShowMessage("Please enter the renter's full name.", true); return; }
        if (phone.Length == 0) { ShowMessage("Please enter a contact number.", true); return; }
        if (email.Length > 0 && (!email.Contains("@") || !email.Contains(".")))
        { ShowMessage("That email address doesn't look right.", true); return; }
        if (!MoveInPicker.SelectedDate.HasValue) { ShowMessage("Please pick a move-in date.", true); return; }
        if (MoveInPicker.SelectedDate.Value.Date < DateTime.Today)
        { ShowMessage("The move-in date can't be in the past.", true); return; }
        if (months < 1 || months > 60) { ShowMessage("Duration must be between 1 and 60 months.", true); return; }

        var unit = _selected;
        decimal total = unit.MonthlyRate * months;
        decimal down = Math.Round(total * BookingItem.DownRate, 2);

        // The unit is only blocked once the 20% downpayment is received
        var answer = MessageBox.Show(
            $"Confirm that the 20% downpayment of {BookingItem.Money(down)} for {unit.Title} " +
            $"has been received from {name}?",
            "Confirm downpayment", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        // ---- save ----
        var booking = new BookingItem
        {
            Id = "R-" + _nextId++,
            RenterName = name, Phone = phone, Email = email,
            UnitCode = unit.Code, UnitType = unit.TypeName, MonthlyRate = unit.MonthlyRate,
            MoveIn = MoveInPicker.SelectedDate.Value.Date, Months = months
        };
        _bookings.Insert(0, booking);

        // TODO: save `booking` through your DAO / Service here

        unit.IsSelected = false;
        unit.ReservedBy = name;          // tile turns gray, unclickable, "Reserved by: name"
        _selected = null;

        ResetForm();
        UpdateSummary();
        UpdateLog();
        ShowMessage($"{unit.Title} reserved for {name}. Downpayment {BookingItem.Money(down)} recorded.", false);
    }

    // -----------------------------------------------------------------
    //  Cancel a reservation (frees the unit again)
    // -----------------------------------------------------------------
    private void CancelRow_Click(object sender, RoutedEventArgs e)
    {
        var booking = (sender as FrameworkElement)?.DataContext as BookingItem;
        if (booking == null) return;

        var answer = MessageBox.Show(
            $"Cancel reservation {booking.Id} for {booking.RenterName} ({booking.UnitText})?\n" +
            "The unit will become available again.",
            "Cancel reservation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        _bookings.Remove(booking);

        var unit = _units.FirstOrDefault(u => u.Code == booking.UnitCode);
        if (unit != null) unit.ReservedBy = null;   // tile becomes clickable again

        // TODO: delete / cancel `booking` through your DAO / Service here
        UpdateLog();
    }

    // -----------------------------------------------------------------
    //  Small helpers
    // -----------------------------------------------------------------
    private void ResetForm()
    {
        NameBox.Text = "";
        PhoneBox.Text = "";
        EmailBox.Text = "";
        MoveInPicker.SelectedDate = DateTime.Today;
        DurationBox.Text = "6";
    }

    private void UpdateLog()
    {
        int n = _bookings.Count;
        LogCountText.Text = n == 1 ? "1 active reservation" : $"{n} active reservations";
        EmptyText.Visibility = n == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowMessage(string text, bool isError)
    {
        FormMessage.Text = text;
        FormMessage.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(isError ? "#C0392B" : "#1E6B3B");
    }
}