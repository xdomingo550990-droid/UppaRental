using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace RentalManagementSystem.Presentation
{
    /// <summary>One line in a renter's history. Positive = charge billed, negative = payment received.</summary>
    public class LedgerEntry
    {
        public DateTime Date { get; set; } = DateTime.Today;
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
    }

    /// <summary>One row in the table: either a renter (lease) or a reservation.</summary>
    public class RenterRow
    {
        public const decimal DownRate = 0.20m;
        public const string LongTerm = "Long-Term";
        public const string ShortTerm = "Short-Term";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Contact { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string LeaseStart { get; set; } = "";
        public string LeaseEnd { get; set; } = "";
        public string Status { get; set; } = "Active";   // Active, Pending, Past

        /// <summary>Long-Term or Short-Term.</summary>
        public string RentalTerm { get; set; } = LongTerm;

        /// <summary>Rate: per month for Long-Term, per day for Short-Term.</summary>
        public decimal Rate { get; set; }

        /// <summary>Whole months covered (Long-Term only).</summary>
        public int Months { get; set; }

        /// <summary>Total days covered (used by Short-Term).</summary>
        public int Days { get; set; }

        /// <summary>True when this row is a reservation (Pending until the renter moves in).</summary>
        public bool IsReservation { get; set; }

        // ---- reservation fields ----
        public bool ReservationPaid { get; set; }
        public DateTime? HoldUntil { get; set; }
        public bool DownRecorded { get; set; }

        // ---- move-in fields ----
        public decimal Advance { get; set; }
        public decimal Deposit { get; set; }
        public decimal ReservationCredit { get; set; }
        public bool MoveInRecorded { get; set; }

        // ---- archiving (nothing is ever erased) ----
        public bool IsArchived { get; set; }
        public DateTime? MoveOutDate { get; set; }
        public string ArchiveReason { get; set; } = "";

        /// <summary>Full transaction history. Kept after move-out for audit and tracking.</summary>
        public List<LedgerEntry> Transactions { get; } = new List<LedgerEntry>();

        public bool IsShortTerm => RentalTerm == ShortTerm;
        public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name.Substring(0, 1).ToUpper();
        public string LeasePeriod => $"{LeaseStart} – {LeaseEnd}";
        public string RentText => "₱" + Rate.ToString("N0", Inv) + (IsShortTerm ? " / day" : "");
        public string TypeText => IsReservation ? "Reservation" : "Renter";

        // ---- calculation: rate × months (long) or rate × days (short); 20% down only for long-term ----
        public decimal TotalRent => IsShortTerm ? Rate * Days : Rate * Months;
        public decimal Downpayment => IsShortTerm ? 0m : Math.Round(TotalRent * DownRate, 2);

        /// <summary>Positive = unpaid balance owed. Negative = payment held as credit.</summary>
        public decimal Balance => Transactions.Sum(t => t.Amount);

        public bool HoldExpired => IsReservation && !IsArchived && !ReservationPaid
                                   && HoldUntil.HasValue && HoldUntil.Value.Date < DateTime.Today;

        public string SubText
        {
            get
            {
                if (IsArchived && Balance > 0) return "Unpaid balance: " + Money(Balance);
                if (IsReservation && !IsArchived)
                {
                    string pay = ReservationPaid ? "Paid" : (HoldExpired ? "Hold expired" : "Unpaid");
                    return IsShortTerm ? $"Short-term · {Days} days · {pay}"
                                       : $"20% down: {Money(Downpayment)} · {pay}";
                }
                if (IsShortTerm) return $"Short-term · {Days} days";
                return "";
            }
        }
        public bool HasSubText => !string.IsNullOrEmpty(SubText);

        public bool CanPromote => !IsArchived && (Status == "Pending" || Status == "Reserved");
        public bool CanMoveOut => !IsArchived && Status == "Active";

        public void Log(string description, decimal amount) =>
            Transactions.Add(new LedgerEntry { Date = DateTime.Today, Description = description, Amount = amount });

        public static string Money(decimal v) => "₱" + v.ToString("N2", Inv);

        /// <summary>Whole months covered from start to end (inclusive). Jan 15 – Jul 14 = 6.</summary>
        public static int MonthsBetween(DateTime start, DateTime end)
        {
            DateTime stop = end.Date.AddDays(1);
            int m = (stop.Year - start.Year) * 12 + stop.Month - start.Month;
            if (start.Date.AddMonths(m) > stop) m--;
            return Math.Max(m, 0);
        }

        /// <summary>Days covered from start to end (inclusive). Oct 1 – Oct 7 = 7.</summary>
        public static int DaysBetween(DateTime start, DateTime end) =>
            Math.Max((end.Date - start.Date).Days + 1, 0);
    }

    public partial class RentersPage : UserControl
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private const string DateFormat = "MMM dd, yyyy";
        private const string AllUnits = "All Properties / Units";

        // Monthly rate of every unit. Used to pre-fill the rent when you reserve a unit (long-term).
        // TODO: load these from your DAO / Service (same source as the Properties page).
        private static readonly Dictionary<string, decimal> UnitRates = new()
        {
            ["Unit 101"] = 10000m,
            ["Unit 102"] = 10000m,
            ["Unit 105"] = 12500m,
            ["Unit 201"] = 15000m,
            ["Unit 202"] = 15000m,
            ["Unit 203"] = 20000m,
            ["Unit 204"] = 20000m,
            ["Unit 301"] = 25000m,
            ["Unit 302"] = 25000m,
            ["Unit 401"] = 30000m,
            ["Unit 402"] = 30000m,
        };
        private readonly string[] _statuses = { "Active", "Pending", "Past" };

        /// <summary>Available / Reserved / Occupied for every unit, recalculated whenever a record changes.</summary>
        private readonly Dictionary<string, string> _unitStatus = new Dictionary<string, string>();

        private readonly ObservableCollection<RenterRow> _renters = new ObservableCollection<RenterRow>
        {
            new RenterRow { Name = "Juan Dela Cruz",   Unit = "Unit 101", Contact = "0917 123 4567", Email = "juan.dc@email.com",  Address = "Tagum City",  LeaseStart = "Jan 15, 2026", LeaseEnd = "Jan 14, 2027", Rate = 12500, Months = 12, Status = "Active" },
            new RenterRow { Name = "Maria Santos",     Unit = "Unit 204", Contact = "0918 765 4321", Email = "maria.s@email.com",  Address = "Davao City",  LeaseStart = "Mar 01, 2026", LeaseEnd = "Feb 28, 2027", Rate = 9800,  Months = 12, Status = "Active" },
            new RenterRow { Name = "Ana Reyes",        Unit = "Unit 105", Contact = "0922 555 0148", Email = "ana.reyes@email.com", Address = "Digos City", LeaseStart = "Jun 10, 2026", LeaseEnd = "Jun 09, 2027", Rate = 12500, Months = 12, Status = "Active" },
            new RenterRow { Name = "Mark Villanueva",  Unit = "Unit 203", Contact = "0916 880 3392", Email = "mark.v@email.com",   Address = "Davao City",  LeaseStart = "Feb 01, 2026", LeaseEnd = "Jan 31, 2027", Rate = 15000, Months = 12, Status = "Active" },
            new RenterRow { Name = "Carlo Mendoza",    Unit = "Unit 202", Contact = "0905 222 7781", Email = "carlo.m@email.com",  Address = "Mati City",   LeaseStart = "Oct 15, 2026", LeaseEnd = "Oct 14, 2027", Rate = 15000, Months = 12, Status = "Pending" },
            // Past renters stay on file with their history (sample unpaid balance shown here)
            new RenterRow { Name = "Liza Garcia",      Unit = "Unit 301", Contact = "0999 310 4426", Email = "liza.g@email.com",   Address = "Panabo City", LeaseStart = "Sep 01, 2025", LeaseEnd = "Aug 31, 2026", Rate = 9800,  Months = 12, Status = "Past",
                           IsArchived = true, MoveOutDate = new DateTime(2026, 8, 31), ArchiveReason = "Lease ended" },
        };

        private ICollectionView _view = null!;
        private RenterRow? _editing;
        private string _statusFilter = "All";
        private string _unitFilter = AllUnits;
        private string _globalSearchQuery = "";

        private bool _ready;       // false while the page is still being built
        private int _suspend;      // > 0 while code (not the user) is filling the form

        private bool IsReservationMode => rbModeReservation.IsChecked == true;
        private bool IsShortTerm => (TermCombo.SelectedItem as string) == RenterRow.ShortTerm;
        private bool IsConverting => _editing != null && _editing.IsReservation && !IsReservationMode;

        public RentersPage()
        {
            InitializeComponent();

            var unitNames = UnitRates.Keys.OrderBy(k => k).ToList();
            UnitCombo.ItemsSource = unitNames;
            UnitFilterCombo.ItemsSource = new[] { AllUnits }.Concat(unitNames).ToList();
            UnitFilterCombo.SelectedIndex = 0;
            StatusCombo.ItemsSource = _statuses;
            TermCombo.ItemsSource = new[] { RenterRow.ShortTerm, RenterRow.LongTerm };
            TermCombo.SelectedItem = RenterRow.LongTerm;

            // The sample renters that are not Pending have already moved in
            foreach (var r in _renters.Where(r => r.Status != "Pending")) r.MoveInRecorded = true;
            _renters.First(r => r.Status == "Past").Log("Unpaid utilities (Aug 2026)", 1250m);   // sample data

            SeedSampleReservations();

            _view = CollectionViewSource.GetDefaultView(_renters);
            _view.Filter = Matches;
            dgRenters.ItemsSource = _view;

            ClearForm();
            UpdateSummary();
            RefreshUnitStatuses();

            _ready = true;
            ApplyMode();
        }

        // Sample reservations (delete once your DAO supplies real ones)
        private void SeedSampleReservations()
        {
            AddSampleReservation("Paolo Ramirez", "0917 808 9090", "paolo.r@email.com", "Unit 102", 3, 6, true);
            AddSampleReservation("Grace Tan", "0918 555 0142", "grace.t@email.com", "Unit 302", 10, 12, true);
            AddSampleReservation("Daniel Cruz", "0917 123 8801", "daniel.c@email.com", "Unit 401", 20, 6, false);
        }

        private void AddSampleReservation(string name, string phone, string email, string unit, int moveInDays, int months, bool paid)
        {
            DateTime start = DateTime.Today.AddDays(moveInDays);
            var row = new RenterRow
            {
                Name = name,
                Contact = phone,
                Email = email,
                Unit = unit,
                LeaseStart = start.ToString(DateFormat, Inv),
                LeaseEnd = start.AddMonths(months).AddDays(-1).ToString(DateFormat, Inv),
                Rate = UnitRates[unit],
                Months = months,
                Status = "Pending",
                IsReservation = true,
                ReservationPaid = paid,
                HoldUntil = start
            };
            if (paid) RecordReservationPayment(row);
            _renters.Add(row);
        }

        // ---------- Global Search Hook (called by DashboardPage's top search bar) ----------

        public void ApplyGlobalSearch(string query)
        {
            _globalSearchQuery = query?.Trim() ?? "";
            _view?.Refresh();
        }

        // ---------- Filtering ----------

        private bool Matches(object item)
        {
            var r = (RenterRow)item;

            if (_unitFilter != AllUnits && r.Unit != _unitFilter) return false;

            if (_statusFilter == "Reservation") { if (!r.IsReservation) return false; }
            else if (_statusFilter != "All" && r.Status != _statusFilter) return false;

            if (string.IsNullOrEmpty(_globalSearchQuery)) return true;

            return r.Name.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.Unit.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.Contact.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.Email.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.Status.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.TypeText.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase);
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb) _statusFilter = rb.Tag?.ToString() ?? "All";
            _view?.Refresh();
        }

        private void UnitFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            _unitFilter = UnitFilterCombo.SelectedItem as string ?? AllUnits;
            _view?.Refresh();
        }

        // ---------- Filter pill counts ----------

        private void UpdateSummary()
        {
            rbAll.Content = $"All ({_renters.Count})";
            rbActive.Content = $"Active ({_renters.Count(r => r.Status == "Active")})";
            rbPending.Content = $"Pending ({_renters.Count(r => r.Status == "Pending")})";
            rbReservations.Content = $"Reservations ({_renters.Count(r => r.IsReservation)})";
            rbPast.Content = $"Past ({_renters.Count(r => r.Status == "Past")})";
        }

        // ---------- Unit availability ----------

        /// <summary>A unit is Available when nobody current holds it. Archived (Past) records free the unit.</summary>
        private void RefreshUnitStatuses()
        {
            foreach (var u in UnitRates.Keys)
            {
                var holder = _renters.FirstOrDefault(r => r.Unit == u && !r.IsArchived && r.Status != "Past");
                _unitStatus[u] = holder == null ? "Available" : holder.Status == "Active" ? "Occupied" : "Reserved";
            }

            // TODO: save _unitStatus through your DAO / Service so the Properties page lists freed units as Available
        }

        public string GetUnitStatus(string unit) =>
            _unitStatus.TryGetValue(unit, out var s) ? s : "Available";

        // ---------- Entry type: New Renter | Reservation ----------

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            if (!_ready || _suspend > 0) return;

            bool res = IsReservationMode;

            if (_editing == null)
            {
                // Brand-new entry: switch the defaults to suit the chosen type
                _suspend++;
                try
                {
                    StatusCombo.SelectedItem = res ? "Pending" : "Active";
                    if (StartPicker.SelectedDate is DateTime s) EndPicker.SelectedDate = DefaultEnd(s);
                    if (res && !IsShortTerm && UnitCombo.SelectedItem is string u && UnitRates.TryGetValue(u, out decimal rate))
                        RentBox.Text = rate.ToString("0.##", Inv);
                    if (!res) FillAdvanceFromRate();
                }
                finally { _suspend--; }
            }
            else if (!res && _editing.IsReservation)
            {
                // Editing a reservation and switching to "New Renter" = the renter is moving in
                _suspend++;
                try
                {
                    if ((StatusCombo.SelectedItem as string) == "Pending") StatusCombo.SelectedItem = "Active";
                    FillMoveInFromReservation(_editing);
                }
                finally { _suspend--; }
            }

            ApplyMode();
        }

        /// <summary>Move-in defaults for a reservation: advance = 1 month, credit = the downpayment already paid.</summary>
        private void FillMoveInFromReservation(RenterRow r)
        {
            decimal credit = r.ReservationPaid ? r.Downpayment : 0m;
            CreditBox.Text = credit > 0 ? credit.ToString("0.##", Inv) : "";
            AdvanceBox.Text = !r.IsShortTerm ? r.Rate.ToString("0.##", Inv) : "";
            DepositBox.Text = r.Deposit > 0 ? r.Deposit.ToString("0.##", Inv) : "";
        }

        /// <summary>For long-term renters the advance is one month of rent.</summary>
        private void FillAdvanceFromRate()
        {
            AdvanceBox.Text = !IsShortTerm && TryParseRent(RentBox.Text, out decimal r) && r > 0
                ? r.ToString("0.##", Inv) : "";
        }

        /// <summary>Updates every label, strip and button that depends on term / renter / reservation / editing.</summary>
        private void ApplyMode()
        {
            bool res = IsReservationMode;
            bool edit = _editing != null;
            bool st = IsShortTerm;

            PanelTitle.Text = res ? "Reservation Details" : "Renter Details";
            PanelSubtitle.Text = res
                ? (edit ? "Editing a reservation. Switch to New Renter once the tenant moves in."
                        : st ? "Reserve a unit for a short stay. No downpayment is required."
                             : "Reserve a unit. A 20% downpayment secures it until move-in.")
                : (edit ? (IsConverting ? "Moving in from a reservation. Review the payments, then update."
                                        : "Editing a renter. Click Clear Fields to add a new one.")
                        : "Add a new renter, or click a row in the table to edit it.");

            SaveText.Text = (edit ? "Update " : "Save ") + (res ? "Reservation" : "Renter");
            DeleteText.Text = res ? "Cancel Reservation" : "Move Out Renter";
            ArchiveHint.Text = res
                ? "Cancel archives the reservation and frees the unit. Nothing is erased."
                : "Move Out archives the selected record and frees the unit. Nothing is erased.";

            TermLabel.Text = res ? (st ? "Stay Period" : "Move-in & End Date")
                                 : (st ? "Stay Period" : "Lease Term");
            RentLabel.Text = st ? "Daily/Weekly Rate (\u20B1)" : "Monthly Rent (\u20B1)";
            AdvanceLabel.Text = st ? "Advance Payment (\u20B1)" : "1 Month Advance (\u20B1)";

            ReservationFields.Visibility = res ? Visibility.Visible : Visibility.Collapsed;
            ReservationStrip.Visibility = res ? Visibility.Visible : Visibility.Collapsed;
            DownPanel.Visibility = st ? Visibility.Collapsed : Visibility.Visible;
            ResNoteText.Text = st ? "No downpayment is required for short-term reservations."
                                  : "A 20% downpayment is required to reserve the unit.";
            MoveInStrip.Visibility = res ? Visibility.Collapsed : Visibility.Visible;

            // Reservation credit only applies when a reservation is being converted to a renter
            CreditBox.IsEnabled = IsConverting;
            CreditBox.Opacity = IsConverting ? 1.0 : 0.55;

            UpdateCalc();
        }

        private DateTime DefaultEnd(DateTime start) =>
            IsShortTerm ? start.AddDays(6)
            : IsReservationMode ? start.AddMonths(6).AddDays(-1)
            : start.AddYears(1).AddDays(-1);

        // ---------- Live calculation ----------
        // Long-Term: rate × months, 20% downpayment.  Short-Term: rate × days, no downpayment.

        private void Term_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _suspend > 0) return;

            if (_editing == null)
            {
                _suspend++;
                try
                {
                    if (StartPicker.SelectedDate is DateTime s) EndPicker.SelectedDate = DefaultEnd(s);

                    if (IsShortTerm) RentBox.Text = "";   // a monthly rate would be wrong as a daily rate
                    else if (IsReservationMode && UnitCombo.SelectedItem is string u && UnitRates.TryGetValue(u, out decimal rate))
                        RentBox.Text = rate.ToString("0.##", Inv);

                    if (!IsReservationMode) FillAdvanceFromRate();
                }
                finally { _suspend--; }
            }

            ApplyMode();
        }

        private void UnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _suspend > 0) return;

            // For long-term reservations the rent follows the unit's monthly rate
            if (IsReservationMode && !IsShortTerm && UnitCombo.SelectedItem is string u && UnitRates.TryGetValue(u, out decimal rate))
                RentBox.Text = rate.ToString("0.##", Inv);

            UpdateCalc();
        }

        private void Rent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_ready) return;

            // typing a monthly rent for a new renter also fills the 1 month advance
            if (_suspend == 0 && !IsReservationMode && !IsShortTerm && !IsConverting) FillAdvanceFromRate();

            UpdateCalc();
        }

        private void Money_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ready) UpdateCalc();
        }

        private void Date_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready) return;

            // a new reservation holds the unit until the move-in date by default
            if (_suspend == 0 && _editing == null && ReferenceEquals(sender, StartPicker) && StartPicker.SelectedDate is DateTime s)
                HoldPicker.SelectedDate = s;

            UpdateCalc();
        }

        private void UpdateCalc()
        {
            if (!_ready) return;

            bool st = IsShortTerm;
            decimal rate = TryParseRent(RentBox.Text, out decimal r) ? r : 0m;

            int count = 0;
            if (StartPicker.SelectedDate is DateTime s && EndPicker.SelectedDate is DateTime en && (st ? en >= s : en > s))
                count = st ? RenterRow.DaysBetween(s, en) : RenterRow.MonthsBetween(s, en);

            decimal total = rate * count;
            decimal down = st ? 0m : Math.Round(total * RenterRow.DownRate, 2);
            string word = st ? (count == 1 ? "day" : "days") : (count == 1 ? "month" : "months");

            ResTotalText.Text = count < 1
                ? (st ? "Pick the start and end dates to compute the total rent."
                      : "Pick the move-in and end dates to compute the total rent.")
                : $"Total rent: {RenterRow.Money(total)}  ({count} {word} \u00D7 {RenterRow.Money(rate)})";
            ResDownText.Text = RenterRow.Money(down);

            decimal due = ParseOptional(AdvanceBox.Text) + ParseOptional(DepositBox.Text) - ParseOptional(CreditBox.Text);
            NetDueText.Text = RenterRow.Money(Math.Max(due, 0m));
        }

        private static bool TryParseRent(string text, out decimal rent) =>
            decimal.TryParse(text.Replace("₱", "").Replace(",", "").Trim(), NumberStyles.Number, Inv, out rent);

        /// <summary>Blank or invalid counts as 0 (use IsValidOptional to reject bad input first).</summary>
        private static decimal ParseOptional(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0m;
            return TryParseRent(text, out decimal v) && v >= 0 ? v : 0m;
        }

        private static bool IsValidOptional(string text) =>
            string.IsNullOrWhiteSpace(text) || (TryParseRent(text, out decimal v) && v >= 0);

        // ---------- Ledger helpers ----------

        /// <summary>A paid reservation downpayment is held as credit until the renter moves in.</summary>
        private static void RecordReservationPayment(RenterRow row)
        {
            if (row.DownRecorded || row.IsShortTerm || row.Downpayment <= 0) return;
            row.Log("Reservation downpayment received", -row.Downpayment);
            row.DownRecorded = true;
        }

        private static void RecordMoveIn(RenterRow row, decimal advance, decimal deposit, decimal credit)
        {
            if (advance > 0) row.Log(row.IsShortTerm ? "Advance payment" : "1 month advance", advance);
            if (deposit > 0) row.Log("Security deposit", deposit);

            decimal cash = advance + deposit - credit;     // the credit was already paid with the reservation
            if (cash > 0) row.Log("Move-in payment received", -cash);
            row.MoveInRecorded = true;
        }

        // ---------- Form: save / clear / move out ----------

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            bool res = IsReservationMode;
            bool st = IsShortTerm;

            string name = NameBox.Text.Trim();
            string contact = ContactBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string? unit = UnitCombo.SelectedItem as string;
            string? status = StatusCombo.SelectedItem as string;

            // ---- validation ----
            if (name.Length == 0) { ShowMessage("Please enter the renter's name.", true); return; }
            if (contact.Length == 0) { ShowMessage("Please enter a contact number.", true); return; }
            if (email.Length > 0 && (!email.Contains('@') || !email.Contains('.')))
            { ShowMessage("That email address doesn't look right.", true); return; }
            if (unit == null) { ShowMessage("Please choose an assigned unit.", true); return; }
            if (!StartPicker.SelectedDate.HasValue || !EndPicker.SelectedDate.HasValue)
            { ShowMessage(st ? "Please pick the start and end dates." : res ? "Please pick the move-in and end dates." : "Please pick the lease start and end dates.", true); return; }

            DateTime start = StartPicker.SelectedDate.Value.Date;
            DateTime end = EndPicker.SelectedDate.Value.Date;
            if (st ? end < start : end <= start)
            { ShowMessage(st ? "The end date can't be before the start date." : "The end date must be after the start date.", true); return; }

            if (!TryParseRent(RentBox.Text, out decimal rate) || rate <= 0)
            { ShowMessage(st ? "Enter a valid daily rate." : "Enter a valid monthly rent.", true); return; }

            if (status == null) { ShowMessage("Please choose a lease status.", true); return; }

            int months = st ? 0 : RenterRow.MonthsBetween(start, end);
            int days = RenterRow.DaysBetween(start, end);
            if (!st && months < 1)
            { ShowMessage("A long-term rental must cover at least 1 month. Use Short-Term for shorter stays.", true); return; }

            bool newReservation = res && (_editing == null || !_editing.IsReservation);
            DateTime hold = HoldPicker.SelectedDate?.Date ?? start;

            decimal advance = 0, deposit = 0, credit = 0;

            if (res)
            {
                if (newReservation && start < DateTime.Today)
                { ShowMessage("The move-in date can't be in the past.", true); return; }
                if (!st && months > 60)
                { ShowMessage("A reservation must cover 1 to 60 months.", true); return; }
                if (!HoldPicker.SelectedDate.HasValue)
                { ShowMessage("Please pick the hold-until date.", true); return; }
                if (newReservation && hold < DateTime.Today)
                { ShowMessage("The hold-until date can't be in the past.", true); return; }
                if (hold > start)
                { ShowMessage("The hold should end on or before the move-in date.", true); return; }
            }
            else
            {
                if (!IsValidOptional(AdvanceBox.Text) || !IsValidOptional(DepositBox.Text) || !IsValidOptional(CreditBox.Text))
                { ShowMessage("Advance, deposit and credit must be valid amounts.", true); return; }

                advance = ParseOptional(AdvanceBox.Text);
                deposit = ParseOptional(DepositBox.Text);
                credit = IsConverting ? ParseOptional(CreditBox.Text) : (_editing?.ReservationCredit ?? 0m);

                if (credit > advance + deposit)
                { ShowMessage("The reservation credit can't be more than the advance plus the deposit.", true); return; }
            }

            // one unit can't have two current renters / reservations
            if (status != "Past")
            {
                var clash = _renters.FirstOrDefault(r => r != _editing && r.Unit == unit && r.Status != "Past");
                if (clash != null)
                {
                    ShowMessage($"{unit} is already assigned to {clash.Name}.", true);
                    return;
                }
            }

            // ---- payment confirmations ----
            bool paid = rbPayPaid.IsChecked == true;
            decimal down = st ? 0m : Math.Round(rate * months * RenterRow.DownRate, 2);

            bool becamePaid = res && paid && !st && down > 0
                              && (_editing == null || !_editing.IsReservation || !_editing.ReservationPaid);
            if (becamePaid)
            {
                var answer = MessageBox.Show(
                    $"Confirm that the 20% downpayment of {RenterRow.Money(down)} for {unit} " +
                    $"has been received from {name}?",
                    "Confirm downpayment", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes) return;
            }

            bool recordMoveIn = !res && status == "Active" && (_editing == null || !_editing.MoveInRecorded);
            decimal due = advance + deposit - credit;
            if (recordMoveIn && due > 0)
            {
                var answer = MessageBox.Show(
                    $"Confirm that the move-in payment of {RenterRow.Money(due)} " +
                    $"(advance + deposit{(credit > 0 ? " - reservation credit" : "")}) has been received from {name}?",
                    "Confirm move-in payment", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes) return;
            }

            // ---- save ----
            bool isEdit = _editing != null;
            bool converted = !res && isEdit && _editing!.IsReservation;
            var row = _editing ?? new RenterRow();

            row.Name = name;
            row.Contact = contact;
            row.Email = email;
            row.Address = AddressBox.Text.Trim();
            row.Unit = unit;
            row.LeaseStart = start.ToString(DateFormat, Inv);
            row.LeaseEnd = end.ToString(DateFormat, Inv);
            row.RentalTerm = st ? RenterRow.ShortTerm : RenterRow.LongTerm;
            row.Rate = rate;
            row.Months = months;
            row.Days = days;
            row.Status = status;
            row.IsReservation = res;

            if (res)
            {
                row.ReservationPaid = paid;
                row.HoldUntil = hold;
            }
            else
            {
                row.Advance = advance;
                row.Deposit = deposit;
                row.ReservationCredit = credit;
            }

            // Past = archived (kept with its history); anything else is current
            if (status == "Past")
            {
                if (!row.IsArchived)
                {
                    row.IsArchived = true;
                    row.MoveOutDate = DateTime.Today;
                    row.ArchiveReason = "Marked as past";
                }
            }
            else row.IsArchived = false;

            if (!isEdit) _renters.Add(row);

            if (res && paid) RecordReservationPayment(row);
            if (recordMoveIn) RecordMoveIn(row, advance, deposit, credit);

            // TODO: save `row` and its Transactions through your DAO / Service here (insert or update)

            _view.Refresh();
            UpdateSummary();
            RefreshUnitStatuses();
            ClearForm();

            if (newReservation)
                ShowMessage(paid && !st
                    ? $"{unit} reserved for {row.Name}. Downpayment {RenterRow.Money(down)} recorded."
                    : $"{unit} reserved for {row.Name}. Held until {hold.ToString(DateFormat, Inv)}.", false);
            else if (converted) ShowMessage($"{row.Name} moved in to {unit}.", false);
            else ShowMessage(isEdit ? $"{row.Name} updated." : $"{row.Name} saved.", false);
        }

        private void Clear_Click(object sender, RoutedEventArgs e) => ClearForm();

        /// <summary>The form's red button. Archives the selected record; nothing is deleted.</summary>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var target = dgRenters.SelectedItem as RenterRow ?? _editing;
            if (target == null)
            {
                ShowMessage("Select a renter or reservation in the table first.", true);
                return;
            }
            ArchiveRow(target);
        }

        private void MoveOut_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RenterRow r) ArchiveRow(r);
        }

        /// <summary>
        /// Move Out / Terminate Lease / Cancel Reservation.
        /// Frees the unit, moves the profile to Past, and keeps the profile, transactions and unpaid balance.
        /// </summary>
        private void ArchiveRow(RenterRow target)
        {
            if (target.IsArchived || target.Status == "Past")
            {
                ShowMessage($"{target.Name} is already archived under Past.", true);
                return;
            }

            bool isRes = target.IsReservation;
            var sb = new StringBuilder();
            sb.Append(isRes
                ? $"Cancel the reservation of {target.Name} for {target.Unit}?"
                : $"Move out {target.Name} from {target.Unit} and end the lease?");
            sb.Append($"\n\n{target.Unit} will become Available again.");
            sb.Append("\nThe profile moves to Past and keeps its history and transactions.");
            if (target.Balance > 0)
                sb.Append($"\n\nUnpaid balance kept on record: {RenterRow.Money(target.Balance)}");
            else if (target.Balance < 0)
                sb.Append($"\n\nPayments on file: {RenterRow.Money(-target.Balance)} (any refund is handled outside this screen).");

            var answer = MessageBox.Show(sb.ToString(), isRes ? "Cancel reservation" : "Move out renter",
                                         MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            target.Status = "Past";
            target.IsArchived = true;
            target.MoveOutDate = DateTime.Today;
            target.ArchiveReason = isRes ? "Reservation cancelled" : "Moved out";
            target.Log(isRes ? "Reservation cancelled" : "Moved out / lease terminated", 0m);

            // TODO: save the archived `target` (status Past + MoveOutDate) and set the unit to Available through your DAO / Service here

            _view.Refresh();
            UpdateSummary();
            RefreshUnitStatuses();
            ClearForm();

            ShowMessage(isRes
                ? $"{target.Name}'s reservation was cancelled. {target.Unit} is now {GetUnitStatus(target.Unit)}."
                : $"{target.Name} moved out. {target.Unit} is now {GetUnitStatus(target.Unit)}; the profile is under Past.", false);
        }

        /// <summary>Empties the form. Keeps the current type and term so you can add several in a row.</summary>
        private void ClearForm()
        {
            _suspend++;
            try
            {
                _editing = null;

                NameBox.Text = "";
                ContactBox.Text = "";
                EmailBox.Text = "";
                AddressBox.Text = "";
                RentBox.Text = "";
                AdvanceBox.Text = "";
                DepositBox.Text = "";
                CreditBox.Text = "";
                rbPayUnpaid.IsChecked = true;
                UnitCombo.SelectedIndex = -1;
                if (TermCombo.SelectedItem == null) TermCombo.SelectedItem = RenterRow.LongTerm;
                StatusCombo.SelectedItem = IsReservationMode ? "Pending" : "Active";
                StartPicker.SelectedDate = DateTime.Today;
                EndPicker.SelectedDate = DefaultEnd(DateTime.Today);
                HoldPicker.SelectedDate = DateTime.Today;
            }
            finally { _suspend--; }

            dgRenters.UnselectAll();
            FormMessage.Visibility = Visibility.Collapsed;
            ApplyMode();
        }

        private void LoadForEdit(RenterRow r)
        {
            _suspend++;
            try
            {
                _editing = r;

                if (r.IsReservation) rbModeReservation.IsChecked = true;
                else rbModeRenter.IsChecked = true;

                NameBox.Text = r.Name;
                ContactBox.Text = r.Contact;
                EmailBox.Text = r.Email;
                AddressBox.Text = r.Address;
                UnitCombo.SelectedItem = r.Unit;
                TermCombo.SelectedItem = r.RentalTerm;
                StartPicker.SelectedDate = ParseDate(r.LeaseStart);
                EndPicker.SelectedDate = ParseDate(r.LeaseEnd);
                RentBox.Text = r.Rate.ToString("0.##", Inv);
                StatusCombo.SelectedItem = r.Status;

                (r.ReservationPaid ? rbPayPaid : rbPayUnpaid).IsChecked = true;
                HoldPicker.SelectedDate = r.HoldUntil ?? ParseDate(r.LeaseStart);
                AdvanceBox.Text = r.Advance > 0 ? r.Advance.ToString("0.##", Inv) : "";
                DepositBox.Text = r.Deposit > 0 ? r.Deposit.ToString("0.##", Inv) : "";
                CreditBox.Text = r.ReservationCredit > 0 ? r.ReservationCredit.ToString("0.##", Inv) : "";
            }
            finally { _suspend--; }

            FormMessage.Visibility = Visibility.Collapsed;
            ApplyMode();
        }

        private static DateTime? ParseDate(string text) =>
            DateTime.TryParseExact(text, DateFormat, Inv, DateTimeStyles.None, out var d) ? d : (DateTime?)null;

        private void ShowMessage(string text, bool isError)
        {
            FormMessage.Text = text;
            FormMessage.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(isError ? "#FFB4A8" : "#BFE8CC")!;
            FormMessage.Visibility = Visibility.Visible;
        }

        // ---------- Table interaction ----------

        // Clicking a row loads that renter / reservation into the form so it can be edited or moved out
        private void Renters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgRenters.SelectedItem is RenterRow r) LoadForEdit(r);
        }

        /// <summary>Promote / Move-In: turns a Pending or Reserved record into an Active renter.</summary>
        private void Promote_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not RenterRow r || !r.CanPromote) return;

            dgRenters.SelectedItem = r;                       // selecting the row fills the form
            if (!ReferenceEquals(_editing, r)) LoadForEdit(r);

            if (r.IsReservation)
            {
                // Switching to New Renter sets Active and fills advance + reservation credit
                rbModeRenter.IsChecked = true;
            }
            else
            {
                _suspend++;
                try
                {
                    StatusCombo.SelectedItem = "Active";
                    if (r.Advance <= 0) FillAdvanceFromRate();
                }
                finally { _suspend--; }
                ApplyMode();
            }

            ShowMessage($"Moving in {r.Name}. Check the move-in payments, then click Update Renter.", false);
            DepositBox.Focus();
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not RenterRow r) return;

            var sb = new StringBuilder();
            sb.AppendLine($"{r.Name}  ·  {r.TypeText}  ·  {r.Status}");
            sb.AppendLine(r.Unit);
            sb.AppendLine(r.Contact);
            sb.AppendLine(r.Email);
            sb.AppendLine(r.Address);
            sb.AppendLine($"{r.RentalTerm}: {r.LeasePeriod}");
            sb.AppendLine($"Rate: {r.RentText}");

            if (r.IsReservation)
            {
                int n = r.IsShortTerm ? r.Days : r.Months;
                sb.AppendLine($"Total rent: {RenterRow.Money(r.TotalRent)} ({n} {(r.IsShortTerm ? "days" : "months")})");
                if (!r.IsShortTerm) sb.AppendLine($"Downpayment (20%): {RenterRow.Money(r.Downpayment)}");
                sb.AppendLine($"Payment: {(r.ReservationPaid ? "Paid" : "Unpaid")}");
                if (r.HoldUntil.HasValue)
                    sb.AppendLine($"Hold until: {r.HoldUntil.Value.ToString(DateFormat, Inv)}{(r.HoldExpired ? " (expired)" : "")}");
            }
            else if (r.Advance > 0 || r.Deposit > 0)
            {
                sb.AppendLine($"Advance: {RenterRow.Money(r.Advance)}   Deposit: {RenterRow.Money(r.Deposit)}");
                if (r.ReservationCredit > 0) sb.AppendLine($"Reservation credit: {RenterRow.Money(r.ReservationCredit)}");
            }

            if (r.IsArchived)
                sb.AppendLine($"Archived: {r.MoveOutDate?.ToString(DateFormat, Inv)} ({r.ArchiveReason})");

            sb.AppendLine();
            sb.AppendLine(r.Balance > 0 ? $"Unpaid balance: {RenterRow.Money(r.Balance)}"
                        : r.Balance < 0 ? $"Credit on file: {RenterRow.Money(-r.Balance)}"
                        : "Balance: settled");

            if (r.Transactions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Transactions:");
                foreach (var t in r.Transactions.TakeLast(10))
                    sb.AppendLine($"  {t.Date.ToString(DateFormat, Inv)}  {t.Description}  " +
                                  (t.Amount == 0 ? "" : (t.Amount > 0 ? "+" : "-") + RenterRow.Money(Math.Abs(t.Amount))));
            }

            MessageBox.Show(sb.ToString(), r.IsReservation ? "Reservation details" : "Renter details");
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RenterRow r)
            {
                dgRenters.SelectedItem = r;   // selecting the row fills the form
                NameBox.Focus();
            }
        }
    }
}