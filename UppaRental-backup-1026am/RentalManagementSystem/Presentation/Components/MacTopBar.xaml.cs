using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RentalManagementSystem.Presentation.Components
{
    public partial class MacTopBar : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(MacTopBar),
                new PropertyMetadata("MacBook App"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public MacTopBar()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Window? parentWindow = Window.GetWindow(this);
                parentWindow?.DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Window? parentWindow = Window.GetWindow(this);
            parentWindow?.Close();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            Window? parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.WindowState = WindowState.Minimized;
            }
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            Window? parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.WindowState = parentWindow.WindowState == WindowState.Normal
                    ? WindowState.Maximized
                    : WindowState.Normal;
            }
        }
    }
}