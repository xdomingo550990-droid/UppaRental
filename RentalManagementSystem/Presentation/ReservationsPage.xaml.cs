#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace RentalManagementSystem.Presentation;

// ===================================================================
//  Simple in-memory models. Later you can move these into your Model
//  folder and load/save them through your DAO / Service layer.
// ===================================================================

public class UnitOption
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Rate { get; set; }   // price per night

    public string Label => $"Unit {Code} \u2013 {Name}  ({ReservationItem.Peso}{Rate:N0}/night)";
}

public class ReservationItem : INotifyPropertyChanged
{
    public const string Peso = "\u20B1";

    public event PropertyChangedEventHandler PropertyChanged;

    // ---- stored data ----
    public string Id { get; set; } = "";
    public string RenterName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public int Guests { get; set; } = 1;
    public string UnitCode { get; set; } = "";
    public string UnitName { get; set; } = "";
    public decimal Rate { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal Deposit { get; set; }
    public decimal AmountPaid { get; set; }
    public string Method { get; set; } = "Cash";
    public string Status { get; set; } = "Pending";
    public string Notes { get; set; } = "";

    // ---- calculated (shown in the list) ----
    public int Nights => Math.Max(1, (CheckOut.Date - CheckIn.Date).Days);
    public decimal RoomCharge => Nights * Rate;
    public decimal TotalDue => RoomCharge + Deposit;
    public decimal Balance => Math.Max(0, TotalDue - AmountPaid);
    public string PaymentStatus => AmountPaid <= 0 ? "Unpaid" : (AmountPaid >= TotalDue ? "Paid" : "Partial");

    public string UnitLabel => $"Unit {UnitCode}";
    public string StayText => $"{CheckIn:MMM d} \u2013 {CheckOut:MMM d}";
    public string NightsText => Nights == 1 ? "1 night" : $"{Nights} nights";
    public string TotalText => Money(TotalDue);
    public string BalanceLine => Balance > 0 ? $"Bal {Money(Balance)}" : "Settled";

    public bool Occupies(DateTime day) => day.Date >= CheckIn.Date && day.Date < CheckOut.Date;

    public static string Money(decimal v) => Peso + v.ToString("N2", CultureInfo.InvariantCulture);

    /// <summary>Tells the DataGrid to refresh every calculated column.</summary>
    public void Notify() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}

// ===================================================================
//  The page
// ===================================================================

public partial class ReservationsPage : UserControl
{
    private readonly ObservableCollection<ReservationItem> _reservations = new();

    private readonly List<UnitOption> _units = new()
    {
        new UnitOption { Code = "101", Name = "Studio",        Rate = 1200 },
        new UnitOption { Code = "102", Name = "1-Bedroom",     Rate = 1800 },
        new UnitOption { Code = "201", Name = "2-Bedroom",     Rate = 2500 },
        new UnitOption { Code = "202", Name = "2-BR Deluxe",   Rate = 3000 },
        new UnitOption { Code = "301", Name = "Penthouse",     Rate = 4500 },
        new UnitOption { Code = "302", Name = "Family Suite",  Rate = 3500 },
    };

    private readonly string[] _methods  = { "Cash", "GCash", "Bank Transfer", "Credit/Debit Card" };
    private readonly string[] _statuses = { "Pending", "Confirmed", "Checked-in", "Completed", "Cancelled" };

    private ICollectionView _view;
    private DateTime _month;
    private DateTime? _selectedDay;
    private ReservationItem _editing;
    private int _nextId = 1001;
    private bool _ready;

    public ReservationsPage()
    {
        InitializeComponent();

        UnitCombo.ItemsSource = _units;
        MethodCombo.ItemsSource = _methods;
        StatusCombo.ItemsSource = _statuses;

        SeedSampleData();

        _view = CollectionViewSource.GetDefaultView(_reservations);
        _view.SortDescriptions.Add(new SortDescription(nameof(ReservationItem.CheckIn), ListSortDirection.Ascending));
        _view.Filter = FilterReservation;
        ReservationGrid.ItemsSource = _view;

        _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        ClearForm();
        _ready = true;
        UpdateSummary();
        RefreshAll();
    }

