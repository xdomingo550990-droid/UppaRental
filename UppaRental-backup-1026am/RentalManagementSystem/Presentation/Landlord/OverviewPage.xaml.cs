using MySql.Data.MySqlClient;
using RentalManagementSystem.Model;
using RentalManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace RentalManagementSystem.Presentation
{
    public partial class OverviewPage : UserControl
    {
        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        // Display data representations
        private sealed record Lease(string Code, string Unit, string UnitType, string Tenant,
                                    DateTime Start, DateTime End, decimal Rent, DateTime Updated, string Payment)
        {
            public bool Occupied => !string.IsNullOrWhiteSpace(Tenant) && Tenant != "—";
        }

        private sealed record GridRow(string C1, string C2, string C3, string C4, string C5, string Status, string Tone);

        private sealed record LegendItem(string Label, string Display, Brush Color);

        private static readonly string[] ReportNames = { "Rental Report", "Payment Report", "Occupancy / Unit Status" };

        private string _query = "";
        private User loggedInUser;

        public OverviewPage()
        {
            InitializeComponent();
            StartPicker.SelectedDate = DateTime.Today.AddDays(-45);
            EndPicker.SelectedDate = DateTime.Today;
            Generate();
        }

        // Added : this() chaining so InitializeComponent() and controls load properly
        public OverviewPage(User loggedInUser) : this()
        {
            this.loggedInUser = loggedInUser;
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e) => Generate();

        public void SetSearch(string text)
        {
            _query = text?.Trim() ?? "";

            if (ReportsGrid == null) return;
            Generate();
        }

        private bool MatchesQuery(GridRow r) =>
            _query.Length == 0
            || new[] { r.C1, r.C2, r.C3, r.C4, r.C5, r.Status }
                .Any(s => !string.IsNullOrEmpty(s) && s.Contains(_query, StringComparison.OrdinalIgnoreCase));

        private List<Lease> GetLeasesFromDb()
        {
            var list = new List<Lease>();

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Queries lease, unit, tenant, and payment records from MySQL
                    string sql = @"
                        SELECT 
                            COALESCE(l.lease_code, CONCAT('LS-', l.lease_id)) AS lease_code,
                            COALESCE(u.unit_label, u.unit_number, '—') AS unit_name,
                            COALESCE(u.unit_type, 'Standard') AS unit_type,
                            TRIM(CONCAT(COALESCE(usr.first_name, ''), ' ', COALESCE(usr.last_name, ''))) AS tenant_name,
                            l.start_date,
                            l.end_date,
                            COALESCE(l.monthly_rent, u.monthly_rate, 0) AS rent,
                            COALESCE(l.updated_at, l.start_date, NOW()) AS updated_at,
                            COALESCE(p.payment_status, 'Paid') AS payment_status
                        FROM leases l
                        LEFT JOIN units u ON l.unit_id = u.unit_id
                        LEFT JOIN users usr ON l.tenant_id = usr.user_id
                        LEFT JOIN (
                            SELECT lease_id, payment_status 
                            FROM payments 
                            ORDER BY payment_date DESC
                        ) p ON l.lease_id = p.lease_id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tenantName = reader["tenant_name"]?.ToString() ?? "";
                            if (string.IsNullOrWhiteSpace(tenantName)) tenantName = "—";

                            list.Add(new Lease(
                                Code: reader["lease_code"]?.ToString() ?? "—",
                                Unit: reader["unit_name"]?.ToString() ?? "—",
                                UnitType: reader["unit_type"]?.ToString() ?? "—",
                                Tenant: tenantName,
                                Start: reader["start_date"] != DBNull.Value ? Convert.ToDateTime(reader["start_date"]) : DateTime.Today,
                                End: reader["end_date"] != DBNull.Value ? Convert.ToDateTime(reader["end_date"]) : DateTime.Today.AddYears(1),
                                Rent: reader["rent"] != DBNull.Value ? Convert.ToDecimal(reader["rent"]) : 0m,
                                Updated: reader["updated_at"] != DBNull.Value ? Convert.ToDateTime(reader["updated_at"]) : DateTime.Today,
                                Payment: reader["payment_status"]?.ToString() ?? "Paid"
                            ));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error querying database in OverviewPage: {ex.Message}");
            }

            return list;
        }

        private void Generate()
        {
            DateTime from = StartPicker.SelectedDate ?? DateTime.MinValue;
            DateTime to = EndPicker.SelectedDate ?? DateTime.MaxValue;

            var dbLeases = GetLeasesFromDb();

            var inRange = dbLeases.Where(l => l.Updated.Date >= from.Date && l.Updated.Date <= to.Date).ToList();
            var leases = inRange.Where(l => l.Occupied).ToList();

            int kind = Math.Max(0, ReportTypeBox?.SelectedIndex ?? 0);
            string[] headers;
            GridRow[] rows;

            switch (kind)
            {
                case 1: // Payment Report
                    headers = new[] { "Payment ID", "Tenant", "Unit", "Date Paid", "Amount", "Payment Status" };
                    rows = leases.Select(l => new GridRow(
                        l.Code.Replace("LS", "PAY"), l.Tenant, l.Unit,
                        l.Updated.ToString("MMM d, yyyy", Us), Money(l.Rent), l.Payment, Tone(l.Payment))).ToArray();
                    break;

                case 2: // Occupancy / Unit Status
                    headers = new[] { "Unit", "Unit Type", "Tenant", "Last Update", "Monthly Rate", "Unit Status" };
                    rows = inRange.Select(l =>
                    {
                        string s = l.Occupied ? "Occupied" : "Vacant";
                        return new GridRow(l.Unit, l.UnitType, l.Tenant,
                            l.Updated.ToString("MMM d, yyyy", Us), Money(l.Rent), s, Tone(s));
                    }).ToArray();
                    break;

                default: // Rental Report
                    headers = new[] { "Lease ID", "Tenant", "Unit", "Lease Period", "Monthly Rent", "Lease Status" };
                    rows = leases.Select(l =>
                    {
                        string s = l.End < DateTime.Today.AddDays(45) ? "Expiring" : "Active";
                        string period = l.Start.ToString("MMM yyyy", Us) + " – " + l.End.ToString("MMM yyyy", Us);
                        return new GridRow(l.Code, l.Tenant, l.Unit, period, Money(l.Rent), s, Tone(s));
                    }).ToArray();
                    break;
            }

            rows = rows.Where(MatchesQuery).ToArray();

            SetColumns(headers);
            ReportsGrid.ItemsSource = rows;
            TableTitle.Text = $"{ReportNames[kind]}  ·  {rows.Length} records";

            // KPI calculation
            RevenueText.Text = Money(leases.Where(l => l.Payment == "Paid").Sum(l => l.Rent));
            OutstandingText.Text = Money(leases.Where(l => l.Payment is "Pending" or "Overdue").Sum(l => l.Rent));
            OccupancyText.Text = inRange.Count == 0 ? "0%" : ((double)leases.Count / inRange.Count).ToString("P1", Us);
            LeasesText.Text = leases.Count(l => l.End >= DateTime.Today).ToString();

            BuildDonut(leases);
        }

        private void SetColumns(string[] headers)
        {
            ReportsGrid.Columns.Clear();
            string[] props = { nameof(GridRow.C1), nameof(GridRow.C2), nameof(GridRow.C3), nameof(GridRow.C4), nameof(GridRow.C5) };
            double[] widths = { 1.1, 1.6, 1.3, 1.6, 1.3 };

            for (int i = 0; i < 5; i++)
            {
                var col = new DataGridTextColumn
                {
                    Header = headers[i],
                    Binding = new Binding(props[i]),
                    Width = new DataGridLength(widths[i], DataGridLengthUnitType.Star),
                    MinWidth = 100
                };
                if (i == 0) col.ElementStyle = (Style)FindResource("IdText");
                if (i == 4)
                {
                    col.ElementStyle = (Style)FindResource("AmountText");
                    col.HeaderStyle = (Style)FindResource("HeaderRight");
                }
                ReportsGrid.Columns.Add(col);
            }

            ReportsGrid.Columns.Add(new DataGridTemplateColumn
            {
                Header = headers[5],
                CellTemplate = (DataTemplate)FindResource("StatusBadge"),
                Width = new DataGridLength(1.2, DataGridLengthUnitType.Star),
                MinWidth = 110
            });
        }

        private static string Money(decimal v) => "₱ " + v.ToString("N2", Us);

        private static string Tone(string status) => status switch
        {
            "Paid" or "Active" or "Occupied" => "good",
            "Pending" or "Expiring" => "warn",
            "Overdue" => "bad",
            _ => "neutral"
        };

        private void BuildDonut(List<Lease> leases)
        {
            const double size = 180;
            double radius = size / 2;
            var center = new Point(radius, radius);
            var legend = new List<LegendItem>();
            double angle = -90;

            PieCanvas.Children.Clear();
            int total = leases.Count;

            foreach (var (status, hex) in new[] { ("Paid", "#12874F"), ("Pending", "#F59E0B"), ("Overdue", "#EF4444") })
            {
                int count = leases.Count(l => l.Payment == status);
                if (count == 0 || total == 0) continue;

                var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
                double sweep = 360.0 * count / total;

                PieCanvas.Children.Add(MakeSlice(center, radius, angle, sweep, brush));
                legend.Add(new LegendItem(status, $"{count}  ({(double)count / total:P0})", brush));
                angle += sweep;
            }

            PieTotal.Text = total.ToString();
            LegendList.ItemsSource = legend;
        }

        private static System.Windows.Shapes.Path MakeSlice(Point c, double r, double startDeg, double sweepDeg, Brush fill)
        {
            if (sweepDeg >= 359.99) sweepDeg = 359.99;

            Point P(double deg) => new(c.X + r * Math.Cos(deg * Math.PI / 180),
                                       c.Y + r * Math.Sin(deg * Math.PI / 180));

            var figure = new PathFigure { StartPoint = c, IsClosed = true };
            figure.Segments.Add(new LineSegment(P(startDeg), true));
            figure.Segments.Add(new ArcSegment(P(startDeg + sweepDeg), new Size(r, r), 0,
                                               sweepDeg > 180, SweepDirection.Clockwise, true));

            return new System.Windows.Shapes.Path
            {
                Data = new PathGeometry(new[] { figure }),
                Fill = fill,
                Stroke = Brushes.White,
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round
            };
        }
    }
}