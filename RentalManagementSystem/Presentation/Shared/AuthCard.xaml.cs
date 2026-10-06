using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        /// <summary>Raised when the user clicks "Register" on the login side (so the page can ask tenant/landlord first).</summary>
        public event EventHandler RegisterRequested;

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
        private void ShowRegister_Click(object sender, RoutedEventArgs e) => RegisterRequested?.Invoke(this, EventArgs.Empty);

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

            // Live "passwords do not match" warning while typing on the register form
            if (sender == RegPassword || sender == RegPasswordPlain ||
                sender == RegConfirmPassword || sender == RegConfirmPasswordPlain)
            {
                CheckPasswordMatch();
            }
        }

        // ---------- Show / hide password ----------
        // Each password field is a Grid holding a PasswordBox, a plain TextBox (hidden) and the eye ToggleButton.
        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            var toggle = (ToggleButton)sender;
            var grid = (Grid)toggle.Parent;
            var pb = grid.Children.OfType<PasswordBox>().First();
            var tb = grid.Children.OfType<TextBox>().First();

            if (toggle.IsChecked == true)       // show the password
            {
                tb.Text = pb.Password;
                pb.Visibility = Visibility.Collapsed;
                tb.Visibility = Visibility.Visible;
                tb.Focus();
                tb.CaretIndex = tb.Text.Length;
            }
            else                                // hide the password
            {
                pb.Password = tb.Text;
                tb.Visibility = Visibility.Collapsed;
                pb.Visibility = Visibility.Visible;
                pb.Focus();
            }
        }

        /// <summary>Reads the password from whichever control (masked or plain) is currently showing.</summary>
        private static string GetPassword(PasswordBox pb, TextBox tb) =>
            tb.Visibility == Visibility.Visible ? tb.Text : pb.Password;

        private bool CheckPasswordMatch()
        {
            if (RegMessage == null) return true;

            string password = GetPassword(RegPassword, RegPasswordPlain);
            string confirm = GetPassword(RegConfirmPassword, RegConfirmPasswordPlain);

            bool mismatch = confirm.Length > 0 && password != confirm;
            RegMessage.Text = "Passwords do not match.";
            RegMessage.Visibility = mismatch ? Visibility.Visible : Visibility.Collapsed;
            return !mismatch;
        }

        // ---------- Actions (hook these up to your DAO / Service layer) ----------
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string password = GetPassword(RegPassword, RegPasswordPlain);
            string confirm = GetPassword(RegConfirmPassword, RegConfirmPasswordPlain);

            if (RegUsername.Text.Trim().Length == 0 || RegEmail.Text.Trim().Length == 0 ||
                password.Length == 0 || confirm.Length == 0)
            {
                MessageBox.Show("Please fill in all fields.", "Registration");
                return;
            }

            if (password != confirm)
            {
                RegMessage.Text = "Passwords do not match.";
                RegMessage.Visibility = Visibility.Visible;
                return;
            }
            RegMessage.Visibility = Visibility.Collapsed;

            // TODO: save the new user via your Service/DAO (use the 'password' variable above)
            MessageBox.Show("Account created! You can now log in.", "Registration");
            Slide(toLogin: true);
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string password = GetPassword(LoginPassword, LoginPasswordPlain);

            if (LoginUsername.Text.Trim().Length == 0 || password.Length == 0)
            {
                MessageBox.Show("Enter your username and password.", "Login");
                return;
            }

            // TODO: validate the credentials via your Service/DAO (use the 'password' variable above); only raise on success
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }

        private void Close_Click(object sender, RoutedEventArgs e) =>
            CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