    // -----------------------------------------------------------------
    //  Sample data (delete this once your DAO supplies real reservations)
    // -----------------------------------------------------------------
    private void SeedSampleData()
    {
        Seed("Maria Santos",     "+63 917 123 4567", "maria.santos@email.com",  "Davao City",    2, "101",  -2,  3, 1000, 7000,  "GCash",         "Checked-in");
        Seed("Juan Dela Cruz",   "+63 918 555 0142", "juan.dc@email.com",       "Tagum City",    3, "102",   1,  4, 1000, 3000,  "Cash",          "Confirmed");
        Seed("Angela Reyes",     "+63 920 777 3321", "angela.reyes@email.com",  "Digos City",    2, "201",   5,  8, 1500, 0,     "Bank Transfer", "Pending");
        Seed("Mark Villanueva",  "+63 917 808 9090", "mark.v@email.com",        "Davao City",    4, "301",   2,  6, 2000, 20000, "Credit/Debit Card", "Confirmed");
        Seed("Sofia Lim",        "+63 921 444 1212", "sofia.lim@email.com",     "Panabo City",   5, "302",   9, 12, 1500, 5000,  "GCash",         "Confirmed");
        Seed("Carlo Mendoza",    "+63 915 222 6868", "carlo.m@email.com",       "Mati City",     2, "202",  -6, -3, 1000, 10000, "Cash",          "Completed");
    }

    private void Seed(string name, string phone, string email, string address, int guests, string unitCode,
                      int startOffset, int endOffset, decimal deposit, decimal paid, string method, string status)
    {
        var unit = _units.First(u => u.Code == unitCode);
        _reservations.Add(new ReservationItem
        {
            Id = "R-" + _nextId++,
            RenterName = name, Phone = phone, Email = email, Address = address, Guests = guests,
            UnitCode = unit.Code, UnitName = unit.Name, Rate = unit.Rate,
            CheckIn = DateTime.Today.AddDays(startOffset),
            CheckOut = DateTime.Today.AddDays(endOffset),
            Deposit = deposit, AmountPaid = paid, Method = method, Status = status
        });
    }

    // -----------------------------------------------------------------
    //  Refreshing everything on screen
    // -----------------------------------------------------------------
    private void RefreshAll()
    {
        _view.Refresh();
        BuildCalendar();
        UpdateStats();
        UpdateFilterText();
    }

    private void UpdateStats()
    {
        var today = DateTime.Today;
        var live = _reservations.Where(r => r.Status != "Cancelled").ToList();

        StatActive.Text = live.Count(r => r.CheckOut.Date >= today).ToString();
        StatOccupied.Text = live.Count(r => r.Occupies(today)).ToString();
        StatCollected.Text = ReservationItem.Money(live.Sum(r => r.AmountPaid));
        StatOutstanding.Text = ReservationItem.Money(live.Sum(r => r.Balance));
    }

