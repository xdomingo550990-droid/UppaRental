using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RentalManagementSystem.Presentation;

public partial class ReportsPage : UserControl
{
    private sealed record Row(string ReportId, DateTime DateGenerated, string ReportType,
                              string Reference, decimal Amount, string Status);

    private sealed record LegendItem(string Label, string Display, Brush Color);

    public ReportsPage()
    {
        InitializeComponent();

        StartPicker.SelectedDate = DateTime.Today.AddDays(-30);
        EndPicker.SelectedDate = DateTime.Today;

        // Sample rows for the design. Your teammates can replace these with database data later.
        var rows = new[]
        {
            new Row("RPT-1001", DateTime.Today.AddDays(-1),  "Payment Report",          "Ana Reyes - Unit 105",       12500m, "Paid"),
            new Row("RPT-1002", DateTime.Today.AddDays(-2),  "Rental Report",           "Carlo Mendoza - Unit 212",   28900m, "Paid"),
            new Row("RPT-1003", DateTime.Today.AddDays(-4),  "Rental Report",           "Liza Garcia - Unit 301",      9800m, "Pending"),
            new Row("RPT-1004", DateTime.Today.AddDays(-5),  "Payment Report",          "Mark Villanueva - Unit 118", 15000m, "Overdue"),
            new Row("RPT-1005", DateTime.Today.AddDays(-7),  "Occupancy / Unit Status", "Bea Navarro - Unit 204",     21400m, "Paid"),
            new Row("RPT-1006", DateTime.Today.AddDays(-9),  "Rental Report",           "Paolo Aquino - Unit 110",    33000m, "Paid"),
            new Row("RPT-1007", DateTime.Today.AddDays(-12), "Occupancy / Unit Status", "Jenny Ramos - Unit 307",      7600m, "Pending"),
            new Row("RPT-1008", DateTime.Today.AddDays(-15), "Payment Report",          "Rico Bautista - Unit 120",   18200m, "Paid"),
        };

        ReportsGrid.ItemsSource = rows;
        BuildPie(rows);
    }

    // Draws the pie from the rows' Status values, so the chart always matches the table.
    private void BuildPie(Row[] rows)
    {
        const double size = 200;
        double radius = size / 2;
        var center = new Point(radius, radius);
        var legend = new List<LegendItem>();
        double angle = -90;   // start at 12 o'clock

        foreach (var (status, hex) in new[] { ("Paid", "#12874F"), ("Pending", "#F59E0B"), ("Overdue", "#EF4444") })
        {
            int count = rows.Count(r => r.Status == status);
            if (count == 0) continue;

            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            double sweep = 360.0 * count / rows.Length;

            PieCanvas.Children.Add(MakeSlice(center, radius, angle, sweep, brush));
            legend.Add(new LegendItem(status, $"{count}  ({(double)count / rows.Length:P0})", brush));
            angle += sweep;
        }

        PieTotal.Text = rows.Length.ToString();
        LegendList.ItemsSource = legend;
    }

    private static System.Windows.Shapes.Path MakeSlice(Point c, double r, double startDeg, double sweepDeg, Brush fill)
    {
        if (sweepDeg >= 359.99) sweepDeg = 359.99;   // a full circle can't be a single arc

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
            Stroke = Brushes.White,          // white gaps between slices
            StrokeThickness = 3,
            StrokeLineJoin = PenLineJoin.Round
        };
    }
}