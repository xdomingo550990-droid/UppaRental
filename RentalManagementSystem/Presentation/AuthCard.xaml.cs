using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RentalManagementSystem.Presentation
{
    public partial class AuthCard : UserControl
    {
        private const double RightX = 400;   // overlay on the right -> Register form visible
        private const double LeftX = -120;   // overlay on the left  -> Login form visible
        private static readonly Duration SlideTime = new Duration(TimeSpan.FromMilliseconds(600));

        /// <summary>Raised when the user logs in successfully.</summary>
        public event EventHandler LoginSucceeded;

        /// <summary>Raised when the user clicks the close (X) button.</summary>
        public event EventHandler CloseRequested;

        public AuthCard()
        {
            InitializeComponent();
        }

        /// <summary>Shows the card instantly in Login or Register mode (no animation).</summary>
        public void ShowMode(bool login)
        {
            OverlayTransform.BeginAnimation(TranslateTransform.XProperty, null);
            OverlayTransform.X = login ? LeftX : RightX;
            SetVisible(LoginPanel, login);
            SetVisible(HelloPanel, login);
            SetVisible(RegisterPanel, !login);
            SetVisible(WelcomeBackPanel, !login);
        }

        private static void SetVisible(UIElement element, bool show)
        {
            element.BeginAnimation(OpacityProperty, null);
            element.Opacity = show ? 1 : 0;
            element.IsHitTestVisible = show;
        }

        // ---------- Slide between Register and Login ----------
        private void ShowLogin_Click(object sender, RoutedEventArgs e) => Slide(toLogin: true);
        private void ShowRegister_Click(object sender, RoutedEventArgs e) => Slide(toLogin: false);

        private void Slide(bool toLogin)
        {
            var move = new DoubleAnimation(toLogin ? LeftX : RightX, SlideTime)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            OverlayTransform.BeginAnimation(TranslateTransform.XProperty, move);

            Fade(LoginPanel, toLogin);
            Fade(HelloPanel, toLogin);
            Fade(RegisterPanel, !toLogin);
            Fade(WelcomeBackPanel, !toLogin);
        }

        private static void Fade(UIElement element, bool show)
        {
            element.IsHitTestVisible = show;
            var fade = new DoubleAnimation(show ? 1 : 0, new Duration(TimeSpan.FromMilliseconds(300)))
            {
                BeginTime = show ? TimeSpan.FromMilliseconds(250) : TimeSpan.Zero
            };
            element.BeginAnimation(OpacityProperty, fade);
        }

        // ---------- Placeholder (hint) text ----------
        private void Field_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is Control c && c.Tag is TextBlock hint)
            {
                string value = sender is PasswordBox pb ? pb.Password : ((TextBox)sender).Text;
                hint.Visibility = string.IsNullOrEmpty(value) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // ---------- Actions (hook these up to your DAO / Service layer) ----------
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            if (RegUsername.Text.Trim().Length == 0 || RegEmail.Text.Trim().Length == 0 ||
                RegPassword.Password.Length == 0)
            {
                MessageBox.Show("Please fill in all fields.", "Registration");
                return;
            }

            // TODO: save the new user via your Service/DAO
            MessageBox.Show("Account created! You can now log in.", "Registration");
            Slide(toLogin: true);
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (LoginUsername.Text.Trim().Length == 0 || LoginPassword.Password.Length == 0)
            {
                MessageBox.Show("Enter your username and password.", "Login");
                return;
            }

            // TODO: validate the credentials via your Service/DAO; only raise on success
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }

        private void Close_Click(object sender, RoutedEventArgs e) =>
            CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