    private void UpdateFilterText()
    {
        int count = _view.Cast<object>().Count();

        if (_selectedDay.HasValue)
        {
            FilterText.Text = $"Showing {_selectedDay.Value:MMM d, yyyy} \u00B7 {count} found";
            ShowAllButton.Visibility = Visibility.Visible;
        }
        else
        {
            FilterText.Text = $"Showing all reservations \u00B7 {count}";
            ShowAllButton.Visibility = Visibility.Collapsed;
        }

        EmptyText.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool FilterReservation(object o)
    {
        var r = (ReservationItem)o;

        if (_selectedDay.HasValue && !r.Occupies(_selectedDay.Value))
            return false;

        string q = SearchBox?.Text?.Trim();
        if (string.IsNullOrEmpty(q)) return true;

        return Has(r.Id, q) || Has(r.RenterName, q) || Has(r.Phone, q) || Has(r.UnitLabel, q) || Has(r.UnitName, q);
    }

    private static bool Has(string text, string q) =>
        text != null && text.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0;

    // -----------------------------------------------------------------
    //  Calendar
    // -----------------------------------------------------------------
    private static SolidColorBrush B(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex);

    private static string PaymentColor(string status) =>
        status == "Paid" ? "#27894C" : status == "Partial" ? "#D99A1E" : "#D9795B";

    private void BuildCalendar()
    {
        CalendarGrid.Children.Clear();
        MonthText.Text = _month.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

        var first = new DateTime(_month.Year, _month.Month, 1);
        var gridStart = first.AddDays(-(int)first.DayOfWeek);

        for (int i = 0; i < 42; i++)
            CalendarGrid.Children.Add(CreateDayCell(gridStart.AddDays(i)));
    }

    private UIElement CreateDayCell(DateTime date)
    {
        bool inMonth = date.Month == _month.Month;
        bool isToday = date.Date == DateTime.Today;
        bool isSelected = _selectedDay.HasValue && _selectedDay.Value.Date == date.Date;

        var bookings = _reservations.Where(r => r.Status != "Cancelled" && r.Occupies(date)).ToList();

        var cell = new Border
        {
            Margin = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Cursor = Cursors.Hand,
            Tag = date,
            BorderThickness = new Thickness(isSelected ? 2 : 1),
            BorderBrush = B(isSelected ? "#27894C" : "#E3EBE5"),
            Background = isSelected ? B("#EAF5EE") : (inMonth ? Brushes.White : B("#F6F8F6"))
        };

        var stack = new StackPanel { Margin = new Thickness(5, 4, 5, 3) };

        // day number (green circle for today)
        var num = new TextBlock
        {
            Text = date.Day.ToString(),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = isToday ? Brushes.White : B(inMonth ? "#1E3223" : "#A9B5AD"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (isToday)
        {
            stack.Children.Add(new Border
            {
                Background = B("#27894C"),
                CornerRadius = new CornerRadius(10),
                MinWidth = 20,
                Height = 20,
                Padding = new Thickness(4, 0, 4, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = num
            });
        }
        else
        {
            num.HorizontalAlignment = HorizontalAlignment.Left;
            stack.Children.Add(num);
        }

        // up to 2 booking chips, coloured by payment status
        foreach (var r in bookings.Take(2))
        {
            stack.Children.Add(new Border
            {
                Background = B(PaymentColor(r.PaymentStatus)),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 3, 0, 0),
                Padding = new Thickness(4, 1, 4, 1),
                Child = new TextBlock
                {
                    Text = $"{r.UnitCode} \u00B7 {r.RenterName.Split(' ')[0]}",
                    FontSize = 10,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                }
            });
        }
        if (bookings.Count > 2)
        {
            stack.Children.Add(new TextBlock
            {
                Text = $"+{bookings.Count - 2} more",
                FontSize = 10,
                Foreground = B("#6B7A70"),
                Margin = new Thickness(2, 2, 0, 0)
            });
        }

        if (bookings.Count > 0)
        {
            cell.ToolTip = string.Join("\n", bookings.Select(r =>
                $"{r.UnitLabel} \u2013 {r.RenterName} ({r.PaymentStatus})"));
        }

        cell.Child = stack;
        cell.MouseLeftButtonUp += Day_Click;
        return cell;
    }

    private void Day_Click(object sender, MouseButtonEventArgs e)
    {
        var date = ((DateTime)((Border)sender).Tag).Date;

        if (_selectedDay.HasValue && _selectedDay.Value.Date == date)
        {
            _selectedDay = null;                       // click again = clear filter
        }
        else
        {
            _selectedDay = date;

            // starting a new booking? pre-fill the dates
            if (_editing == null)
            {
                CheckInPicker.SelectedDate = date;
                if (!CheckOutPicker.SelectedDate.HasValue || CheckOutPicker.SelectedDate.Value.Date <= date)
                    CheckOutPicker.SelectedDate = date.AddDays(1);
            }
        }
        RefreshAll();
    }

    private void PrevMonth_Click(object sender, RoutedEventArgs e) { _month = _month.AddMonths(-1); BuildCalendar(); }
    private void NextMonth_Click(object sender, RoutedEventArgs e) { _month = _month.AddMonths(1); BuildCalendar(); }

    private void Today_Click(object sender, RoutedEventArgs e)
    {
        _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        BuildCalendar();
    }

    private void ShowAll_Click(object sender, RoutedEventArgs e)
    {
        _selectedDay = null;
        RefreshAll();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_ready) return;
        _view.Refresh();
        UpdateFilterText();
    }

    // Let the mouse wheel scroll the whole page even when the pointer is over the list
    private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        PageScroll.ScrollToVerticalOffset(PageScroll.VerticalOffset - e.Delta);
    }

    // -----------------------------------------------------------------
    //  Booking form: live pricing + payment summary
    // -----------------------------------------------------------------
    private void UnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        if (UnitCombo.SelectedItem is UnitOption u)
            RateBox.Text = u.Rate.ToString("0.##", CultureInfo.InvariantCulture);
        UpdateSummary();
    }

    private void Dates_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;

        // keep check-out after check-in
        if (CheckInPicker.SelectedDate.HasValue &&
            (!CheckOutPicker.SelectedDate.HasValue || CheckOutPicker.SelectedDate.Value <= CheckInPicker.SelectedDate.Value))
        {
            CheckOutPicker.SelectedDate = CheckInPicker.SelectedDate.Value.AddDays(1);
        }
        UpdateSummary();
    }

    private void Pricing_Changed(object sender, EventArgs e)
    {
        if (!_ready) return;
        UpdateSummary();
    }

    /// <summary>Returns 0 for blank, -1 for invalid, otherwise the amount.</summary>
    private static decimal ParseMoney(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Replace(ReservationItem.Peso, "").Replace("PHP", "").Trim();

        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out var v)) return v;
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v)) return v;
        return -1;
    }

    private void UpdateSummary()
    {
        int nights = 0;
        if (CheckInPicker.SelectedDate.HasValue && CheckOutPicker.SelectedDate.HasValue)
            nights = Math.Max(0, (CheckOutPicker.SelectedDate.Value.Date - CheckInPicker.SelectedDate.Value.Date).Days);

        decimal rate = Math.Max(0, ParseMoney(RateBox.Text));
        decimal deposit = Math.Max(0, ParseMoney(DepositBox.Text));
        decimal paid = Math.Max(0, ParseMoney(PaidBox.Text));

        decimal room = nights * rate;
        decimal total = room + deposit;
        decimal balance = Math.Max(0, total - paid);

        SumNights.Text = nights.ToString();
        SumRoom.Text = ReservationItem.Money(room);
        SumDeposit.Text = ReservationItem.Money(deposit);
        SumTotal.Text = ReservationItem.Money(total);
        BalanceText.Text = ReservationItem.Money(balance);

        string status = paid <= 0 ? "Unpaid" : (paid >= total ? "Paid" : "Partial");
        PayBadgeText.Text = status;
        switch (status)
        {
            case "Paid":
                PayBadge.Background = B("#D8EFE0"); PayBadgeText.Foreground = B("#1E6B3B"); break;
            case "Partial":
                PayBadge.Background = B("#FBEFD2"); PayBadgeText.Foreground = B("#8A6414"); break;
            default:
                PayBadge.Background = B("#FBE4DC"); PayBadgeText.Foreground = B("#A94B30"); break;
        }
    }

    private void ShowMessage(string text, bool isError)
    {
        FormMessage.Text = text;
        FormMessage.Foreground = B(isError ? "#C0392B" : "#1E6B3B");
    }

    // -----------------------------------------------------------------
    //  Form: new / clear / save / edit
    // -----------------------------------------------------------------
    private void NewReservation_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
        PageScroll.ScrollToTop();
        NameBox.Focus();
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => ClearForm();

    private void ClearForm()
    {
        _editing = null;
        FormTitle.Text = "New Reservation";
        SaveButton.Content = "Reserve Unit";

        UnitCombo.SelectedIndex = -1;
        RateBox.Text = "";
        DepositBox.Text = "0";
        PaidBox.Text = "0";

        var start = (_selectedDay ?? DateTime.Today).Date;
        CheckInPicker.SelectedDate = start;
        CheckOutPicker.SelectedDate = start.AddDays(1);

        NameBox.Text = "";
        PhoneBox.Text = "";
        GuestsBox.Text = "1";
        EmailBox.Text = "";
        AddressBox.Text = "";
        NotesBox.Text = "";

        MethodCombo.SelectedIndex = 0;
        StatusCombo.SelectedItem = "Confirmed";
        FormMessage.Text = "";

        if (_ready) UpdateSummary();
    }

    private void LoadForEdit(ReservationItem r)
    {
        _editing = r;
        FormTitle.Text = $"Edit Reservation {r.Id}";
        SaveButton.Content = "Update Reservation";

        UnitCombo.SelectedItem = _units.FirstOrDefault(u => u.Code == r.UnitCode);
        CheckInPicker.SelectedDate = r.CheckIn;
        CheckOutPicker.SelectedDate = r.CheckOut;

        NameBox.Text = r.RenterName;
        PhoneBox.Text = r.Phone;
        GuestsBox.Text = r.Guests.ToString();
        EmailBox.Text = r.Email;
        AddressBox.Text = r.Address;

        // set after the unit so the saved rate wins over the unit's default rate
        RateBox.Text = r.Rate.ToString("0.##", CultureInfo.InvariantCulture);
        DepositBox.Text = r.Deposit.ToString("0.##", CultureInfo.InvariantCulture);
        PaidBox.Text = r.AmountPaid.ToString("0.##", CultureInfo.InvariantCulture);

        MethodCombo.SelectedItem = r.Method;
        StatusCombo.SelectedItem = r.Status;
        NotesBox.Text = r.Notes;
        FormMessage.Text = "";

        UpdateSummary();
        PageScroll.ScrollToTop();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var unit = UnitCombo.SelectedItem as UnitOption;
        string name = NameBox.Text.Trim();
        string phone = PhoneBox.Text.Trim();
        string email = EmailBox.Text.Trim();

        // ---- validation ----
        if (unit == null) { ShowMessage("Please choose a unit.", true); return; }
        if (!CheckInPicker.SelectedDate.HasValue || !CheckOutPicker.SelectedDate.HasValue)
        { ShowMessage("Please pick the check-in and check-out dates.", true); return; }

        DateTime checkIn = CheckInPicker.SelectedDate.Value.Date;
        DateTime checkOut = CheckOutPicker.SelectedDate.Value.Date;
        if (checkOut <= checkIn) { ShowMessage("Check-out must be after check-in.", true); return; }

        if (name.Length == 0) { ShowMessage("Please enter the renter's full name.", true); return; }
        if (phone.Length == 0) { ShowMessage("Please enter a contact number.", true); return; }
        if (email.Length > 0 && !email.Contains("@")) { ShowMessage("That email address doesn't look right.", true); return; }
        if (!int.TryParse(GuestsBox.Text.Trim(), out int guests) || guests < 1)
        { ShowMessage("Guests must be a number, 1 or more.", true); return; }

        decimal rate = ParseMoney(RateBox.Text);
        decimal deposit = ParseMoney(DepositBox.Text);
        decimal paid = ParseMoney(PaidBox.Text);
        if (rate <= 0) { ShowMessage("Enter a valid rate per night.", true); return; }
        if (deposit < 0) { ShowMessage("Enter a valid security deposit (or 0).", true); return; }
        if (paid < 0) { ShowMessage("Enter a valid amount paid (or 0).", true); return; }

        string status = StatusCombo.SelectedItem as string ?? "Pending";

        // ---- no double booking ----
        if (status != "Cancelled")
        {
            var clash = _reservations.FirstOrDefault(r =>
                r != _editing && r.Status != "Cancelled" && r.UnitCode == unit.Code &&
                checkIn < r.CheckOut.Date && checkOut > r.CheckIn.Date);

            if (clash != null)
            {
                ShowMessage($"{unit.Label.Split('(')[0].Trim()} is already booked {clash.StayText} by {clash.RenterName}.", true);
                return;
            }
        }

        // ---- save ----
        bool wasEditing = _editing != null;
        var item = _editing ?? new ReservationItem { Id = "R-" + _nextId++ };

        item.UnitCode = unit.Code;
        item.UnitName = unit.Name;
        item.Rate = rate;
        item.CheckIn = checkIn;
        item.CheckOut = checkOut;
        item.RenterName = name;
        item.Phone = phone;
        item.Email = email;
        item.Address = AddressBox.Text.Trim();
        item.Guests = guests;
        item.Deposit = deposit;
        item.AmountPaid = paid;
        item.Method = MethodCombo.SelectedItem as string ?? "Cash";
        item.Status = status;
        item.Notes = NotesBox.Text.Trim();

        if (wasEditing) item.Notify();
        else _reservations.Add(item);

        // TODO: save to your DAO / Service here (insert or update `item`)

        _month = new DateTime(checkIn.Year, checkIn.Month, 1);   // jump the calendar to the stay
        _selectedDay = null;
        ClearForm();
        RefreshAll();
        ShowMessage(wasEditing ? $"Reservation {item.Id} updated." : $"Reservation {item.Id} saved for {item.RenterName}.", false);
    }

    // -----------------------------------------------------------------
    //  List row actions
    // -----------------------------------------------------------------
    private static ReservationItem RowItem(object sender) =>
        (sender as FrameworkElement)?.DataContext as ReservationItem;

    private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ReservationGrid.SelectedItem is ReservationItem r) LoadForEdit(r);
    }

    private void EditRow_Click(object sender, RoutedEventArgs e)
    {
        var r = RowItem(sender);
        if (r != null) LoadForEdit(r);
    }

    private void DeleteRow_Click(object sender, RoutedEventArgs e)
    {
        var r = RowItem(sender);
        if (r == null) return;

        var answer = MessageBox.Show($"Delete reservation {r.Id} for {r.RenterName}?", "Delete reservation",
                                     MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        if (_editing == r) ClearForm();
        _reservations.Remove(r);

        // TODO: delete from your DAO / Service here
        RefreshAll();
    }

    private void PayRow_Click(object sender, RoutedEventArgs e)
    {
        var r = RowItem(sender);
        if (r != null) AddPayment(r);
    }

    /// <summary>Small pop-up to record a payment against a reservation.</summary>
    private void AddPayment(ReservationItem r)
    {
        var win = new Window
        {
            Title = "Record payment",
            Width = 380,
            SizeToContent = SizeToContent.Height,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            Background = Brushes.White
        };

        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock
        {
            Text = $"{r.Id} \u00B7 {r.RenterName}",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = B("#1E3223")
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Total {r.TotalText}  \u00B7  Paid {ReservationItem.Money(r.AmountPaid)}  \u00B7  Balance {ReservationItem.Money(r.Balance)}",
            FontSize = 12,
            Foreground = B("#6B7A70"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 14)
        });

        panel.Children.Add(new TextBlock { Text = "Amount received", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = B("#6B7A70") });
        var amount = new TextBox
        {
            Text = r.Balance.ToString("0.##", CultureInfo.InvariantCulture),
            Height = 34,
            Margin = new Thickness(0, 4, 0, 10),
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(8, 0, 8, 0)
        };
        panel.Children.Add(amount);

        panel.Children.Add(new TextBlock { Text = "Method", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = B("#6B7A70") });
        var method = new ComboBox
        {
            ItemsSource = _methods,
            SelectedItem = _methods.Contains(r.Method) ? r.Method : _methods[0],
            Height = 34,
            Margin = new Thickness(0, 4, 0, 18),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(method);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancel", Width = 90, Height = 34, Margin = new Thickness(0, 0, 8, 0) };
        var save = new Button { Content = "Save payment", Width = 120, Height = 34, IsDefault = true };
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        panel.Children.Add(buttons);

        bool saved = false;
        cancel.Click += (s, a) => win.Close();
        save.Click += (s, a) =>
        {
            decimal value = ParseMoney(amount.Text);
            if (value <= 0)
            {
                MessageBox.Show("Enter an amount greater than 0.", "Record payment");
                return;
            }
            r.AmountPaid += value;
            r.Method = method.SelectedItem as string ?? r.Method;
            saved = true;
            win.Close();
        };

        win.Content = panel;
        win.ShowDialog();

        if (saved)
        {
            r.Notify();
            // TODO: save the payment through your DAO / Service here
            if (_editing == r) LoadForEdit(r);   // keep the form in sync if it's open
            RefreshAll();
            ShowMessage($"Payment recorded for {r.Id}. {r.PaymentStatus}.", false);
        }
    }
}
